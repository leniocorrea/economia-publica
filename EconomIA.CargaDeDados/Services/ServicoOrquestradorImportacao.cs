using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Repositories;

namespace EconomIA.CargaDeDados.Services;

public class ServicoOrquestradorImportacao {
	private readonly Orgaos orgaos;
	private readonly ControlesImportacao controlesImportacao;
	private readonly ServicoCarga servicoCarga;
	private readonly ServicoCargaContratosAtas servicoCargaContratosAtas;

	public ServicoOrquestradorImportacao(
		Orgaos orgaos,
		ControlesImportacao controlesImportacao,
		ServicoCarga servicoCarga,
		ServicoCargaContratosAtas servicoCargaContratosAtas) {
		this.orgaos = orgaos;
		this.controlesImportacao = controlesImportacao;
		this.servicoCarga = servicoCarga;
		this.servicoCargaContratosAtas = servicoCargaContratosAtas;
	}

	public async Task ExecutarImportacaoDiariaAsync(String[]? cnpjsFiltro = null, Int32 diasRetroativos = 1) {
		Console.WriteLine("=== Iniciando Importacao Diaria ===");
		Console.WriteLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
		Console.WriteLine($"Dias retroativos: {diasRetroativos}");
		Console.WriteLine();

		var orgaosParaImportar = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
			? await orgaos.ListarPorCnpjsAsync(cnpjsFiltro)
			: await orgaos.ListarTodosAsync();

		if (orgaosParaImportar.Count == 0) {
			Console.WriteLine("Nenhum orgao encontrado para importacao.");
			return;
		}

		Console.WriteLine($"Total de orgaos a processar: {orgaosParaImportar.Count}");
		Console.WriteLine();

		var dataFinal = DateTime.Now;
		var dataInicial = dataFinal.AddDays(-diasRetroativos);

		foreach (var orgao in orgaosParaImportar) {
			Console.WriteLine($"=== Processando: {orgao.RazaoSocial} ({orgao.Cnpj}) ===");

			await ImportarComprasDoOrgaoAsync(orgao, dataInicial, dataFinal);
			await ImportarContratosDoOrgaoAsync(orgao, dataInicial, dataFinal);
			await ImportarAtasDoOrgaoAsync(orgao, dataInicial, dataFinal);

			Console.WriteLine();
		}

		Console.WriteLine("=== Importacao Diaria Finalizada ===");
	}

	public async Task ExecutarImportacaoIncrementalAsync(String[]? cnpjsFiltro = null) {
		Console.WriteLine("=== Iniciando Importacao Incremental ===");
		Console.WriteLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
		Console.WriteLine();

		var orgaosParaImportar = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
			? await orgaos.ListarPorCnpjsAsync(cnpjsFiltro)
			: await orgaos.ListarTodosAsync();

		if (orgaosParaImportar.Count == 0) {
			Console.WriteLine("Nenhum orgao encontrado para importacao.");
			return;
		}

		Console.WriteLine($"Total de orgaos a processar: {orgaosParaImportar.Count}");
		Console.WriteLine();

		var dataFinal = DateTime.Now;

		foreach (var orgao in orgaosParaImportar) {
			Console.WriteLine($"=== Processando: {orgao.RazaoSocial} ({orgao.Cnpj}) ===");

			await ImportarComprasIncrementalAsync(orgao, dataFinal);
			await ImportarContratosIncrementalAsync(orgao, dataFinal);
			await ImportarAtasIncrementalAsync(orgao, dataFinal);

			Console.WriteLine();
		}

		Console.WriteLine("=== Importacao Incremental Finalizada ===");
	}

	private async Task ImportarComprasDoOrgaoAsync(OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal) {
		var tipoDado = TipoDado.Compras;

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

			Console.WriteLine($"  [Compras] Importacao concluida: {dataInicial:dd/MM/yyyy} a {dataFinal:dd/MM/yyyy}");
		} catch (Exception ex) {
			await controlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			Console.WriteLine($"  [Compras] ERRO: {ex.Message}");
		}
	}

	private async Task ImportarComprasIncrementalAsync(OrgaoResumo orgao, DateTime dataFinal) {
		var tipoDado = TipoDado.Compras;
		var proximaData = await controlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			Console.WriteLine($"  [Compras] Primeira importacao - usando ultimos 90 dias");
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			Console.WriteLine($"  [Compras] Ja esta atualizado ate {dataFinal:dd/MM/yyyy}");
			return;
		}

		await ImportarComprasDoOrgaoAsync(orgao, proximaData.Value, dataFinal);
	}

	private async Task ImportarContratosDoOrgaoAsync(OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal) {
		var tipoDado = TipoDado.Contratos;

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

			Console.WriteLine($"  [Contratos] Importacao concluida: {dataInicial:dd/MM/yyyy} a {dataFinal:dd/MM/yyyy}");
		} catch (Exception ex) {
			await controlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			Console.WriteLine($"  [Contratos] ERRO: {ex.Message}");
		}
	}

	private async Task ImportarContratosIncrementalAsync(OrgaoResumo orgao, DateTime dataFinal) {
		var tipoDado = TipoDado.Contratos;
		var proximaData = await controlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			Console.WriteLine($"  [Contratos] Primeira importacao - usando ultimos 90 dias");
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			Console.WriteLine($"  [Contratos] Ja esta atualizado ate {dataFinal:dd/MM/yyyy}");
			return;
		}

		await ImportarContratosDoOrgaoAsync(orgao, proximaData.Value, dataFinal);
	}

	private async Task ImportarAtasDoOrgaoAsync(OrgaoResumo orgao, DateTime dataInicial, DateTime dataFinal) {
		var tipoDado = TipoDado.Atas;

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

			Console.WriteLine($"  [Atas] Importacao concluida: {dataInicial:dd/MM/yyyy} a {dataFinal:dd/MM/yyyy}");
		} catch (Exception ex) {
			await controlesImportacao.FinalizarComErroAsync(orgao.Identificador, tipoDado, ex.Message);
			Console.WriteLine($"  [Atas] ERRO: {ex.Message}");
		}
	}

	private async Task ImportarAtasIncrementalAsync(OrgaoResumo orgao, DateTime dataFinal) {
		var tipoDado = TipoDado.Atas;
		var proximaData = await controlesImportacao.ObterProximaDataParaImportarAsync(orgao.Identificador, tipoDado);

		if (proximaData is null) {
			Console.WriteLine($"  [Atas] Primeira importacao - usando ultimos 90 dias");
			proximaData = dataFinal.AddDays(-90);
		}

		if (proximaData > dataFinal) {
			Console.WriteLine($"  [Atas] Ja esta atualizado ate {dataFinal:dd/MM/yyyy}");
			return;
		}

		await ImportarAtasDoOrgaoAsync(orgao, proximaData.Value, dataFinal);
	}

	public async Task ExibirStatusImportacaoAsync(String[]? cnpjsFiltro = null) {
		Console.WriteLine("=== Status de Importacao ===");
		Console.WriteLine();

		var orgaosParaExibir = cnpjsFiltro is not null && cnpjsFiltro.Length > 0
			? await orgaos.ListarPorCnpjsAsync(cnpjsFiltro)
			: await orgaos.ListarTodosAsync();

		foreach (var orgao in orgaosParaExibir) {
			Console.WriteLine($"{orgao.RazaoSocial} ({orgao.Cnpj}):");

			var controles = await controlesImportacao.ListarPorOrgaoAsync(orgao.Identificador);

			if (!controles.Any()) {
				Console.WriteLine("  Nenhuma importacao registrada");
			} else {
				foreach (var controle in controles) {
					var dataInicial = controle.DataInicialImportada?.ToString("dd/MM/yyyy") ?? "N/A";
					var dataFinal = controle.DataFinalImportada?.ToString("dd/MM/yyyy") ?? "N/A";
					var status = controle.Status.ToUpper();

					Console.WriteLine($"  [{controle.TipoDado}] {dataInicial} a {dataFinal} | Status: {status}");

					if (controle.Status == StatusImportacao.Erro && !String.IsNullOrEmpty(controle.MensagemErro)) {
						Console.WriteLine($"    Erro: {controle.MensagemErro}");
					}
				}
			}

			Console.WriteLine();
		}
	}
}
