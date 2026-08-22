using Cronos;
using EconomIA.CargaDeDados.Configuration;
using EconomIA.CargaDeDados.HealthChecks;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Observability;
using EconomIA.CargaDeDados.Repositories;
using EconomIA.CargaDeDados.Services;
using Microsoft.Extensions.Options;

namespace EconomIA.CargaDeDados.Workers;

public class CargaDiariaWorker : BackgroundService {
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<CargaDiariaWorker> logger;
	private readonly WorkerConfiguration configuracao;
	private readonly CronExpression cronExpression;

	public CargaDiariaWorker(
		IServiceScopeFactory scopeFactory,
		ILogger<CargaDiariaWorker> logger,
		IOptions<WorkerConfiguration> configuracao) {
		this.scopeFactory = scopeFactory;
		this.logger = logger;
		this.configuracao = configuracao.Value;
		this.cronExpression = CronExpression.Parse(this.configuracao.CronExpression);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		if (!configuracao.CargaDiariaHabilitada) {
			logger.LogInformation("Worker de carga diaria desabilitado por configuracao");
			return;
		}

		logger.LogInformation(
			"Worker de carga diaria iniciado. Cron: {CronExpression}, Modo: {Modo}",
			configuracao.CronExpression,
			configuracao.ModoCargaAutomatica);

		while (!stoppingToken.IsCancellationRequested) {
			var proximaExecucao = cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Local);

			if (proximaExecucao is null) {
				logger.LogWarning("Nao foi possivel calcular proxima execucao. Tentando novamente em 1 minuto");
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				continue;
			}

			var delay = proximaExecucao.Value - DateTimeOffset.UtcNow;

			if (delay > TimeSpan.Zero) {
				logger.LogInformation("Proxima execucao agendada para {ProximaExecucao:dd/MM/yyyy HH:mm:ss}", proximaExecucao.Value.LocalDateTime);

				try {
					await Task.Delay(delay, stoppingToken);
				} catch (OperationCanceledException) {
					break;
				}
			}

			if (!stoppingToken.IsCancellationRequested) {
				await ExecutarCargaAsync(stoppingToken);
			}
		}

		logger.LogInformation("Worker de carga diaria finalizado");
	}

	private async Task ExecutarCargaAsync(CancellationToken stoppingToken) {
		using var scope = scopeFactory.CreateScope();
		var servicos = scope.ServiceProvider;

		var execucoesCarga = servicos.GetRequiredService<ExecucoesCarga>();

		var execucaoAtiva = await execucoesCarga.ObterExecucaoEmAndamentoAsync();

		if (execucaoAtiva is not null) {
			logger.LogWarning(
				"Execucao em andamento detectada (ID: {ExecucaoId}, Modo: {Modo}, Inicio: {Inicio}). Carga agendada sera ignorada.",
				execucaoAtiva.Identificador,
				execucaoAtiva.ModoExecucao,
				execucaoAtiva.InicioEm);
			return;
		}

		if (configuracao.ModoCargaAutomatica == ModoExecucao.Brasil) {
			await ExecutarCargaBrasilAsync(servicos, execucoesCarga, stoppingToken);
		} else {
			await ExecutarCargaIncrementalAsync(servicos, execucoesCarga, stoppingToken);
		}
	}

	private async Task ExecutarCargaBrasilAsync(IServiceProvider servicos, ExecucoesCarga execucoesCarga, CancellationToken stoppingToken) {
		var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();

		var metricas = new MetricasExecucao();
		var execucao = await execucoesCarga.IniciarExecucaoAsync(ModoExecucao.Brasil, TipoGatilho.Scheduler);

		WorkerHealthCheck.RegistrarInicioExecucao(execucao.Identificador);

		try {
			var dataFinal = DateTime.Now;
			var dataInicial = dataFinal.AddDays(-configuracao.DiasRetroativos);

			logger.LogInformation(
				"Iniciando carga Brasil. Execucao ID: {ExecucaoId}, Periodo: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy}",
				execucao.Identificador, dataInicial, dataFinal);

			var resultado = await servicoBrasil.ProcessarCargaCompletaAsync(dataInicial, dataFinal, apenasModalidadesComDados: true, cancellationToken: stoppingToken);

			metricas.TotalComprasProcessadas = resultado.ComprasProcessadas;
			metricas.TotalItensIndexados = resultado.ItensIndexados;
			metricas.TotalContratosProcessados = resultado.ContratosProcessados;
			metricas.TotalAtasProcessadas = resultado.AtasProcessadas;
			metricas.TotalOrgaosProcessados = resultado.OrgaosProcessados;

			await EncadearEnriquecimentoDeAdesaoAsync(servicoBrasil, ModoExecucao.Brasil, metricas, stoppingToken);

			await execucoesCarga.FinalizarComSucessoAsync(execucao.Identificador, metricas);

			logger.LogInformation(
				"Carga Brasil finalizada. Execucao ID: {ExecucaoId}, Compras: {TotalCompras}, Contratos: {TotalContratos}, Atas: {TotalAtas}, Itens: {TotalItens}, Duracao: {Duracao}ms",
				execucao.Identificador,
				resultado.ComprasProcessadas,
				resultado.ContratosProcessados,
				resultado.AtasProcessadas,
				resultado.ItensIndexados,
				resultado.DuracaoMs);

			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Sucesso);
		} catch (OperationCanceledException) {
			logger.LogWarning("Carga Brasil cancelada. Execucao ID: {ExecucaoId}", execucao.Identificador);
			await execucoesCarga.FinalizarComCancelamentoAsync(execucao.Identificador, metricas);
			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Cancelado);
		} catch (Exception ex) {
			logger.LogError(ex, "Erro na carga Brasil. Execucao ID: {ExecucaoId}", execucao.Identificador);
			await execucoesCarga.FinalizarComErroAsync(execucao.Identificador, ex.Message, ex.StackTrace, metricas);
			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Erro);
		}
	}

	private async Task ExecutarCargaIncrementalAsync(IServiceProvider servicos, ExecucoesCarga execucoesCarga, CancellationToken stoppingToken) {
		var orquestrador = servicos.GetRequiredService<ServicoOrquestradorImportacao>();

		var metricas = new MetricasExecucao();
		var execucao = await execucoesCarga.IniciarExecucaoAsync(ModoExecucao.Incremental, TipoGatilho.Scheduler);

		WorkerHealthCheck.RegistrarInicioExecucao(execucao.Identificador);

		try {
			logger.LogInformation("Iniciando carga incremental. Execucao ID: {ExecucaoId}", execucao.Identificador);

			await orquestrador.ExecutarImportacaoIncrementalAsync(metricas, cnpjsFiltro: null, stoppingToken);

			var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();
			await EncadearEnriquecimentoDeAdesaoAsync(servicoBrasil, ModoExecucao.Incremental, metricas, stoppingToken);

			await execucoesCarga.FinalizarComSucessoAsync(execucao.Identificador, metricas);

			logger.LogInformation(
				"Carga incremental finalizada. Execucao ID: {ExecucaoId}, Orgaos: {TotalOrgaos}, Compras: {TotalCompras}, Contratos: {TotalContratos}, Atas: {TotalAtas}, Duracao: {Duracao}ms",
				execucao.Identificador,
				metricas.TotalOrgaosProcessados,
				metricas.TotalComprasProcessadas,
				metricas.TotalContratosProcessados,
				metricas.TotalAtasProcessadas,
				metricas.DuracaoTotalMs);

			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Sucesso);
		} catch (OperationCanceledException) {
			logger.LogWarning("Carga incremental cancelada. Execucao ID: {ExecucaoId}", execucao.Identificador);
			await execucoesCarga.FinalizarComCancelamentoAsync(execucao.Identificador, metricas);
			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Cancelado);
		} catch (Exception ex) {
			logger.LogError(ex, "Erro na carga incremental. Execucao ID: {ExecucaoId}", execucao.Identificador);
			await execucoesCarga.FinalizarComErroAsync(execucao.Identificador, ex.Message, ex.StackTrace, metricas);
			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Erro);
		}
	}

	private async Task EncadearEnriquecimentoDeAdesaoAsync(
		ServicoCargaBrasil servicoBrasil,
		String modoExecucao,
		MetricasExecucao metricas,
		CancellationToken stoppingToken) {
		logger.LogInformation("Encadeando enriquecimento de atas apos {Modo}", modoExecucao);

		var resultado = await servicoBrasil.EnriquecerIndiceComAtasAsync(stoppingToken);
		metricas.TotalItensIndexados += resultado.DocumentosEnriquecidos;
	}
}
