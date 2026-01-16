using System.Net.Http.Json;
using EconomIA.CargaDeDados.Dtos;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Repositories;

namespace EconomIA.CargaDeDados.Services;

public class ServicoCargaContratosAtas {
	private readonly HttpClient httpClient;
	private readonly Orgaos orgaos;
	private readonly Contratos contratos;
	private readonly Atas atas;

	public ServicoCargaContratosAtas(HttpClient httpClient, Orgaos orgaos, Contratos contratos, Atas atas) {
		this.httpClient = httpClient;
		this.orgaos = orgaos;
		this.contratos = contratos;
		this.atas = atas;
	}

	public async Task<Int32> CarregarContratosAsync(String[] cnpjs, String dataInicial, String dataFinal) {
		Console.WriteLine("=== Carga de Contratos do PNCP ===");

		var totalContratos = 0;

		foreach (var cnpj in cnpjs) {
			Console.Write($"[{cnpj}] ");

			var contratosDoOrgao = 0;
			var pagina = 1;

			while (true) {
				try {
					var url = $"https://pncp.gov.br/api/consulta/v1/contratos?dataInicial={dataInicial}&dataFinal={dataFinal}&cnpjOrgao={cnpj}&pagina={pagina}";
					var response = await httpClient.GetAsync(url);

					if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
						break;
					}

					if (!response.IsSuccessStatusCode) {
						Console.WriteLine($"Erro: {response.StatusCode}");
						break;
					}

					var resultado = await response.Content.ReadFromJsonAsync<PncpContratosResponse>();

					if (resultado == null || resultado.Data == null || resultado.Data.Count == 0) {
						break;
					}

					foreach (var contratoDto in resultado.Data) {
						var idOrgao = await ObterOuCriarOrgaoAsync(contratoDto.OrgaoEntidade);

						var contrato = new Contrato {
							IdentificadorDoOrgao = idOrgao,
							NumeroControlePncp = contratoDto.NumeroControlePncp,
							NumeroControlePncpCompra = contratoDto.NumeroControlePncpCompra,
							AnoContrato = contratoDto.AnoContrato,
							SequencialContrato = contratoDto.SequencialContrato,
							NumeroContratoEmpenho = contratoDto.NumeroContratoEmpenho,
							Processo = contratoDto.Processo,
							ObjetoContrato = contratoDto.ObjetoContrato,
							TipoContratoId = contratoDto.TipoContrato?.Id,
							TipoContratoNome = contratoDto.TipoContrato?.Nome,
							CategoriaProcessoId = contratoDto.CategoriaProcesso?.Id,
							CategoriaProcessoNome = contratoDto.CategoriaProcesso?.Nome,
							NiFornecedor = contratoDto.NiFornecedor,
							NomeRazaoSocialFornecedor = contratoDto.NomeRazaoSocialFornecedor,
							TipoPessoa = contratoDto.TipoPessoa,
							ValorInicial = contratoDto.ValorInicial,
							ValorGlobal = contratoDto.ValorGlobal,
							ValorParcela = contratoDto.ValorParcela,
							ValorAcumulado = contratoDto.ValorAcumulado,
							NumeroParcelas = contratoDto.NumeroParcelas,
							DataAssinatura = contratoDto.DataAssinatura,
							DataVigenciaInicio = contratoDto.DataVigenciaInicio,
							DataVigenciaFim = contratoDto.DataVigenciaFim,
							DataPublicacaoPncp = contratoDto.DataPublicacaoPncp,
							DataAtualizacao = contratoDto.DataAtualizacao,
							DataAtualizacaoGlobal = contratoDto.DataAtualizacaoGlobal,
							Receita = contratoDto.Receita,
							InformacaoComplementar = contratoDto.InformacaoComplementar,
							UsuarioNome = contratoDto.UsuarioNome
						};

						await contratos.UpsertAsync(contrato);
						contratosDoOrgao++;
						totalContratos++;
					}

					if (resultado.PaginasRestantes == 0) {
						break;
					}

					pagina++;
				} catch (Exception ex) {
					Console.WriteLine($"Erro na pagina {pagina}: {ex.Message}");
					break;
				}
			}

			Console.WriteLine($"{contratosDoOrgao} contratos");
		}

		Console.WriteLine($"=== Total: {totalContratos} contratos importados ===");

		return totalContratos;
	}

	public async Task<Int32> CarregarAtasAsync(String[] cnpjs, String dataInicial, String dataFinal) {
		Console.WriteLine("=== Carga de Atas de Registro de Preco do PNCP ===");

		var totalAtas = 0;

		foreach (var cnpj in cnpjs) {
			Console.Write($"[{cnpj}] ");

			var atasDoOrgao = 0;
			var pagina = 1;

			while (true) {
				try {
					var url = $"https://pncp.gov.br/api/consulta/v1/atas?dataInicial={dataInicial}&dataFinal={dataFinal}&cnpj={cnpj}&pagina={pagina}";
					var response = await httpClient.GetAsync(url);

					if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
						break;
					}

					if (!response.IsSuccessStatusCode) {
						Console.WriteLine($"Erro: {response.StatusCode}");
						break;
					}

					var resultado = await response.Content.ReadFromJsonAsync<PncpAtasResponse>();

					if (resultado == null || resultado.Data == null || resultado.Data.Count == 0) {
						break;
					}

					foreach (var ataDto in resultado.Data) {
						var idOrgao = await ObterOuCriarOrgaoAsync(new PncpOrgaoDto(
							ataDto.CnpjOrgao,
							ataDto.NomeOrgao ?? "",
							null,
							null
						));

						var ata = new Ata {
							IdentificadorDoOrgao = idOrgao,
							NumeroControlePncpAta = ataDto.NumeroControlePncpAta,
							NumeroControlePncpCompra = ataDto.NumeroControlePncpCompra,
							NumeroAtaRegistroPreco = ataDto.NumeroAtaRegistroPreco,
							AnoAta = ataDto.AnoAta,
							ObjetoContratacao = ataDto.ObjetoContratacao,
							Cancelado = ataDto.Cancelado,
							DataCancelamento = ataDto.DataCancelamento,
							DataAssinatura = ataDto.DataAssinatura,
							VigenciaInicio = ataDto.VigenciaInicio,
							VigenciaFim = ataDto.VigenciaFim,
							DataPublicacaoPncp = ataDto.DataPublicacaoPncp,
							DataInclusao = ataDto.DataInclusao,
							DataAtualizacao = ataDto.DataAtualizacao,
							DataAtualizacaoGlobal = ataDto.DataAtualizacaoGlobal,
							Usuario = ataDto.Usuario
						};

						await this.atas.UpsertAsync(ata);
						atasDoOrgao++;
						totalAtas++;
					}

					if (resultado.PaginasRestantes == 0) {
						break;
					}

					pagina++;
				} catch (Exception ex) {
					Console.WriteLine($"Erro na pagina {pagina}: {ex.Message}");
					break;
				}
			}

			Console.WriteLine($"{atasDoOrgao} atas");
		}

		Console.WriteLine($"=== Total: {totalAtas} atas importadas ===");

		return totalAtas;
	}

	private async Task<Int64> ObterOuCriarOrgaoAsync(PncpOrgaoDto orgaoDto) {
		var orgao = new Orgao {
			Cnpj = orgaoDto.Cnpj,
			RazaoSocial = orgaoDto.RazaoSocial
		};

		return await orgaos.UpsertAsync(orgao);
	}
}
