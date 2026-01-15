using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence.Pagination;

namespace EconomIA.Common.Persistence;

public abstract class ReadRepository<TAggregate> : IReadRepository<TAggregate> where TAggregate : Aggregate {
	protected abstract Task<IDataScope<TAggregate>> CreateScope(CancellationToken cancellationToken = default);
	protected abstract Task<TAggregate[]> List(IQueryable<TAggregate> query, CancellationToken cancellationToken = default);
	protected abstract Task<TAggregate?> Find(IQueryable<TAggregate> query, CancellationToken cancellationToken = default);
	protected abstract Task<Boolean> Exists(IQueryable<TAggregate> query, CancellationToken cancellationToken = default);
	protected abstract Task<Int64> Count(IQueryable<TAggregate> query, CancellationToken cancellationToken = default);

	public async Task<ImmutableArray<TAggregate>> All(CancellationToken cancellationToken = default) {
		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		var items = await List(query, cancellationToken);
		return items.ToImmutableArray();
	}

	public async Task<Result<TAggregate, RepositoryError>> Retrieve(Int64 id, CancellationToken cancellationToken = default) {
		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		query = query.Where(x => x.Id == id);
		var entity = await Find(query, cancellationToken);

		if (entity is null) {
			return RepositoryError.NotFound($"{typeof(TAggregate).Name} with Id {id} not found.");
		}

		return entity;
	}

	public async Task<Result<TAggregate, RepositoryError>> Find(Specification<TAggregate> filter, CancellationToken cancellationToken = default) {
		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		query = query.Where(filter.Rule());
		var items = await List(query.Take(2), cancellationToken);

		if (items.Length == 0) {
			return RepositoryError.NotFound($"{typeof(TAggregate).Name} not found.");
		}

		if (items.Length > 1) {
			return RepositoryError.MultipleFound($"Multiple {typeof(TAggregate).Name} found.");
		}

		return items[0];
	}

	public async Task<Result<ImmutableArray<TAggregate>, RepositoryError>> Filter(Specification<TAggregate> filter, CancellationToken cancellationToken = default) {
		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		query = query.Where(filter.Rule());
		var items = await List(query, cancellationToken);
		return items.ToImmutableArray();
	}

	public async Task<Result<PaginationResult<TAggregate>, RepositoryError>> Paginate(PaginationParameters? page = null, Specification<TAggregate>? filter = null, CancellationToken cancellationToken = default) {
		page ??= PaginationParameters.Create().Value;
		filter ??= Specification<TAggregate>.True;

		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		query = query.Where(filter.Rule());

		var isDescending = page.Order?.StartsWith("-") == true;

		query = isDescending
			? query.OrderByDescending(x => x.Id)
			: query.OrderBy(x => x.Id);

		if (!String.IsNullOrWhiteSpace(page.Cursor)) {
			var cursorId = BitConverter.ToInt64(Convert.FromBase64String(page.Cursor));
			query = isDescending
				? query.Where(x => x.Id < cursorId)
				: query.Where(x => x.Id > cursorId);
		}

		var items = await List(query.Take(page.Limit + 1), cancellationToken);
		var hasMore = items.Length > page.Limit;
		var resultItems = hasMore ? items.Take(page.Limit).ToArray() : items;

		String? nextCursor = null;
		if (hasMore && resultItems.Length > 0) {
			var lastId = resultItems[^1].Id;
			nextCursor = Convert.ToBase64String(BitConverter.GetBytes(lastId));
		}

		return new PaginationResult<TAggregate>(resultItems, nextCursor);
	}

	public async Task<Boolean> Exists(Specification<TAggregate>? filter = null, CancellationToken cancellationToken = default) {
		filter ??= Specification<TAggregate>.True;

		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		query = query.Where(filter.Rule());
		return await Exists(query, cancellationToken);
	}

	public async Task<Int64> Count(Specification<TAggregate>? filter = null, CancellationToken cancellationToken = default) {
		filter ??= Specification<TAggregate>.True;

		await using var scope = await CreateScope(cancellationToken);
		var query = await scope.Query();
		query = query.Where(filter.Rule());
		return await Count(query, cancellationToken);
	}
}

public interface IDataScope<TAggregate> : IAsyncDisposable where TAggregate : Aggregate {
	Task<IQueryable<TAggregate>> Query();
}
