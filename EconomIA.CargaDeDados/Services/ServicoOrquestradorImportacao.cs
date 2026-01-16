using System.Diagnostics;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Observability;
using EconomIA.CargaDeDados.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EconomIA.CargaDeDados.Services;

public class ServicoOrquestradorImportacao {
	private const Int32 MaxOrgaosEmParalelo = 5;

	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<ServicoOrquestradorImportacao> logger;

	public ServicoOrquestradorImportacao(
		IServiceScopeFactory scopeFactory,
		ILogger<ServicoOrquestradorImportacao> logger) {
		this.scopeFactory = scopeFactory;
		this.logger = logger;
	}

	public async Task ExecutarImportacaoDiariaAsync(MetricasExecucao metricas, String[]? cnpjsFiltro = null, Int32 diasRetroativos = 1, CancellationToken cancellationToken = default) {
		logger.LogInformation("Iniciando importacao diaria. Dias retroativos: {DiasRetroativos}, Paralelismo: {MaxParalelo}", diasRetroativos, MaxOrgaosEmParalelo);

		List<OrgaoResumo> orgaosParaImportar;
		using (var scope = scopeFactory.CreateScope()) {
			var orgaosMonitorados = scope.ServiceProvider.GetRequiredService<OrgaosMonitorados>();
			orgaosParaImportar = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
				? await orgaosMonitorados.ListarPorCnpjsAsync(cnpjsFiltro)
				: await orgaosMonitorados.ListarAtivosAsync();
		}

		if (orgaosParaImportar.Count == 0) {
			logger.LogWarning("Nenhum orgao monitorado encontrado para importacao");
			return;
		}

		logger.LogInformation("Total de orgaos monitorados a processar: {TotalOrgaos}", orgaosParaImportar.Count);

		var dataFinal = DateTime.Now;
		var dataInicial = dataFinal.AddDays(-diasRetroativos);

		var opcoes = new ParallelOptions {
			MaxDegreeOfParallelism = MaxOrgaosEmParalelo,
			CancellationToken = cancellationToken
		};

		await Parallel.ForEachAsync(orgaosParaImportar, opcoes, async (orgao, token) => {
			using var scope = scopeFactory.CreateScope();
			var servicosDoScope = new ServicosDoScope(scope.ServiceProvider);

			var metricaOrgao = metricas.ObterOuCriarMetricasOrgao(orgao.Identificador);
			metricaOrgao.DataInicialProcessada = dataInicial;
			metricaOrgao.DataFinalProcessada = dataFinal;

			logger.LogInformation("Processando orgao: {RazaoSocial} ({Cnpj})", orgao.RazaoSocial, orgao.Cnpj);

			try {
				await ImportarComprasDoOrgaoAsync(servicosDoScope, orgao, dataInicial, dataFinal, metricaOrgao);
				await ImportarContratosDoOrgaoAsync(servicosDoScope, orgao, dataInicial, dataFinal, metricaOrgao);
				await ImportarAtasDoOrgaoAsync(servicosDoScope, orgao, dataInicial, dataFinal, metricaOrgao);
				metricaOrgao.Finalizar("sucesso");
			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao processar orgao {Cnpj}", orgao.Cnpj);
				metricaOrgao.Finalizar("erro", ex.Message);
			}
		});

		logger.LogInformation("Importacao diaria finalizada");
	}

	public async Task ExecutarImportacaoIncrementalAsync(MetricasExecucao metricas, String[]? cnpjsFiltro = null, CancellationToken cancellationToken = default) {
		logger.LogInformation("Iniciando importacao incremental. Paralelismo: {MaxParalelo}", MaxOrgaosEmParalelo);

		List<OrgaoResumo> orgaosParaImportar;
		using (var scope = scopeFactory.CreateScope()) {
			var orgaosMonitorados = scope.ServiceProvider.GetRequiredService<OrgaosMonitorados>();
			orgaosParaImportar = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
				? await orgaosMonitorados.ListarPorCnpjsAsync(cnpjsFiltro)
				: await orgaosMonitorados.ListarAtivosAsync();
		}

		if (orgaosParaImportar.Count == 0) {
			logger.LogWarning("Nenhum orgao monitorado encontrado para importacao");
			return;
		}

		logger.LogInformation("Total de orgaos monitorados a processar: {TotalOrgaos}", orgaosParaImportar.Count);

		var dataFinal = DateTime.Now;

		var opcoes = new ParallelOptions {
			MaxDegreeOfParallelism = MaxOrgaosEmParalelo,
			CancellationToken = cancellationToken
		};

		await Parallel.ForEachAsync(orgaosParaImportar, opcoes, async (orgao, token) => {
			using var scope = scopeFactory.CreateScope();
			var servicosDoScope = new ServicosDoScope(scope.ServiceProvider);

			var metricaOrgao = metricas.ObterOuCriarMetricasOrgao(orgao.Identificador);
			metricaOrgao.DataFinalProcessada = dataFinal;

			logger.LogInformation("Processando orgao: {RazaoSocial} ({Cnpj})", orgao.RazaoSocial, orgao.Cnpj);

			try {
				await ImportarComprasIncrementalAsync(servicosDoScope, orgao, dataFinal, metricaOrgao);
				await ImportarContratosIncrementalAsync(servicosDoScope, orgao, dataFinal, metricaOrgao);
				await ImportarAtasIncrementalAsync(servicosDoScope, orgao, dataFinal, metricaOrgao);
				metricaOrgao.Finalizar("sucesso");
			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao processar orgao {Cnpj}", orgao.Cnpj);
				metricaOrgao.Finalizar("erro", ex.Message);
			}
		});

		logger.LogInformation("Importacao incremental finalizada");
	}

	private async Task ImportarComprasDoOrgaoAsync(ServicosDoScope servicos, OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Compras;
		var cronometro = Stopwatch.StartNew();

		try {
			await servicos.ControlesImportacao.IniciarImportacaoAsync(orgao.Identificador, tipoDado);

			var dataInicialStr = dataInicial.ToString("yyyyMMdd");
			var dataFinalStr = dataFinal.ToString("yyyyMMdd");

			var parametros = new List<ParametroCarga>();
			for (var modalidade = 1; modalidade <= 14; modalidade++) {
				parametros.Add(new ParametroCarga(dataInicialStr, dataFinalStr, modalidade, orgao.Cnpj, 50));
			}

			var resultado = await servicos.ServicoCarga.ProcessarCargaAsync(parametros);

			await servicos.ControlesImportacao.FinalizarComSucessoAsync(
				orgao.Identificador,
				tipoDado,
				dataInicial,
				dataFinal,
				resultado.ComprasProcessadas);

			cronometro.Stop();
			metricaOrgao.ComprasDuracaoMs = cronometro.ElapsedMilliseconds;
			metricaOrgao.ComprasProcessadas += resultado.ComprasProcessadas;
			metricaOrgao.ItensProcessados += resultado.ItensIndexados;

			logger.LogDebug("Compras importadas para {Cnpj}: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy} em {Duracao}ms",
				orgao.Cnpj, dataInicial, dataFinal, cronometro.ElapsedMilliseconds);
		} catch (Exception ex) {
			cronometro.Stop();
			metricaOrgao.ComprasDuracaoMs = cronometro.ElapsedMilliseconds;
			await servicos.ControlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			logger.LogWarning(ex, "Erro ao importar compras do orgao {Cnpj}", orgao.Cnpj);
		}
	}

	private async Task ImportarComprasIncrementalAsync(ServicosDoScope servicos, OrgaoResumo orgao, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Compras;
		var proximaData = await servicos.ControlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			logger.LogDebug("Primeira importacao de compras para {Cnpj} - usando ultimos 90 dias", orgao.Cnpj);
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			logger.LogDebug("Compras de {Cnpj} ja atualizadas ate {DataFinal:dd/MM/yyyy}", orgao.Cnpj, dataFinal);
			return;
		}

		metricaOrgao.DataInicialProcessada = proximaData;
		await ImportarComprasDoOrgaoAsync(servicos, orgao, proximaData.Value, dataFinal, metricaOrgao);
	}

	private async Task ImportarContratosDoOrgaoAsync(ServicosDoScope servicos, OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Contratos;
		var cronometro = Stopwatch.StartNew();

		try {
			await servicos.ControlesImportacao.IniciarImportacaoAsync(orgao.Identificador, tipoDado);

			var dataInicialStr = dataInicial.ToString("yyyyMMdd");
			var dataFinalStr = dataFinal.ToString("yyyyMMdd");

			var totalContratos = await servicos.ServicoCargaContratosAtas.CarregarContratosAsync(new[] { orgao.Cnpj }, dataInicialStr, dataFinalStr);

			await servicos.ControlesImportacao.FinalizarComSucessoAsync(
				orgao.Identificador,
				tipoDado,
				dataInicial,
				dataFinal,
				totalContratos);

			cronometro.Stop();
			metricaOrgao.ContratosDuracaoMs = cronometro.ElapsedMilliseconds;
			metricaOrgao.ContratosProcessados += totalContratos;

			logger.LogDebug("Contratos importados para {Cnpj}: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy} em {Duracao}ms",
				orgao.Cnpj, dataInicial, dataFinal, cronometro.ElapsedMilliseconds);
		} catch (Exception ex) {
			cronometro.Stop();
			metricaOrgao.ContratosDuracaoMs = cronometro.ElapsedMilliseconds;
			await servicos.ControlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			logger.LogWarning(ex, "Erro ao importar contratos do orgao {Cnpj}", orgao.Cnpj);
		}
	}

	private async Task ImportarContratosIncrementalAsync(ServicosDoScope servicos, OrgaoResumo orgao, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Contratos;
		var proximaData = await servicos.ControlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			logger.LogDebug("Primeira importacao de contratos para {Cnpj} - usando ultimos 90 dias", orgao.Cnpj);
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			logger.LogDebug("Contratos de {Cnpj} ja atualizados ate {DataFinal:dd/MM/yyyy}", orgao.Cnpj, dataFinal);
			return;
		}

		await ImportarContratosDoOrgaoAsync(servicos, orgao, proximaData.Value, dataFinal, metricaOrgao);
	}

	private async Task ImportarAtasDoOrgaoAsync(ServicosDoScope servicos, OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Atas;
		var cronometro = Stopwatch.StartNew();

		try {
			await servicos.ControlesImportacao.IniciarImportacaoAsync(orgao.Identificador, tipoDado);

			var dataInicialStr = dataInicial.ToString("yyyyMMdd");
			var dataFinalStr = dataFinal.ToString("yyyyMMdd");

			var totalAtas = await servicos.ServicoCargaContratosAtas.CarregarAtasAsync(new[] { orgao.Cnpj }, dataInicialStr, dataFinalStr);

			await servicos.ControlesImportacao.FinalizarComSucessoAsync(
				orgao.Identificador,
				tipoDado,
				dataInicial,
				dataFinal,
				totalAtas);

			cronometro.Stop();
			metricaOrgao.AtasDuracaoMs = cronometro.ElapsedMilliseconds;
			metricaOrgao.AtasProcessadas += totalAtas;

			logger.LogDebug("Atas importadas para {Cnpj}: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy} em {Duracao}ms",
				orgao.Cnpj, dataInicial, dataFinal, cronometro.ElapsedMilliseconds);
		} catch (Exception ex) {
			cronometro.Stop();
			metricaOrgao.AtasDuracaoMs = cronometro.ElapsedMilliseconds;
			await servicos.ControlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			logger.LogWarning(ex, "Erro ao importar atas do orgao {Cnpj}", orgao.Cnpj);
		}
	}

	private async Task ImportarAtasIncrementalAsync(ServicosDoScope servicos, OrgaoResumo orgao, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Atas;
		var proximaData = await servicos.ControlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			logger.LogDebug("Primeira importacao de atas para {Cnpj} - usando ultimos 90 dias", orgao.Cnpj);
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			logger.LogDebug("Atas de {Cnpj} ja atualizadas ate {DataFinal:dd/MM/yyyy}", orgao.Cnpj, dataFinal);
			return;
		}

		await ImportarAtasDoOrgaoAsync(servicos, orgao, proximaData.Value, dataFinal, metricaOrgao);
	}

	public async Task ExibirStatusImportacaoAsync(String[]? cnpjsFiltro = null) {
		logger.LogInformation("=== Status de Importacao (Orgaos Monitorados) ===");

		using var scope = scopeFactory.CreateScope();
		var orgaosMonitorados = scope.ServiceProvider.GetRequiredService<OrgaosMonitorados>();
		var controlesImportacao = scope.ServiceProvider.GetRequiredService<ControlesImportacao>();

		var orgaosParaExibir = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
			? await orgaosMonitorados.ListarPorCnpjsAsync(cnpjsFiltro)
			: await orgaosMonitorados.ListarAtivosAsync();

		foreach (var orgao in orgaosParaExibir) {
			Console.WriteLine($"{orgao.RazaoSocial} ({orgao.Cnpj}):");

			var controles = await controlesImportacao.ListarPorOrgaoAsync(orgao.Identificador);

			if (!controles.Any()) {
				Console.WriteLine("  Nenhuma importacao registrada");
			} else {
				foreach (var controle in controles) {
					var dataInicial = controle.DataInicialImportada?.ToString("dd/MM/yyyy") ?? "N/A";
					var dataFinalControle = controle.DataFinalImportada?.ToString("dd/MM/yyyy") ?? "N/A";
					var status = controle.Status.ToUpper();

					Console.WriteLine($"  [{controle.TipoDado}] {dataInicial} a {dataFinalControle} | Status: {status}");

					if (controle.Status == StatusImportacao.Erro && !String.IsNullOrEmpty(controle.MensagemErro)) {
						Console.WriteLine($"    Erro: {controle.MensagemErro}");
					}
				}
			}

			Console.WriteLine();
		}
	}

	private class ServicosDoScope {
		public ControlesImportacao ControlesImportacao { get; }
		public ServicoCarga ServicoCarga { get; }
		public ServicoCargaContratosAtas ServicoCargaContratosAtas { get; }

		public ServicosDoScope(IServiceProvider serviceProvider) {
			ControlesImportacao = serviceProvider.GetRequiredService<ControlesImportacao>();
			ServicoCarga = serviceProvider.GetRequiredService<ServicoCarga>();
			ServicoCargaContratosAtas = serviceProvider.GetRequiredService<ServicoCargaContratosAtas>();
		}
	}
}
