using System.Diagnostics;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Observability;
using EconomIA.CargaDeDados.Repositories;
using Microsoft.Extensions.Logging;

namespace EconomIA.CargaDeDados.Services;

public class ServicoOrquestradorImportacao {
	private const Int32 MaxOrgaosEmParalelo = 2;

	private readonly OrgaosMonitorados orgaosMonitorados;
	private readonly ControlesImportacao controlesImportacao;
	private readonly ServicoCarga servicoCarga;
	private readonly ServicoCargaContratosAtas servicoCargaContratosAtas;
	private readonly ILogger<ServicoOrquestradorImportacao> logger;

	public ServicoOrquestradorImportacao(
		OrgaosMonitorados orgaosMonitorados,
		ControlesImportacao controlesImportacao,
		ServicoCarga servicoCarga,
		ServicoCargaContratosAtas servicoCargaContratosAtas,
		ILogger<ServicoOrquestradorImportacao> logger) {
		this.orgaosMonitorados = orgaosMonitorados;
		this.controlesImportacao = controlesImportacao;
		this.servicoCarga = servicoCarga;
		this.servicoCargaContratosAtas = servicoCargaContratosAtas;
		this.logger = logger;
	}

	public async Task ExecutarImportacaoDiariaAsync(MetricasExecucao metricas, String[]? cnpjsFiltro = null, Int32 diasRetroativos = 1, CancellationToken cancellationToken = default) {
		logger.LogInformation("Iniciando importacao diaria. Dias retroativos: {DiasRetroativos}, Paralelismo: {MaxParalelo}", diasRetroativos, MaxOrgaosEmParalelo);

		var orgaosParaImportar = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
			? await orgaosMonitorados.ListarPorCnpjsAsync(cnpjsFiltro)
			: await orgaosMonitorados.ListarAtivosAsync();

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
			var metricaOrgao = metricas.ObterOuCriarMetricasOrgao(orgao.Identificador);
			metricaOrgao.DataInicialProcessada = dataInicial;
			metricaOrgao.DataFinalProcessada = dataFinal;

			logger.LogInformation("Processando orgao: {RazaoSocial} ({Cnpj})", orgao.RazaoSocial, orgao.Cnpj);

			try {
				await ImportarComprasDoOrgaoAsync(orgao, dataInicial, dataFinal, metricaOrgao);
				await ImportarContratosDoOrgaoAsync(orgao, dataInicial, dataFinal, metricaOrgao);
				await ImportarAtasDoOrgaoAsync(orgao, dataInicial, dataFinal, metricaOrgao);
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

		var orgaosParaImportar = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
			? await orgaosMonitorados.ListarPorCnpjsAsync(cnpjsFiltro)
			: await orgaosMonitorados.ListarAtivosAsync();

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
			var metricaOrgao = metricas.ObterOuCriarMetricasOrgao(orgao.Identificador);
			metricaOrgao.DataFinalProcessada = dataFinal;

			logger.LogInformation("Processando orgao: {RazaoSocial} ({Cnpj})", orgao.RazaoSocial, orgao.Cnpj);

			try {
				await ImportarComprasIncrementalAsync(orgao, dataFinal, metricaOrgao);
				await ImportarContratosIncrementalAsync(orgao, dataFinal, metricaOrgao);
				await ImportarAtasIncrementalAsync(orgao, dataFinal, metricaOrgao);
				metricaOrgao.Finalizar("sucesso");
			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao processar orgao {Cnpj}", orgao.Cnpj);
				metricaOrgao.Finalizar("erro", ex.Message);
			}
		});

		logger.LogInformation("Importacao incremental finalizada");
	}

	private async Task ImportarComprasDoOrgaoAsync(OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Compras;
		var cronometro = Stopwatch.StartNew();

		try {
			await controlesImportacao.IniciarImportacaoAsync(orgao.Identificador, tipoDado);

			var dataInicialStr = dataInicial.ToString("yyyyMMdd");
			var dataFinalStr = dataFinal.ToString("yyyyMMdd");

			var parametros = new List<ParametroCarga>();
			for (var modalidade = 1; modalidade <= 14; modalidade++) {
				parametros.Add(new ParametroCarga(dataInicialStr, dataFinalStr, modalidade, orgao.Cnpj, 50));
			}

			await servicoCarga.ProcessarCargaAsync(parametros);

			await controlesImportacao.FinalizarComSucessoAsync(
				orgao.Identificador,
				tipoDado,
				dataInicial,
				dataFinal,
				parametros.Count);

			cronometro.Stop();
			metricaOrgao.ComprasDuracaoMs = cronometro.ElapsedMilliseconds;
			metricaOrgao.ComprasProcessadas++;

			logger.LogDebug("Compras importadas para {Cnpj}: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy} em {Duracao}ms",
				orgao.Cnpj, dataInicial, dataFinal, cronometro.ElapsedMilliseconds);
		} catch (Exception ex) {
			cronometro.Stop();
			metricaOrgao.ComprasDuracaoMs = cronometro.ElapsedMilliseconds;
			await controlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			logger.LogWarning(ex, "Erro ao importar compras do orgao {Cnpj}", orgao.Cnpj);
		}
	}

	private async Task ImportarComprasIncrementalAsync(OrgaoResumo orgao, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Compras;
		var proximaData = await controlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			logger.LogDebug("Primeira importacao de compras para {Cnpj} - usando ultimos 90 dias", orgao.Cnpj);
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			logger.LogDebug("Compras de {Cnpj} ja atualizadas ate {DataFinal:dd/MM/yyyy}", orgao.Cnpj, dataFinal);
			return;
		}

		metricaOrgao.DataInicialProcessada = proximaData;
		await ImportarComprasDoOrgaoAsync(orgao, proximaData.Value, dataFinal, metricaOrgao);
	}

	private async Task ImportarContratosDoOrgaoAsync(OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Contratos;
		var cronometro = Stopwatch.StartNew();

		try {
			await controlesImportacao.IniciarImportacaoAsync(orgao.Identificador, tipoDado);

			var dataInicialStr = dataInicial.ToString("yyyyMMdd");
			var dataFinalStr = dataFinal.ToString("yyyyMMdd");

			await servicoCargaContratosAtas.CarregarContratosAsync(new[] { orgao.Cnpj }, dataInicialStr, dataFinalStr);

			await controlesImportacao.FinalizarComSucessoAsync(
				orgao.Identificador,
				tipoDado,
				dataInicial,
				dataFinal,
				0);

			cronometro.Stop();
			metricaOrgao.ContratosDuracaoMs = cronometro.ElapsedMilliseconds;
			metricaOrgao.ContratosProcessados++;

			logger.LogDebug("Contratos importados para {Cnpj}: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy} em {Duracao}ms",
				orgao.Cnpj, dataInicial, dataFinal, cronometro.ElapsedMilliseconds);
		} catch (Exception ex) {
			cronometro.Stop();
			metricaOrgao.ContratosDuracaoMs = cronometro.ElapsedMilliseconds;
			await controlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			logger.LogWarning(ex, "Erro ao importar contratos do orgao {Cnpj}", orgao.Cnpj);
		}
	}

	private async Task ImportarContratosIncrementalAsync(OrgaoResumo orgao, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Contratos;
		var proximaData = await controlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			logger.LogDebug("Primeira importacao de contratos para {Cnpj} - usando ultimos 90 dias", orgao.Cnpj);
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			logger.LogDebug("Contratos de {Cnpj} ja atualizados ate {DataFinal:dd/MM/yyyy}", orgao.Cnpj, dataFinal);
			return;
		}

		await ImportarContratosDoOrgaoAsync(orgao, proximaData.Value, dataFinal, metricaOrgao);
	}

	private async Task ImportarAtasDoOrgaoAsync(OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Atas;
		var cronometro = Stopwatch.StartNew();

		try {
			await controlesImportacao.IniciarImportacaoAsync(orgao.Identificador, tipoDado);

			var dataInicialStr = dataInicial.ToString("yyyyMMdd");
			var dataFinalStr = dataFinal.ToString("yyyyMMdd");

			await servicoCargaContratosAtas.CarregarAtasAsync(new[] { orgao.Cnpj }, dataInicialStr, dataFinalStr);

			await controlesImportacao.FinalizarComSucessoAsync(
				orgao.Identificador,
				tipoDado,
				dataInicial,
				dataFinal,
				0);

			cronometro.Stop();
			metricaOrgao.AtasDuracaoMs = cronometro.ElapsedMilliseconds;
			metricaOrgao.AtasProcessadas++;

			logger.LogDebug("Atas importadas para {Cnpj}: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy} em {Duracao}ms",
				orgao.Cnpj, dataInicial, dataFinal, cronometro.ElapsedMilliseconds);
		} catch (Exception ex) {
			cronometro.Stop();
			metricaOrgao.AtasDuracaoMs = cronometro.ElapsedMilliseconds;
			await controlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			logger.LogWarning(ex, "Erro ao importar atas do orgao {Cnpj}", orgao.Cnpj);
		}
	}

	private async Task ImportarAtasIncrementalAsync(OrgaoResumo orgao, DateTime dataFinal, MetricasOrgao metricaOrgao) {
		var tipoDado = TipoDado.Atas;
		var proximaData = await controlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			logger.LogDebug("Primeira importacao de atas para {Cnpj} - usando ultimos 90 dias", orgao.Cnpj);
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			logger.LogDebug("Atas de {Cnpj} ja atualizadas ate {DataFinal:dd/MM/yyyy}", orgao.Cnpj, dataFinal);
			return;
		}

		await ImportarAtasDoOrgaoAsync(orgao, proximaData.Value, dataFinal, metricaOrgao);
	}

	public async Task ExibirStatusImportacaoAsync(String[]? cnpjsFiltro = null) {
		logger.LogInformation("=== Status de Importacao (Orgaos Monitorados) ===");

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
}
