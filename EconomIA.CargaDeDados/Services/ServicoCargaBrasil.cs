using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using EconomIA.CargaDeDados.Dtos;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EconomIA.CargaDeDados.Services;

public record ResultadoCargaBrasil(
	Int32 ComprasProcessadas,
	Int32 ItensIndexados,
	Int32 ContratosProcessados,
	Int32 AtasProcessadas,
	Int64 DuracaoMs);

public class ServicoCargaBrasil {
	private const Int32 TamanhoBufferElastic = 1000;
	private const Int32 TamanhoPagina = 50;
	private const Int32 MaxModalidadesEmParalelo = 4;
	private const Int32 RequestsPorSegundo = 70;

	private static readonly Int32[] ModalidadesComDados = { 6, 8, 9, 12 };

	private readonly HttpClient httpClient;
	private readonly Elastic.Clients.Elasticsearch.ElasticsearchClient elasticClient;
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<ServicoCargaBrasil> logger;
	private readonly RateLimiter rateLimiter;

	public ServicoCargaBrasil(
		HttpClient httpClient,
		Elastic.Clients.Elasticsearch.ElasticsearchClient elasticClient,
		IServiceScopeFactory scopeFactory,
		ILogger<ServicoCargaBrasil> logger) {
		this.httpClient = httpClient;
		this.elasticClient = elasticClient;
		this.scopeFactory = scopeFactory;
		this.logger = logger;
		this.rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions {
			TokenLimit = RequestsPorSegundo,
			ReplenishmentPeriod = TimeSpan.FromSeconds(1),
			TokensPerPeriod = RequestsPorSegundo,
			QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
			QueueLimit = 1000
		});
	}

	public async Task<ResultadoCargaBrasil> ProcessarCargaCompletaAsync(
		DateTime dataInicial,
		DateTime dataFinal,
		Boolean apenasModalidadesComDados = true,
		CancellationToken cancellationToken = default) {

		var cronometro = Stopwatch.StartNew();
		var dataInicialStr = dataInicial.ToString("yyyyMMdd");
		var dataFinalStr = dataFinal.ToString("yyyyMMdd");

		logger.LogInformation(
			"Iniciando carga de todo o Brasil. Periodo: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy}, Rate limit: {RateLimit} req/s",
			dataInicial, dataFinal, RequestsPorSegundo);

		var totalCompras = 0;
		var totalItens = 0;
		var totalContratos = 0;
		var totalAtas = 0;

		var modalidades = apenasModalidadesComDados
			? ModalidadesComDados
			: Enumerable.Range(1, 14).ToArray();

		var resultadoCompras = await ProcessarComprasBrasilAsync(dataInicialStr, dataFinalStr, modalidades, cancellationToken);
		totalCompras = resultadoCompras.ComprasProcessadas;
		totalItens = resultadoCompras.ItensIndexados;

		var resultadoContratos = await ProcessarContratosBrasilAsync(dataInicialStr, dataFinalStr, cancellationToken);
		totalContratos = resultadoContratos;

		var resultadoAtas = await ProcessarAtasBrasilAsync(dataInicialStr, dataFinalStr, cancellationToken);
		totalAtas = resultadoAtas;

		cronometro.Stop();

		logger.LogInformation(
			"Carga Brasil finalizada em {Duracao}ms. Compras: {Compras}, Itens: {Itens}, Contratos: {Contratos}, Atas: {Atas}",
			cronometro.ElapsedMilliseconds, totalCompras, totalItens, totalContratos, totalAtas);

		return new ResultadoCargaBrasil(
			totalCompras,
			totalItens,
			totalContratos,
			totalAtas,
			cronometro.ElapsedMilliseconds);
	}

	private async Task<(Int32 ComprasProcessadas, Int32 ItensIndexados)> ProcessarComprasBrasilAsync(
		String dataInicial,
		String dataFinal,
		Int32[] modalidades,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processando compras de {QtdModalidades} modalidades em paralelo", modalidades.Length);

		var totalCompras = 0;
		var totalItens = 0;
		var bufferElastic = new ConcurrentBag<ItemDocument>();

		var opcoes = new ParallelOptions {
			MaxDegreeOfParallelism = MaxModalidadesEmParalelo,
			CancellationToken = cancellationToken
		};

		await Parallel.ForEachAsync(modalidades, opcoes, async (modalidade, token) => {
			var (compras, itens) = await ProcessarModalidadeAsync(
				dataInicial, dataFinal, modalidade, bufferElastic, token);

			Interlocked.Add(ref totalCompras, compras);
			Interlocked.Add(ref totalItens, itens);
		});

		if (bufferElastic.Count > 0) {
			await FlushBufferElasticAsync(bufferElastic.ToList(), cancellationToken);
		}

		return (totalCompras, totalItens);
	}

	private async Task<(Int32 Compras, Int32 Itens)> ProcessarModalidadeAsync(
		String dataInicial,
		String dataFinal,
		Int32 modalidade,
		ConcurrentBag<ItemDocument> bufferElastic,
		CancellationToken cancellationToken) {

		var comprasModalidade = 0;
		var itensModalidade = 0;
		var pagina = 1;

		logger.LogDebug("Iniciando modalidade {Modalidade}", modalidade);

		while (!cancellationToken.IsCancellationRequested) {
			using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

			if (!lease.IsAcquired) {
				await Task.Delay(100, cancellationToken);
				continue;
			}

			try {
				var url = $"https://pncp.gov.br/api/consulta/v1/contratacoes/publicacao?dataInicial={dataInicial}&dataFinal={dataFinal}&codigoModalidadeContratacao={modalidade}&pagina={pagina}&tamanhoPagina={TamanhoPagina}";

				var response = await httpClient.GetAsync(url, cancellationToken);

				if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
					break;
				}

				if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
					logger.LogWarning("Rate limit atingido na modalidade {Modalidade}, pagina {Pagina}. Aguardando...", modalidade, pagina);
					await Task.Delay(2000, cancellationToken);
					continue;
				}

				if (!response.IsSuccessStatusCode) {
					logger.LogWarning("Erro {StatusCode} na modalidade {Modalidade}, pagina {Pagina}", response.StatusCode, modalidade, pagina);
					break;
				}

				var resultado = await response.Content.ReadFromJsonAsync<PncpResponse>(cancellationToken: cancellationToken);

				if (resultado?.Data is null || resultado.Data.Count == 0) {
					break;
				}

				var (compras, itens) = await ProcessarLoteDeComprasAsync(resultado.Data, bufferElastic, cancellationToken);
				comprasModalidade += compras;
				itensModalidade += itens;

				if (bufferElastic.Count >= TamanhoBufferElastic) {
					var bufferParaFlush = bufferElastic.ToList();
					bufferElastic.Clear();
					await FlushBufferElasticAsync(bufferParaFlush, cancellationToken);
				}

				if (pagina >= resultado.TotalPaginas) {
					break;
				}

				pagina++;

				if (pagina % 10 == 0) {
					logger.LogDebug("Modalidade {Modalidade}: pagina {Pagina}/{Total}, compras: {Compras}", modalidade, pagina, resultado.TotalPaginas, comprasModalidade);
				}

			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao processar modalidade {Modalidade}, pagina {Pagina}", modalidade, pagina);
				break;
			}
		}

		logger.LogInformation("Modalidade {Modalidade} finalizada: {Compras} compras, {Itens} itens", modalidade, comprasModalidade, itensModalidade);
		return (comprasModalidade, itensModalidade);
	}

	private async Task<(Int32 Compras, Int32 Itens)> ProcessarLoteDeComprasAsync(
		List<PncpCompraDto> compras,
		ConcurrentBag<ItemDocument> bufferElastic,
		CancellationToken cancellationToken) {

		using var scope = scopeFactory.CreateScope();
		var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
		var comprasRepo = scope.ServiceProvider.GetRequiredService<Compras>();
		var itensDaCompraRepo = scope.ServiceProvider.GetRequiredService<ItensDaCompra>();
		var resultadosItensRepo = scope.ServiceProvider.GetRequiredService<ResultadosItens>();

		var comprasProcessadas = 0;
		var itensProcessados = 0;

		foreach (var compraDto in compras) {
			try {
				var modeloOrgao = new Orgao {
					Cnpj = compraDto.OrgaoEntidade.Cnpj,
					RazaoSocial = compraDto.OrgaoEntidade.RazaoSocial ?? ""
				};

				var idOrgao = await orgaosRepo.UpsertAsync(modeloOrgao);

				var modeloCompra = new Compra {
					IdentificadorDoOrgao = idOrgao,
					NumeroControlePncp = compraDto.NumeroControlePncp,
					AnoCompra = compraDto.AnoCompra,
					SequencialCompra = compraDto.SequencialCompra,
					ModalidadeIdentificador = compraDto.ModalidadeId,
					ModalidadeNome = compraDto.ModalidadeNome,
					ObjetoCompra = compraDto.ObjetoCompra,
					ValorTotalEstimado = compraDto.ValorTotalEstimado,
					ValorTotalHomologado = compraDto.ValorTotalHomologado,
					SituacaoCompraNome = compraDto.SituacaoCompraNome,
					DataInclusao = compraDto.DataInclusao,
					DataAberturaProposta = compraDto.DataAberturaProposta,
					DataEncerramentoProposta = compraDto.DataEncerramentoProposta,
					AmparoLegalNome = compraDto.AmparoLegal?.Nome,
					ModoDisputaNome = compraDto.ModoDisputaNome,
					LinkPncp = compraDto.LinkSistemaOrigem,
					DataAtualizacaoGlobal = compraDto.DataAtualizacaoGlobal
				};

				var idCompra = await comprasRepo.UpsertAsync(modeloCompra);
				comprasProcessadas++;

				var itens = await BuscarItensComRateLimitAsync(compraDto.OrgaoEntidade.Cnpj, compraDto.AnoCompra, compraDto.SequencialCompra, cancellationToken);

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

						var idItem = await itensDaCompraRepo.UpsertAsync(modeloItem);

						bufferElastic.Add(new ItemDocument {
							Id = idItem,
							Descricao = itemDto.Descricao ?? "",
							Valor = itemDto.ValorUnitarioEstimado ?? 0,
							Orgao = compraDto.OrgaoEntidade.RazaoSocial ?? "",
							Data = compraDto.DataAberturaProposta ?? DateTime.MinValue,
							DataInclusao = compraDto.DataInclusao,
							UfSigla = compraDto.UnidadeOrgao?.UfSigla
						});

						itensProcessados++;

						if (itemDto.TemResultado) {
							await BuscarEProcessarResultadosAsync(
								resultadosItensRepo,
								idItem,
								compraDto.OrgaoEntidade.Cnpj,
								compraDto.AnoCompra,
								compraDto.SequencialCompra,
								itemDto.NumeroItem,
								cancellationToken);
						}
					}

					await comprasRepo.AtualizarStatusItensCarregadosAsync(idCompra, true);
				}

			} catch (Exception ex) {
				logger.LogWarning(ex, "Erro ao processar compra {NumeroControle}", compraDto.NumeroControlePncp);
			}
		}

		return (comprasProcessadas, itensProcessados);
	}

	private async Task<List<PncpItemDto>?> BuscarItensComRateLimitAsync(
		String cnpj,
		Int32 anoCompra,
		Int32 sequencialCompra,
		CancellationToken cancellationToken) {

		using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

		if (!lease.IsAcquired) {
			await Task.Delay(100, cancellationToken);
		}

		try {
			var url = $"https://pncp.gov.br/api/pncp/v1/orgaos/{cnpj}/compras/{anoCompra}/{sequencialCompra}/itens";
			return await httpClient.GetFromJsonAsync<List<PncpItemDto>>(url, cancellationToken);
		} catch (Exception ex) {
			logger.LogWarning(ex, "Erro ao buscar itens da compra {Cnpj}/{Ano}/{Seq}", cnpj, anoCompra, sequencialCompra);
			return null;
		}
	}

	private async Task BuscarEProcessarResultadosAsync(
		ResultadosItens resultadosItensRepo,
		Int64 idItem,
		String cnpj,
		Int32 anoCompra,
		Int32 sequencialCompra,
		Int32 numeroItem,
		CancellationToken cancellationToken) {

		using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

		if (!lease.IsAcquired) {
			await Task.Delay(100, cancellationToken);
		}

		try {
			var url = $"https://pncp.gov.br/api/pncp/v1/orgaos/{cnpj}/compras/{anoCompra}/{sequencialCompra}/itens/{numeroItem}/resultados";
			var resultados = await httpClient.GetFromJsonAsync<List<PncpResultadoDto>>(url, cancellationToken);

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

					await resultadosItensRepo.UpsertAsync(modeloResultado);
				}
			}
		} catch (Exception ex) {
			logger.LogWarning(ex, "Erro ao buscar resultados do item {IdItem}", idItem);
		}
	}

	private async Task FlushBufferElasticAsync(List<ItemDocument> documentos, CancellationToken cancellationToken) {
		if (documentos.Count == 0) {
			return;
		}

		try {
			var response = await elasticClient.BulkAsync(b => b.IndexMany(documentos), cancellationToken);

			if (response.Errors) {
				logger.LogWarning("Alguns documentos falharam na indexacao. Total: {Total}, Erros: {Erros}",
					documentos.Count, response.ItemsWithErrors.Count());
			} else {
				logger.LogDebug("Indexados {Count} documentos no Elasticsearch", documentos.Count);
			}
		} catch (Exception ex) {
			logger.LogError(ex, "Erro ao indexar {Count} documentos no Elasticsearch", documentos.Count);
		}
	}

	private async Task<Int32> ProcessarContratosBrasilAsync(
		String dataInicial,
		String dataFinal,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processando contratos de todo o Brasil");

		var totalContratos = 0;
		var pagina = 1;

		while (!cancellationToken.IsCancellationRequested) {
			using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

			if (!lease.IsAcquired) {
				await Task.Delay(100, cancellationToken);
				continue;
			}

			try {
				var url = $"https://pncp.gov.br/api/consulta/v1/contratos?dataInicial={dataInicial}&dataFinal={dataFinal}&pagina={pagina}";
				var response = await httpClient.GetAsync(url, cancellationToken);

				if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
					break;
				}

				if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
					logger.LogWarning("Rate limit atingido em contratos, pagina {Pagina}. Aguardando...", pagina);
					await Task.Delay(2000, cancellationToken);
					continue;
				}

				if (!response.IsSuccessStatusCode) {
					logger.LogWarning("Erro {StatusCode} em contratos, pagina {Pagina}", response.StatusCode, pagina);
					break;
				}

				var resultado = await response.Content.ReadFromJsonAsync<PncpContratosResponse>(cancellationToken: cancellationToken);

				if (resultado?.Data is null || resultado.Data.Count == 0) {
					break;
				}

				var contratosProcessados = await ProcessarLoteDeContratosAsync(resultado.Data, cancellationToken);
				totalContratos += contratosProcessados;

				if (resultado.PaginasRestantes == 0) {
					break;
				}

				pagina++;

				if (pagina % 10 == 0) {
					logger.LogDebug("Contratos: pagina {Pagina}, total: {Total}", pagina, totalContratos);
				}

			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao processar contratos, pagina {Pagina}", pagina);
				break;
			}
		}

		logger.LogInformation("Contratos finalizados: {Total}", totalContratos);
		return totalContratos;
	}

	private async Task<Int32> ProcessarLoteDeContratosAsync(
		List<PncpContratoDto> contratos,
		CancellationToken cancellationToken) {

		using var scope = scopeFactory.CreateScope();
		var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
		var contratosRepo = scope.ServiceProvider.GetRequiredService<Contratos>();

		var processados = 0;

		foreach (var contratoDto in contratos) {
			try {
				var modeloOrgao = new Orgao {
					Cnpj = contratoDto.OrgaoEntidade.Cnpj,
					RazaoSocial = contratoDto.OrgaoEntidade.RazaoSocial
				};

				var idOrgao = await orgaosRepo.UpsertAsync(modeloOrgao);

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

				await contratosRepo.UpsertAsync(contrato);
				processados++;
			} catch (Exception ex) {
				logger.LogWarning(ex, "Erro ao processar contrato {NumeroControle}", contratoDto.NumeroControlePncp);
			}
		}

		return processados;
	}

	private async Task<Int32> ProcessarAtasBrasilAsync(
		String dataInicial,
		String dataFinal,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processando atas de todo o Brasil");

		var totalAtas = 0;
		var pagina = 1;

		while (!cancellationToken.IsCancellationRequested) {
			using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

			if (!lease.IsAcquired) {
				await Task.Delay(100, cancellationToken);
				continue;
			}

			try {
				var url = $"https://pncp.gov.br/api/consulta/v1/atas?dataInicial={dataInicial}&dataFinal={dataFinal}&pagina={pagina}";
				var response = await httpClient.GetAsync(url, cancellationToken);

				if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
					break;
				}

				if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
					logger.LogWarning("Rate limit atingido em atas, pagina {Pagina}. Aguardando...", pagina);
					await Task.Delay(2000, cancellationToken);
					continue;
				}

				if (!response.IsSuccessStatusCode) {
					logger.LogWarning("Erro {StatusCode} em atas, pagina {Pagina}", response.StatusCode, pagina);
					break;
				}

				var resultado = await response.Content.ReadFromJsonAsync<PncpAtasResponse>(cancellationToken: cancellationToken);

				if (resultado?.Data is null || resultado.Data.Count == 0) {
					break;
				}

				var atasProcessadas = await ProcessarLoteDeAtasAsync(resultado.Data, cancellationToken);
				totalAtas += atasProcessadas;

				if (resultado.PaginasRestantes == 0) {
					break;
				}

				pagina++;

				if (pagina % 10 == 0) {
					logger.LogDebug("Atas: pagina {Pagina}, total: {Total}", pagina, totalAtas);
				}

			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao processar atas, pagina {Pagina}", pagina);
				break;
			}
		}

		logger.LogInformation("Atas finalizadas: {Total}", totalAtas);
		return totalAtas;
	}

	private async Task<Int32> ProcessarLoteDeAtasAsync(
		List<PncpAtaDto> atas,
		CancellationToken cancellationToken) {

		using var scope = scopeFactory.CreateScope();
		var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
		var atasRepo = scope.ServiceProvider.GetRequiredService<Atas>();

		var processadas = 0;

		foreach (var ataDto in atas) {
			try {
				var modeloOrgao = new Orgao {
					Cnpj = ataDto.CnpjOrgao,
					RazaoSocial = ataDto.NomeOrgao ?? ""
				};

				var idOrgao = await orgaosRepo.UpsertAsync(modeloOrgao);

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

				await atasRepo.UpsertAsync(ata);
				processadas++;
			} catch (Exception ex) {
				logger.LogWarning(ex, "Erro ao processar ata {NumeroControle}", ataDto.NumeroControlePncpAta);
			}
		}

		return processadas;
	}
}
