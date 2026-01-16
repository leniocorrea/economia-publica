using System.Collections.Concurrent;
using System.Net.Http.Json;
using EconomIA.CargaDeDados.Dtos;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.CargaDeDados.Services;

public record ParametroCarga(String DataInicial, String DataFinal, Int32 CodigoModalidadeContratacao, String? Cnpj, Int32 TamanhoPagina);

public record ResultadoCarga(Int32 ComprasProcessadas, Int32 ItensIndexados);

public class ServicoCarga {
	private readonly HttpClient httpClient;
	private readonly Elastic.Clients.Elasticsearch.ElasticsearchClient elasticClient;
	private readonly IServiceScopeFactory scopeFactory;

	public ServicoCarga(
		HttpClient httpClient,
		Elastic.Clients.Elasticsearch.ElasticsearchClient elasticClient,
		IServiceScopeFactory scopeFactory) {
		this.httpClient = httpClient;
		this.elasticClient = elasticClient;
		this.scopeFactory = scopeFactory;
	}

	public async Task<ResultadoCarga> ProcessarCargaAsync(List<ParametroCarga> parametros) {
		var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
		var totalCompras = 0;
		var totalItensIndexados = 0;

		foreach (var param in parametros) {
			try {
				Console.WriteLine($"Iniciando carga para Modalidade: {param.CodigoModalidadeContratacao}, CNPJ: {param.Cnpj ?? "Todos"}, Data: {param.DataInicial} a {param.DataFinal}");

				var paginaAtual = 1;
				var totalPaginas = 1;

				do {
					var url = $"https://pncp.gov.br/api/consulta/v1/contratacoes/publicacao?dataInicial={param.DataInicial}&dataFinal={param.DataFinal}&codigoModalidadeContratacao={param.CodigoModalidadeContratacao}&pagina={paginaAtual}&tamanhoPagina={param.TamanhoPagina}";

					if (!String.IsNullOrEmpty(param.Cnpj)) {
						url += $"&cnpj={param.Cnpj}";
					}

					using var responseMessage = await httpClient.GetAsync(url);

					if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent) {
						if (paginaAtual == 1) Console.WriteLine("Nenhum dado encontrado (204).");
						break;
					}

					if (!responseMessage.IsSuccessStatusCode) {
						Console.WriteLine($"Erro na requisição: {responseMessage.StatusCode}");
						break;
					}

					var resposta = await responseMessage.Content.ReadFromJsonAsync<PncpResponse>();

					if (resposta == null || resposta.Data == null || resposta.Empty) {
						if (paginaAtual == 1) Console.WriteLine("Nenhum dado encontrado.");
						break;
					}

					totalPaginas = resposta.TotalPaginas;
					Console.WriteLine($"Processando página {paginaAtual} de {totalPaginas} ({resposta.Data.Count} compras)...");

					await Parallel.ForEachAsync(resposta.Data, parallelOptions, async (item, token) => {
						using var scope = scopeFactory.CreateScope();
						var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
						var compras = scope.ServiceProvider.GetRequiredService<Compras>();
						var itensDaCompra = scope.ServiceProvider.GetRequiredService<ItensDaCompra>();
						var resultadosItens = scope.ServiceProvider.GetRequiredService<ResultadosItens>();

						try {
							var modeloOrgao = new Orgao {
								Cnpj = item.OrgaoEntidade.Cnpj,
								RazaoSocial = item.OrgaoEntidade.RazaoSocial
							};

							var idOrgao = await orgaosRepo.UpsertAsync(modeloOrgao);

							var modeloCompra = new Compra {
								IdentificadorDoOrgao = idOrgao,
								NumeroControlePncp = item.NumeroControlePncp,
								AnoCompra = item.AnoCompra,
								SequencialCompra = item.SequencialCompra,
								ModalidadeIdentificador = item.ModalidadeId,
								ModalidadeNome = item.ModalidadeNome,
								ObjetoCompra = item.ObjetoCompra,
								ValorTotalEstimado = item.ValorTotalEstimado,
								ValorTotalHomologado = item.ValorTotalHomologado,
								SituacaoCompraNome = item.SituacaoCompraNome,
								DataInclusao = item.DataInclusao,
								DataAberturaProposta = item.DataAberturaProposta,
								DataEncerramentoProposta = item.DataEncerramentoProposta,
								AmparoLegalNome = item.AmparoLegal?.Nome,
								ModoDisputaNome = item.ModoDisputaNome,
								LinkPncp = item.LinkSistemaOrigem,
								DataAtualizacaoGlobal = item.DataAtualizacaoGlobal
							};

							var idCompra = await compras.UpsertAsync(modeloCompra);
							Interlocked.Increment(ref totalCompras);

							try {
								var urlItens = $"https://pncp.gov.br/api/pncp/v1/orgaos/{item.OrgaoEntidade.Cnpj}/compras/{item.AnoCompra}/{item.SequencialCompra}/itens";
								var itens = await httpClient.GetFromJsonAsync<List<PncpItemDto>>(urlItens, token);

								if (itens is not null && itens.Count > 0) {
									foreach (var itemDto in itens) {
										var modeloItem = new ItemDaCompra {
											IdentificadorDaCompra = idCompra,
											NumeroItem = itemDto.NumeroItem,
											Descricao = itemDto.Descricao,
											Quantidade = itemDto.Quantidade,
											UnidadeMedida = itemDto.UnidadeMedida,
											ValorUnitarioEstimado = itemDto.ValorUnitarioEstimado,
											ValorTotal = itemDto.ValorTotal,
											CriterioJulgamentoNome = itemDto.CriterioJulgamentoNome,
											SituacaoCompraItemNome = itemDto.SituacaoCompraItemNome,
											TemResultado = itemDto.TemResultado
										};
										var idItem = await itensDaCompra.UpsertAsync(modeloItem);

										try {
											var doc = new ItemDocument {
												Id = idItem,
												Descricao = itemDto.Descricao ?? "",
												Valor = itemDto.ValorUnitarioEstimado ?? 0,
												Orgao = item.OrgaoEntidade.RazaoSocial ?? "",
												Data = item.DataAberturaProposta ?? DateTime.MinValue,
												DataInclusao = item.DataInclusao,
												UfSigla = item.UnidadeOrgao?.UfSigla
											};
											await elasticClient.IndexAsync(doc, token);
											Interlocked.Increment(ref totalItensIndexados);
										} catch (Exception ex) {
											Console.WriteLine($"Erro ao indexar item {itemDto.NumeroItem} no Elastic: {ex.Message}");
										}

										if (itemDto.TemResultado) {
											try {
												var urlResultados = $"https://pncp.gov.br/api/pncp/v1/orgaos/{item.OrgaoEntidade.Cnpj}/compras/{item.AnoCompra}/{item.SequencialCompra}/itens/{itemDto.NumeroItem}/resultados";
												var resultados = await httpClient.GetFromJsonAsync<List<PncpResultadoDto>>(urlResultados, token);

												if (resultados is not null) {
													foreach (var resultadoDto in resultados) {
														var modeloResultado = new ResultadoItem {
															IdentificadorDoItemDaCompra = idItem,
															NiFornecedor = resultadoDto.NiFornecedor,
															NomeRazaoSocialFornecedor = resultadoDto.NomeRazaoSocialFornecedor,
															ValorTotalHomologado = resultadoDto.ValorTotalHomologado,
															ValorUnitarioHomologado = resultadoDto.ValorUnitarioHomologado,
															QuantidadeHomologada = resultadoDto.QuantidadeHomologada,
															SituacaoCompraItemResultadoNome = resultadoDto.SituacaoCompraItemResultadoNome,
															DataResultado = resultadoDto.DataResultado
														};
														await resultadosItens.UpsertAsync(modeloResultado);
													}
												}
											} catch (Exception ex) {
												Console.WriteLine($"Erro ao processar resultados do item {itemDto.NumeroItem} da compra {item.NumeroControlePncp}: {ex.Message}");
											}
										}
									}

									await compras.AtualizarStatusItensCarregadosAsync(idCompra, true);
								}
							} catch (Exception ex) {
								Console.WriteLine($"Erro ao processar itens da compra {item.NumeroControlePncp}: {ex.Message}");
							}
						} catch (Exception ex) {
							Console.WriteLine($"Erro ao processar compra {item.NumeroControlePncp}: {ex.Message}");
						}
					});

					paginaAtual++;
				} while (paginaAtual <= totalPaginas);

				Console.WriteLine($"Carga finalizada para CNPJ: {param.Cnpj}.");
			} catch (Exception ex) {
				Console.WriteLine($"Erro ao processar CNPJ {param.Cnpj}: {ex.Message}");
			}
		}

		return new ResultadoCarga(totalCompras, totalItensIndexados);
	}
}
