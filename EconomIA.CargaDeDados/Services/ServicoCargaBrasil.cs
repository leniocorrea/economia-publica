using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
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
	Int32 OrgaosProcessados,
	Int64 DuracaoMs);

public record ResultadoReconciliacao(
	Int32 ComprasProcessadas,
	Int32 ItensIndexados,
	Int32 OrgaosProcessados);

public record ResultadoEnriquecimento(
	Int32 DocumentosEnriquecidos);

public class ServicoCargaBrasil {
	private const Int32 TamanhoBufferElastic = 1000;
	private const Int32 TamanhoPagina = 50;
	private const Int32 MaxModalidadesEmParalelo = 4;
	private const Int32 MaxComprasEmParalelo = 10;
	private const Int32 RequestsPorSegundo = 70;
	private const Int32 MaxTentativasPorPagina = 3;
	private const Int32 DiasPorFatia = 30;
	private static readonly TimeSpan[] IntervalosRetry = { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) };
	private const Int32 MaxComprasReconciliacao = 1;
	private const Int32 TamanhoLoteReconciliacao = 200;
	private const Int32 LotesPorLogDeEnriquecimento = 50;
	private const Int32 MaxTentativasReconciliacao = 5;
	private static readonly TimeSpan DelayPorCompraReconciliacao = TimeSpan.FromMilliseconds(800);
	private static readonly TimeSpan BackoffThrottleReconciliacao = TimeSpan.FromSeconds(30);
	private static readonly Regex RegexControleCompra = new Regex(@"^(\d{14})-1-(\d{6})/(\d{4})$", RegexOptions.Compiled);

	private static readonly Int32[] ModalidadesComDados = { 6, 7, 8, 9, 12 };

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
		Boolean carregarCompras = true,
		Boolean carregarContratos = true,
		Boolean carregarAtas = true,
		CancellationToken cancellationToken = default) {

		var cronometro = Stopwatch.StartNew();

		logger.LogInformation(
			"Iniciando carga de todo o Brasil. Periodo: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy}, Rate limit: {RateLimit} req/s",
			dataInicial, dataFinal, RequestsPorSegundo);

		var totalCompras = 0;
		var totalItens = 0;
		var totalContratos = 0;
		var totalAtas = 0;
		var orgaosProcessados = new ConcurrentDictionary<Int64, Byte>();

		var modalidades = apenasModalidadesComDados
			? ModalidadesComDados
			: Enumerable.Range(1, 14).ToArray();

		var fatias = GerarFatiasDePeriodo(dataInicial, dataFinal);

		logger.LogInformation("Periodo dividido em {QtdFatias} fatias de ate {Dias} dias. Compras: {Compras}, Contratos: {Contratos}, Atas: {Atas}",
			fatias.Count, DiasPorFatia, carregarCompras, carregarContratos, carregarAtas);

		foreach (var (inicioFatia, fimFatia) in fatias) {
			if (cancellationToken.IsCancellationRequested) {
				break;
			}

			var inicioStr = inicioFatia.ToString("yyyyMMdd");
			var fimStr = fimFatia.ToString("yyyyMMdd");

			logger.LogInformation("Processando fatia {Inicio:dd/MM/yyyy} a {Fim:dd/MM/yyyy}", inicioFatia, fimFatia);

			if (carregarCompras) {
				var resultadoCompras = await ProcessarComprasBrasilAsync(inicioStr, fimStr, modalidades, orgaosProcessados, cancellationToken);
				totalCompras += resultadoCompras.ComprasProcessadas;
				totalItens += resultadoCompras.ItensIndexados;
			}

			if (carregarContratos) {
				var resultadoContratos = await ProcessarContratosBrasilAsync(inicioStr, fimStr, orgaosProcessados, cancellationToken);
				totalContratos += resultadoContratos;
			}

			if (carregarAtas) {
				var resultadoAtas = await ProcessarAtasBrasilAsync(inicioStr, fimStr, orgaosProcessados, cancellationToken);
				totalAtas += resultadoAtas;
			}

			logger.LogInformation(
				"Fatia {Inicio:dd/MM/yyyy} a {Fim:dd/MM/yyyy} concluida. Parcial - Compras: {Compras}, Itens: {Itens}, Contratos: {Contratos}, Atas: {Atas}",
				inicioFatia, fimFatia, totalCompras, totalItens, totalContratos, totalAtas);
		}

		await AtualizarControlesImportacaoAsync(orgaosProcessados.Keys, dataInicial, dataFinal, cancellationToken);

		cronometro.Stop();

		logger.LogInformation(
			"Carga Brasil finalizada em {Duracao}ms. Compras: {Compras}, Itens: {Itens}, Contratos: {Contratos}, Atas: {Atas}, Orgaos: {Orgaos}",
			cronometro.ElapsedMilliseconds, totalCompras, totalItens, totalContratos, totalAtas, orgaosProcessados.Count);

		return new ResultadoCargaBrasil(
			totalCompras,
			totalItens,
			totalContratos,
			totalAtas,
			orgaosProcessados.Count,
			cronometro.ElapsedMilliseconds);
	}

	private static List<(DateTime Inicio, DateTime Fim)> GerarFatiasDePeriodo(DateTime dataInicial, DateTime dataFinal) {
		var fatias = new List<(DateTime, DateTime)>();
		var atual = dataInicial;

		while (atual < dataFinal) {
			var fimFatia = atual.AddDays(DiasPorFatia);

			if (fimFatia > dataFinal) {
				fimFatia = dataFinal;
			}

			fatias.Add((atual, fimFatia));
			atual = fimFatia.AddDays(1);
		}

		return fatias;
	}

	private async Task<(Int32 ComprasProcessadas, Int32 ItensIndexados)> ProcessarComprasBrasilAsync(
		String dataInicial,
		String dataFinal,
		Int32[] modalidades,
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
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
				dataInicial, dataFinal, modalidade, bufferElastic, orgaosProcessados, token);

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
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
		CancellationToken cancellationToken) {

		var comprasModalidade = 0;
		var itensModalidade = 0;
		var pagina = 1;
		var tentativasConsecutivas = 0;

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

				tentativasConsecutivas = 0;

				var (compras, itens) = await ProcessarLoteDeComprasAsync(resultado.Data, bufferElastic, orgaosProcessados, cancellationToken);
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

			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				break;
			} catch (Exception ex) {
				tentativasConsecutivas++;

				if (tentativasConsecutivas <= MaxTentativasPorPagina) {
					var intervalo = IntervalosRetry[tentativasConsecutivas - 1];
					logger.LogWarning("Erro ao processar modalidade {Modalidade}, pagina {Pagina}. Tentativa {Tentativa}/{Max}. Retry em {Intervalo}s",
						modalidade, pagina, tentativasConsecutivas, MaxTentativasPorPagina, intervalo.TotalSeconds);
					await Task.Delay(intervalo, cancellationToken);
					continue;
				}

				logger.LogError(ex, "Erro ao processar modalidade {Modalidade}, pagina {Pagina} apos {Max} tentativas. Pulando pagina",
					modalidade, pagina, MaxTentativasPorPagina);
				tentativasConsecutivas = 0;
				pagina++;
			}
		}

		logger.LogInformation("Modalidade {Modalidade} finalizada: {Compras} compras, {Itens} itens", modalidade, comprasModalidade, itensModalidade);
		return (comprasModalidade, itensModalidade);
	}

	private async Task<(Int32 Compras, Int32 Itens)> ProcessarLoteDeComprasAsync(
		List<PncpCompraDto> compras,
		ConcurrentBag<ItemDocument> bufferElastic,
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
		CancellationToken cancellationToken) {

		var comprasProcessadas = 0;
		var itensProcessados = 0;

		var opcoes = new ParallelOptions {
			MaxDegreeOfParallelism = MaxComprasEmParalelo,
			CancellationToken = cancellationToken
		};

		await Parallel.ForEachAsync(compras, opcoes, async (compraDto, token) => {
			try {
				using var scope = scopeFactory.CreateScope();
				var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
				var comprasRepo = scope.ServiceProvider.GetRequiredService<Compras>();
				var itensDaCompraRepo = scope.ServiceProvider.GetRequiredService<ItensDaCompra>();
				var resultadosItensRepo = scope.ServiceProvider.GetRequiredService<ResultadosItens>();

				var modeloOrgao = new Orgao {
					Cnpj = compraDto.OrgaoEntidade.Cnpj,
					RazaoSocial = compraDto.OrgaoEntidade.RazaoSocial ?? ""
				};

				var idOrgao = await orgaosRepo.UpsertAsync(modeloOrgao);
				orgaosProcessados.TryAdd(idOrgao, 0);

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
				Interlocked.Increment(ref comprasProcessadas);

				var itens = await BuscarItensComRateLimitAsync(compraDto.OrgaoEntidade.Cnpj, compraDto.AnoCompra, compraDto.SequencialCompra, token);

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
							UfSigla = compraDto.UnidadeOrgao?.UfSigla,
							ObjetoDaCompra = compraDto.ObjetoCompra
						});

						Interlocked.Increment(ref itensProcessados);

						if (itemDto.TemResultado) {
							await BuscarEProcessarResultadosAsync(
								resultadosItensRepo,
								idItem,
								compraDto.OrgaoEntidade.Cnpj,
								compraDto.AnoCompra,
								compraDto.SequencialCompra,
								itemDto.NumeroItem,
								token);
						}
					}

					await comprasRepo.AtualizarStatusItensCarregadosAsync(idCompra, true);
				}

			} catch (Exception ex) {
				logger.LogWarning(ex, "Erro ao processar compra {NumeroControle}", compraDto.NumeroControlePncp);
			}
		});

		return (comprasProcessadas, itensProcessados);
	}

	public async Task<ResultadoReconciliacao> ReconciliarAtasOrfasAsync(CancellationToken cancellationToken = default) {
		List<String> numerosCompra;

		using (var scope = scopeFactory.CreateScope()) {
			var atasRepo = scope.ServiceProvider.GetRequiredService<Atas>();
			numerosCompra = await atasRepo.ObterComprasOrfasComAdesaoAsync();
		}

		logger.LogInformation("Reconciliacao iniciada: {Total} compras orfas com adesao a buscar", numerosCompra.Count);

		var bufferElastic = new ConcurrentBag<ItemDocument>();
		var orgaosProcessados = new ConcurrentDictionary<Int64, Byte>();
		var totalCompras = 0;
		var totalItens = 0;
		var lotesProcessados = 0;

		var opcoesFetch = new ParallelOptions {
			MaxDegreeOfParallelism = MaxComprasReconciliacao,
			CancellationToken = cancellationToken
		};

		foreach (var lote in numerosCompra.Chunk(TamanhoLoteReconciliacao)) {
			if (cancellationToken.IsCancellationRequested) {
				break;
			}

			var comprasDoLote = new ConcurrentBag<PncpCompraDto>();

			await Parallel.ForEachAsync(lote, opcoesFetch, async (numero, token) => {
				var compra = await BuscarCompraAsync(numero, token);

				if (compra is not null) {
					comprasDoLote.Add(compra);
				}

				await Task.Delay(DelayPorCompraReconciliacao, token);
			});

			var (compras, itens) = await ProcessarLoteDeComprasAsync(comprasDoLote.ToList(), bufferElastic, orgaosProcessados, cancellationToken);
			totalCompras += compras;
			totalItens += itens;

			if (bufferElastic.Count >= TamanhoBufferElastic) {
				var bufferParaFlush = bufferElastic.ToList();
				bufferElastic.Clear();
				await FlushBufferElasticAsync(bufferParaFlush, cancellationToken);
			}

			lotesProcessados++;
			logger.LogInformation("Reconciliacao: lote {Lote}, compras acumuladas: {Compras}, itens: {Itens}", lotesProcessados, totalCompras, totalItens);
		}

		if (bufferElastic.Count > 0) {
			await FlushBufferElasticAsync(bufferElastic.ToList(), cancellationToken);
		}

		logger.LogInformation("Reconciliacao finalizada: {Compras} compras, {Itens} itens, {Orgaos} orgaos", totalCompras, totalItens, orgaosProcessados.Count);

		return new ResultadoReconciliacao(totalCompras, totalItens, orgaosProcessados.Count);
	}

	public async Task<ResultadoEnriquecimento> EnriquecerIndiceComAtasAsync(CancellationToken cancellationToken = default) {
		List<AtasDoItem> itens;

		using (var scope = scopeFactory.CreateScope()) {
			var itensRepo = scope.ServiceProvider.GetRequiredService<ItensDaCompra>();
			itens = await itensRepo.ObterAtasDosItensAsync();
		}

		logger.LogInformation("Enriquecimento iniciado: {Total} itens com ata vigente a atualizar", itens.Count);

		var total = 0;

		foreach (var lote in itens.Chunk(TamanhoBufferElastic)) {
			if (cancellationToken.IsCancellationRequested) {
				break;
			}

			await AtualizarAtasNoElasticAsync(lote, cancellationToken);
			total += lote.Length;
			logger.LogInformation("Enriquecimento: {Total} itens atualizados", total);
		}

		logger.LogInformation("Enriquecimento finalizado: {Total} itens", total);

		return new ResultadoEnriquecimento(total);
	}

	private async Task AtualizarAtasNoElasticAsync(AtasDoItem[] itens, CancellationToken cancellationToken) {
		var response = await elasticClient.BulkAsync(b => b
			.UpdateMany<AtasDoItem>(itens, (op, item) => op
				.Id(item.Id)
				.Doc(item)), cancellationToken);

		if (response.Errors) {
			logger.LogWarning("Alguns itens falharam no enriquecimento. Total: {Total}, Erros: {Erros}",
				itens.Length, response.ItemsWithErrors.Count());
		}
	}

	public async Task<ResultadoEnriquecimento> EnriquecerIndiceComObjetoDaCompraAsync(CancellationToken cancellationToken = default) {
		logger.LogInformation("Enriquecimento com objeto da compra iniciado");

		var ultimoIdentificador = 0L;
		var lotesProcessados = 0;
		var total = 0;

		while (!cancellationToken.IsCancellationRequested) {
			List<ObjetoDaCompraDoItem> lote;

			using (var scope = scopeFactory.CreateScope()) {
				var itensRepo = scope.ServiceProvider.GetRequiredService<ItensDaCompra>();
				lote = await itensRepo.ObterObjetosDasComprasDosItensAsync(ultimoIdentificador, TamanhoBufferElastic);
			}

			if (lote.Count == 0) {
				break;
			}

			await AtualizarObjetoDaCompraNoElasticAsync(lote, cancellationToken);

			ultimoIdentificador = lote[^1].Id;
			lotesProcessados++;
			total += lote.Count;

			if (lotesProcessados % LotesPorLogDeEnriquecimento == 0) {
				logger.LogInformation("Enriquecimento com objeto da compra: {Total} itens atualizados", total);
			}
		}

		logger.LogInformation("Enriquecimento com objeto da compra finalizado: {Total} itens", total);

		return new ResultadoEnriquecimento(total);
	}

	private async Task AtualizarObjetoDaCompraNoElasticAsync(List<ObjetoDaCompraDoItem> itens, CancellationToken cancellationToken) {
		var response = await elasticClient.BulkAsync(b => b
			.UpdateMany<ObjetoDaCompraDoItem>(itens, (op, item) => op
				.Id(item.Id)
				.Doc(item)), cancellationToken);

		if (response.Errors) {
			logger.LogWarning("Alguns itens falharam no enriquecimento com objeto da compra. Total: {Total}, Erros: {Erros}",
				itens.Count, response.ItemsWithErrors.Count());
		}
	}

	private async Task<PncpCompraDto?> BuscarCompraAsync(String numeroControlePncpCompra, CancellationToken cancellationToken) {
		var correspondencia = RegexControleCompra.Match(numeroControlePncpCompra);

		if (!correspondencia.Success) {
			return null;
		}

		var cnpj = correspondencia.Groups[1].Value;
		var sequencial = Int32.Parse(correspondencia.Groups[2].Value);
		var ano = correspondencia.Groups[3].Value;
		var url = $"https://pncp.gov.br/api/consulta/v1/orgaos/{cnpj}/compras/{ano}/{sequencial}";

		for (var tentativa = 1; tentativa <= MaxTentativasReconciliacao; tentativa++) {
			try {
				var response = await httpClient.GetAsync(url, cancellationToken);

				if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
					return null;
				}

				if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
					logger.LogWarning("Throttle (429) na reconciliacao da compra {Numero}. Aguardando {Segundos}s", numeroControlePncpCompra, BackoffThrottleReconciliacao.TotalSeconds);
					await Task.Delay(BackoffThrottleReconciliacao, cancellationToken);
					continue;
				}

				if (response.IsSuccessStatusCode) {
					return await response.Content.ReadFromJsonAsync<PncpCompraDto>(cancellationToken: cancellationToken);
				}

				await Task.Delay(IntervalosRetry[Math.Min(tentativa - 1, IntervalosRetry.Length - 1)], cancellationToken);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				return null;
			} catch (Exception ex) {
				if (tentativa < MaxTentativasReconciliacao) {
					await Task.Delay(IntervalosRetry[Math.Min(tentativa - 1, IntervalosRetry.Length - 1)], cancellationToken);
					continue;
				}

				logger.LogWarning(ex, "Erro ao buscar compra {Numero} na reconciliacao", numeroControlePncpCompra);
			}
		}

		return null;
	}

	private async Task<List<PncpItemDto>?> BuscarItensComRateLimitAsync(
		String cnpj,
		Int32 anoCompra,
		Int32 sequencialCompra,
		CancellationToken cancellationToken) {

		for (var tentativa = 1; tentativa <= MaxTentativasPorPagina; tentativa++) {
			using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

			if (!lease.IsAcquired) {
				await Task.Delay(100, cancellationToken);
			}

			try {
				var url = $"https://pncp.gov.br/api/pncp/v1/orgaos/{cnpj}/compras/{anoCompra}/{sequencialCompra}/itens";
				return await httpClient.GetFromJsonAsync<List<PncpItemDto>>(url, cancellationToken);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				return null;
			} catch (Exception ex) {
				if (tentativa < MaxTentativasPorPagina) {
					logger.LogWarning("Erro ao buscar itens da compra {Cnpj}/{Ano}/{Seq}. Tentativa {Tentativa}/{Max}",
						cnpj, anoCompra, sequencialCompra, tentativa, MaxTentativasPorPagina);
					await Task.Delay(IntervalosRetry[tentativa - 1], cancellationToken);
					continue;
				}

				logger.LogWarning(ex, "Erro ao buscar itens da compra {Cnpj}/{Ano}/{Seq} apos {Max} tentativas",
					cnpj, anoCompra, sequencialCompra, MaxTentativasPorPagina);
				return null;
			}
		}

		return null;
	}

	private async Task BuscarEProcessarResultadosAsync(
		ResultadosItens resultadosItensRepo,
		Int64 idItem,
		String cnpj,
		Int32 anoCompra,
		Int32 sequencialCompra,
		Int32 numeroItem,
		CancellationToken cancellationToken) {

		for (var tentativa = 1; tentativa <= MaxTentativasPorPagina; tentativa++) {
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

				return;
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				return;
			} catch (Exception ex) {
				if (tentativa < MaxTentativasPorPagina) {
					logger.LogWarning("Erro ao buscar resultados do item {IdItem}. Tentativa {Tentativa}/{Max}",
						idItem, tentativa, MaxTentativasPorPagina);
					await Task.Delay(IntervalosRetry[tentativa - 1], cancellationToken);
					continue;
				}

				logger.LogWarning(ex, "Erro ao buscar resultados do item {IdItem} apos {Max} tentativas",
					idItem, MaxTentativasPorPagina);
			}
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
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processando contratos de todo o Brasil");

		var totalContratos = 0;
		var pagina = 1;
		var tentativasConsecutivas = 0;

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

				tentativasConsecutivas = 0;

				var contratosProcessados = await ProcessarLoteDeContratosAsync(resultado.Data, orgaosProcessados, cancellationToken);
				totalContratos += contratosProcessados;

				if (resultado.PaginasRestantes == 0) {
					break;
				}

				pagina++;

				if (pagina % 10 == 0) {
					logger.LogDebug("Contratos: pagina {Pagina}, total: {Total}", pagina, totalContratos);
				}

			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				break;
			} catch (Exception ex) {
				tentativasConsecutivas++;

				if (tentativasConsecutivas <= MaxTentativasPorPagina) {
					var intervalo = IntervalosRetry[tentativasConsecutivas - 1];
					logger.LogWarning("Erro ao processar contratos, pagina {Pagina}. Tentativa {Tentativa}/{Max}. Retry em {Intervalo}s",
						pagina, tentativasConsecutivas, MaxTentativasPorPagina, intervalo.TotalSeconds);
					await Task.Delay(intervalo, cancellationToken);
					continue;
				}

				logger.LogError(ex, "Erro ao processar contratos, pagina {Pagina} apos {Max} tentativas. Pulando pagina",
					pagina, MaxTentativasPorPagina);
				tentativasConsecutivas = 0;
				pagina++;
			}
		}

		logger.LogInformation("Contratos finalizados: {Total}", totalContratos);
		return totalContratos;
	}

	private async Task<Int32> ProcessarLoteDeContratosAsync(
		List<PncpContratoDto> contratos,
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
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
				orgaosProcessados.TryAdd(idOrgao, 0);

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
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processando atas de todo o Brasil (otimizado)");

		var totalAtas = 0;
		var paginaAtual = 1;
		var totalPaginas = Int32.MaxValue;
		var cacheOrgaos = new ConcurrentDictionary<String, Int64>();

		using var scope = scopeFactory.CreateScope();
		var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
		var mapaOrgaos = await orgaosRepo.ObterMapaCnpjIdentificadorAsync();

		foreach (var kvp in mapaOrgaos) {
			cacheOrgaos[kvp.Key] = kvp.Value;
		}

		logger.LogInformation("Cache de orgaos carregado: {Count} orgaos", cacheOrgaos.Count);

		while (paginaAtual <= totalPaginas && !cancellationToken.IsCancellationRequested) {
			var paginasParaBuscar = Enumerable.Range(paginaAtual, Math.Min(8, totalPaginas - paginaAtual + 1)).ToList();

			var tarefas = paginasParaBuscar.Select(async pagina => {
				using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

				if (!lease.IsAcquired) {
					await Task.Delay(100, cancellationToken);
				}

				for (var tentativa = 1; tentativa <= MaxTentativasPorPagina; tentativa++) {
					try {
						var url = $"https://pncp.gov.br/api/consulta/v1/atas?dataInicial={dataInicial}&dataFinal={dataFinal}&pagina={pagina}";
						var response = await httpClient.GetAsync(url, cancellationToken);

						if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
							return (Pagina: pagina, Resultado: (PncpAtasResponse?)null, Sucesso: true);
						}

						if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
							await Task.Delay(2000, cancellationToken);
							response = await httpClient.GetAsync(url, cancellationToken);
						}

						if (!response.IsSuccessStatusCode) {
							return (Pagina: pagina, Resultado: (PncpAtasResponse?)null, Sucesso: false);
						}

						var resultado = await response.Content.ReadFromJsonAsync<PncpAtasResponse>(cancellationToken: cancellationToken);
						return (Pagina: pagina, Resultado: resultado, Sucesso: true);
					} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
						return (Pagina: pagina, Resultado: (PncpAtasResponse?)null, Sucesso: false);
					} catch (Exception ex) {
						if (tentativa < MaxTentativasPorPagina) {
							logger.LogWarning("Erro ao buscar atas pagina {Pagina}. Tentativa {Tentativa}/{Max}",
								pagina, tentativa, MaxTentativasPorPagina);
							await Task.Delay(IntervalosRetry[tentativa - 1], cancellationToken);
							continue;
						}

						logger.LogWarning(ex, "Erro ao buscar atas pagina {Pagina} apos {Max} tentativas", pagina, MaxTentativasPorPagina);
						return (Pagina: pagina, Resultado: (PncpAtasResponse?)null, Sucesso: false);
					}
				}

				return (Pagina: pagina, Resultado: (PncpAtasResponse?)null, Sucesso: false);
			});

			var resultados = await Task.WhenAll(tarefas);

			foreach (var (pagina, resultado, sucesso) in resultados.OrderBy(r => r.Pagina)) {
				if (resultado?.Data is null || resultado.Data.Count == 0) {
					continue;
				}

				if (pagina == paginaAtual && resultado.TotalPaginas > 0) {
					totalPaginas = resultado.TotalPaginas;
				}

				var atasProcessadas = await ProcessarLoteDeAtasComCacheAsync(resultado.Data, cacheOrgaos, orgaosProcessados, cancellationToken);
				Interlocked.Add(ref totalAtas, atasProcessadas);
			}

			paginaAtual += paginasParaBuscar.Count;

			if (paginaAtual % 50 == 0) {
				logger.LogInformation("Atas: pagina {Pagina}/{Total}, processadas: {Processadas}", paginaAtual, totalPaginas, totalAtas);
			}

			if (resultados.All(r => r.Resultado?.Data is null || r.Resultado.Data.Count == 0)) {
				break;
			}
		}

		logger.LogInformation("Atas finalizadas: {Total}", totalAtas);
		return totalAtas;
	}

	private async Task<Int32> ProcessarLoteDeAtasComCacheAsync(
		List<PncpAtaDto> atas,
		ConcurrentDictionary<String, Int64> cacheOrgaos,
		ConcurrentDictionary<Int64, Byte> orgaosProcessados,
		CancellationToken cancellationToken) {

		using var scope = scopeFactory.CreateScope();
		var orgaosRepo = scope.ServiceProvider.GetRequiredService<Orgaos>();
		var atasRepo = scope.ServiceProvider.GetRequiredService<Atas>();

		var processadas = 0;

		foreach (var ataDto in atas) {
			try {
				var idOrgao = await ObterOuCriarOrgaoComCacheAsync(
					ataDto.CnpjOrgao,
					ataDto.NomeOrgao ?? "",
					cacheOrgaos,
					orgaosRepo);
				orgaosProcessados.TryAdd(idOrgao, 0);

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
					Usuario = ataDto.Usuario,
					PossibilidadeAdesao = ataDto.PossibilidadeAdesao
				};

				await atasRepo.UpsertAsync(ata);
				processadas++;
			} catch (Exception ex) {
				logger.LogWarning(ex, "Erro ao processar ata {NumeroControle}", ataDto.NumeroControlePncpAta);
			}
		}

		return processadas;
	}

	private async Task<Int64> ObterOuCriarOrgaoComCacheAsync(
		String cnpj,
		String razaoSocial,
		ConcurrentDictionary<String, Int64> cache,
		Orgaos orgaosRepo) {

		if (cache.TryGetValue(cnpj, out var idExistente)) {
			return idExistente;
		}

		var modeloOrgao = new Orgao {
			Cnpj = cnpj,
			RazaoSocial = razaoSocial
		};

		var idOrgao = await orgaosRepo.UpsertAsync(modeloOrgao);
		cache[cnpj] = idOrgao;
		return idOrgao;
	}

	private async Task AtualizarControlesImportacaoAsync(
		IEnumerable<Int64> orgaosProcessados,
		DateTime dataInicial,
		DateTime dataFinal,
		CancellationToken cancellationToken) {

		var listaOrgaos = orgaosProcessados.ToList();

		if (listaOrgaos.Count == 0) {
			return;
		}

		logger.LogInformation("Atualizando controle de importacao para {Count} orgaos...", listaOrgaos.Count);

		using var scope = scopeFactory.CreateScope();
		var controlesRepo = scope.ServiceProvider.GetRequiredService<ControlesImportacao>();

		var tiposDados = new[] { "compras", "contratos", "atas" };

		foreach (var tipoDado in tiposDados) {
			try {
				var atualizados = await controlesRepo.BulkAtualizarControleAsync(
					listaOrgaos,
					tipoDado,
					dataInicial,
					dataFinal);

				logger.LogInformation("Controle de {TipoDado} atualizado para {Count} orgaos", tipoDado, atualizados);
			} catch (Exception ex) {
				logger.LogError(ex, "Erro ao atualizar controle de importacao para {TipoDado}", tipoDado);
			}
		}
	}
}
