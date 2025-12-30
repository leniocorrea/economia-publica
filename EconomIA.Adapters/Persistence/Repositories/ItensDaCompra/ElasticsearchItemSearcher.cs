using System;
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
	private readonly ElasticsearchClient client;
	private const String IndexName = "itens-da-compra";

	public ElasticsearchItemSearcher(ElasticsearchClient client) {
		this.client = client;
	}

	public async Task<Result<SearchResult, RepositoryError>> Search(
		String query,
		PaginationParameters? pagination = null,
		CancellationToken cancellationToken = default) {
		var limit = pagination?.Limit ?? 20;
		var offset = 0;

		if (pagination?.Cursor is not null && Int32.TryParse(pagination.Cursor, out var cursorValue)) {
			offset = cursorValue;
		}

		try {
			var response = await client.SearchAsync<ItemDocument>(s => s
				.Index(IndexName)
				.From(offset)
				.Size(limit + 1)
				.Query(q => q
					.Match(m => m
						.Field(f => f.Descricao)
						.Query(query)
						.Fuzziness(new Fuzziness("AUTO"))
					)
				),
				cancellationToken
			);

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
}

file class ItemDocument {
	public Int64 Id { get; set; }
	public String Descricao { get; set; } = null!;
}
