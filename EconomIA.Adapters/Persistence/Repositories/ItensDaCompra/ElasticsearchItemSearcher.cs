using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Persistence;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Domain.Repositories;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;

public class ElasticsearchItemSearcher : IItensDaCompraSearcher {
	private const String IndexPadrao = "itens-da-compra";
	internal const String CampoDataInclusao = "dataInclusao";
	internal const String CampoAtaAdesaoVigenciaFim = "ataAdesaoVigenciaFim";
	internal const String CampoAtaVigenciaFim = "ataVigenciaFim";
	internal const String CampoAtaDataDeReferencia = "ataDataDeReferencia";

	private readonly ElasticsearchClient client;
	private readonly String indexName;

	public ElasticsearchItemSearcher(ElasticsearchClient client) : this(client, IndexPadrao) {
	}

	internal ElasticsearchItemSearcher(ElasticsearchClient client, String indexName) {
		this.client = client;
		this.indexName = indexName;
	}

	public async Task<Result<SearchResult, RepositoryError>> Search(
		String? query,
		SearchFilters? filters = null,
		PaginationParameters? pagination = null,
		CancellationToken cancellationToken = default) {
		var limit = pagination?.Limit ?? 20;
		var offset = 0;

		if (pagination?.Cursor is not null && Int32.TryParse(pagination.Cursor, out var cursorValue)) {
			offset = cursorValue;
		}

		var request = new SearchRequest(indexName) {
			From = offset,
			Size = limit + 1,
			Query = BuildQuery(query, filters),
			Sort = BuildSort(query, filters)
		};

		try {
			var response = await client.SearchAsync<ItemDocument>(request, cancellationToken);

			if (!response.IsValidResponse) {
				var errorMessage = response.ElasticsearchServerError?.Error?.Reason ?? "Elasticsearch error";
				return CSharpFunctionalExtensions.Result.Failure<SearchResult, RepositoryError>(
					new RepositoryError(RepositoryErrorCode.Unknown, errorMessage));
			}

			var ids = response.Documents
				.Take(limit)
				.Select(d => d.Id)
				.ToImmutableArray();

			var hasMoreItems = response.Documents.Count > limit;
			var totalHits = response.Total;

			return CSharpFunctionalExtensions.Result.Success<SearchResult, RepositoryError>(
				new SearchResult(ids, totalHits, hasMoreItems));
		}
		catch (Exception ex) {
			return CSharpFunctionalExtensions.Result.Failure<SearchResult, RepositoryError>(
				new RepositoryError(RepositoryErrorCode.Unknown, ex.Message));
		}
	}

	internal static String? CampoDeOrdenacao(String? query, SearchFilters? filters) {
		if (!String.IsNullOrWhiteSpace(query)) {
			return null;
		}

		return filters?.FiltraPorAta == true ? CampoAtaDataDeReferencia : CampoDataInclusao;
	}

	private static List<SortOptions>? BuildSort(String? query, SearchFilters? filters) {
		var campo = CampoDeOrdenacao(query, filters);

		if (campo is null) {
			return null;
		}

		return new List<SortOptions> {
			SortOptions.Field(new Field(campo), new FieldSort { Order = SortOrder.Desc }),
			SortOptions.Field(new Field("id"), new FieldSort { Order = SortOrder.Desc })
		};
	}

	internal static BoolQuery BuildQuery(String? query, SearchFilters? filters) {
		var queries = new List<Query>();

		if (String.IsNullOrWhiteSpace(query)) {
			queries.Add(new MatchAllQuery());
		} else {
			queries.Add(new MatchQuery(new Field("descricao")) {
				Query = query,
				Fuzziness = new Fuzziness("AUTO")
			});
		}

		if (filters is not null) {
			if (!String.IsNullOrWhiteSpace(filters.RazaoSocial)) {
				queries.Add(new MatchQuery(new Field("orgao")) {
					Query = filters.RazaoSocial
				});
			}

			if (!String.IsNullOrWhiteSpace(filters.UfSigla)) {
				queries.Add(new TermQuery(new Field("ufSigla")) {
					Value = filters.UfSigla.ToUpperInvariant()
				});
			}

			if (filters.DataInclusaoInicio.HasValue || filters.DataInclusaoFim.HasValue) {
				queries.Add(new DateRangeQuery(new Field(CampoDataInclusao)) {
					Gte = filters.DataInclusaoInicio,
					Lte = filters.DataInclusaoFim
				});
			}

			if (filters.SomenteComAdesao == true) {
				queries.Add(new DateRangeQuery(new Field(CampoAtaAdesaoVigenciaFim)) {
					Gte = Hoje()
				});
			}

			if (filters.SomenteComAtaVigente == true) {
				queries.Add(new DateRangeQuery(new Field(CampoAtaVigenciaFim)) {
					Gte = Hoje()
				});
			}

			if (filters.DataDaAtaInicio.HasValue || filters.DataDaAtaFim.HasValue) {
				queries.Add(new DateRangeQuery(new Field(CampoAtaDataDeReferencia)) {
					Gte = filters.DataDaAtaInicio?.Date,
					Lt = filters.DataDaAtaFim?.Date.AddDays(1)
				});
			}
		}

		return new BoolQuery { Must = queries };
	}

	private static DateTime Hoje() {
		return DateTime.UtcNow.AddHours(-3).Date;
	}
}

file class ItemDocument {
	public Int64 Id { get; set; }
	public String Descricao { get; set; } = null!;
	public Decimal Valor { get; set; }
	public String Orgao { get; set; } = null!;
	public DateTime Data { get; set; }
	public DateTime? DataInclusao { get; set; }
	public String? UfSigla { get; set; }
}
