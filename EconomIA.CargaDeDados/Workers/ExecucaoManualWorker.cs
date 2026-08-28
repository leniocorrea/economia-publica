using EconomIA.CargaDeDados.Configuration;
using EconomIA.CargaDeDados.HealthChecks;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Observability;
using EconomIA.CargaDeDados.Repositories;
using EconomIA.CargaDeDados.Services;
using Microsoft.Extensions.Options;

namespace EconomIA.CargaDeDados.Workers;

public class ExecucaoManualWorker : BackgroundService {
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<ExecucaoManualWorker> logger;
	private readonly WorkerConfiguration configuracao;
	private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromSeconds(10);

	public ExecucaoManualWorker(
		IServiceScopeFactory scopeFactory,
		ILogger<ExecucaoManualWorker> logger,
		IOptions<WorkerConfiguration> configuracao) {
		this.scopeFactory = scopeFactory;
		this.logger = logger;
		this.configuracao = configuracao.Value;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Worker de execucao manual iniciado. Verificando pendentes a cada {Intervalo} segundos", IntervaloVerificacao.TotalSeconds);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				await VerificarEProcessarPendentesAsync(stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao verificar execucoes pendentes");
			}

			await Task.Delay(IntervaloVerificacao, stoppingToken);
		}

		logger.LogInformation("Worker de execucao manual finalizado");
	}

	private async Task VerificarEProcessarPendentesAsync(CancellationToken stoppingToken) {
		using var scope = scopeFactory.CreateScope();
		var servicos = scope.ServiceProvider;

		var execucoesCarga = servicos.GetRequiredService<ExecucoesCarga>();

		var execucaoAtiva = await execucoesCarga.ObterExecucaoEmAndamentoAsync(configuracao.TimeoutExecucaoHoras);

		if (execucaoAtiva is not null) {
			logger.LogDebug(
				"Execucao em andamento detectada (ID: {ExecucaoId}). Aguardando finalizacao.",
				execucaoAtiva.Identificador);
			return;
		}

		var execucaoPendente = await execucoesCarga.ObterProximaPendenteAsync();

		if (execucaoPendente is null) {
			return;
		}

		logger.LogInformation("Execucao pendente encontrada. ID: {ExecucaoId}, Modo: {Modo}",
			execucaoPendente.Identificador, execucaoPendente.ModoExecucao);

		await ProcessarExecucaoAsync(execucaoPendente, stoppingToken);
	}

	private async Task ProcessarExecucaoAsync(ExecucaoCarga execucao, CancellationToken stoppingToken) {
		using var scope = scopeFactory.CreateScope();
		var servicos = scope.ServiceProvider;

		var execucoesCarga = servicos.GetRequiredService<ExecucoesCarga>();
		var orquestrador = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
		var clienteNotificacoes = servicos.GetRequiredService<IClienteDeNotificacoes>();

		await execucoesCarga.IniciarProcessamentoAsync(execucao.Identificador);
		await clienteNotificacoes.NotificarExecucaoIniciadaAsync(execucao);

		WorkerHealthCheck.RegistrarInicioExecucao(execucao.Identificador);

		var metricas = new MetricasExecucao();
		var parametros = execucao.Parametros;

		try {
			logger.LogInformation("Iniciando processamento. Execucao ID: {ExecucaoId}, Modo: {Modo}, CNPJs: {Cnpjs}, DiasRetroativos: {Dias}",
				execucao.Identificador,
				execucao.ModoExecucao,
				parametros?.Cnpjs?.Length ?? 0,
				parametros?.DiasRetroativos);

			if (execucao.ModoExecucao == ModoExecucao.Incremental) {
				await orquestrador.ExecutarImportacaoIncrementalAsync(metricas, parametros?.Cnpjs, stoppingToken);
			} else if (execucao.ModoExecucao == ModoExecucao.Diaria) {
				var diasRetroativos = parametros?.DiasRetroativos ?? 1;
				await orquestrador.ExecutarImportacaoDiariaAsync(metricas, parametros?.Cnpjs, diasRetroativos, stoppingToken);
			} else if (execucao.ModoExecucao == ModoExecucao.Brasil) {
				var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();
				var diasRetroativos = parametros?.DiasRetroativos ?? 1;
				var dataFinal = DateTime.Now;
				var dataInicial = dataFinal.AddDays(-diasRetroativos);

				var resultado = await servicoBrasil.ProcessarCargaCompletaAsync(
					dataInicial, dataFinal,
					apenasModalidadesComDados: true,
					carregarCompras: parametros?.CarregarCompras ?? true,
					carregarContratos: parametros?.CarregarContratos ?? true,
					carregarAtas: parametros?.CarregarAtas ?? true,
					cancellationToken: stoppingToken);

				metricas.TotalComprasProcessadas = resultado.ComprasProcessadas;
				metricas.TotalItensIndexados = resultado.ItensIndexados;
				metricas.TotalContratosProcessados = resultado.ContratosProcessados;
				metricas.TotalAtasProcessadas = resultado.AtasProcessadas;
				metricas.TotalOrgaosProcessados = resultado.OrgaosProcessados;
			} else if (execucao.ModoExecucao == ModoExecucao.Reconciliacao) {
				var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();

				var resultado = await servicoBrasil.ReconciliarAtasOrfasAsync(stoppingToken);

				metricas.TotalComprasProcessadas = resultado.ComprasProcessadas;
				metricas.TotalItensIndexados = resultado.ItensIndexados;
				metricas.TotalOrgaosProcessados = resultado.OrgaosProcessados;
			} else if (execucao.ModoExecucao == ModoExecucao.Enriquecimento) {
				var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();

				var resultado = await servicoBrasil.EnriquecerIndiceComAtasAsync(stoppingToken);

				metricas.TotalItensIndexados = resultado.DocumentosEnriquecidos;
			} else if (execucao.ModoExecucao == ModoExecucao.EnriquecimentoObjeto) {
				var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();

				var resultado = await servicoBrasil.EnriquecerIndiceComObjetoDaCompraAsync(stoppingToken);

				metricas.TotalItensIndexados = resultado.DocumentosEnriquecidos;
			} else {
				throw new InvalidOperationException($"Modo de execucao nao suportado: {execucao.ModoExecucao}");
			}

			if (execucao.ModoExecucao != ModoExecucao.Enriquecimento && execucao.ModoExecucao != ModoExecucao.EnriquecimentoObjeto) {
				var servicoEnriquecimento = servicos.GetRequiredService<ServicoCargaBrasil>();

				logger.LogInformation("Encadeando enriquecimento de atas apos {Modo}", execucao.ModoExecucao);
				var resultadoEnriquecimento = await servicoEnriquecimento.EnriquecerIndiceComAtasAsync(stoppingToken);
				metricas.TotalItensIndexados += resultadoEnriquecimento.DocumentosEnriquecidos;
			}

			await execucoesCarga.FinalizarComSucessoAsync(execucao.Identificador, metricas);
			await clienteNotificacoes.NotificarExecucaoFinalizadaAsync(execucao, metricas);

			logger.LogInformation(
				"Execucao manual finalizada. ID: {ExecucaoId}, Orgaos: {TotalOrgaos}, Compras: {TotalCompras}, Contratos: {TotalContratos}, Atas: {TotalAtas}, Duracao: {Duracao}ms",
				execucao.Identificador,
				metricas.TotalOrgaosProcessados,
				metricas.TotalComprasProcessadas,
				metricas.TotalContratosProcessados,
				metricas.TotalAtasProcessadas,
				metricas.DuracaoTotalMs);

			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Sucesso);
		} catch (OperationCanceledException) {
			logger.LogWarning("Execucao manual cancelada. ID: {ExecucaoId}", execucao.Identificador);
			await execucoesCarga.FinalizarComCancelamentoAsync(execucao.Identificador, metricas);
			await clienteNotificacoes.NotificarExecucaoErroAsync(execucao, metricas, "Execução cancelada");
			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Cancelado);
		} catch (Exception ex) {
			logger.LogError(ex, "Erro na execucao manual. ID: {ExecucaoId}", execucao.Identificador);
			await execucoesCarga.FinalizarComErroAsync(execucao.Identificador, ex.Message, ex.StackTrace, metricas);
			await clienteNotificacoes.NotificarExecucaoErroAsync(execucao, metricas, ex.Message);
			WorkerHealthCheck.RegistrarFimExecucao(StatusExecucao.Erro);
		}
	}
}
