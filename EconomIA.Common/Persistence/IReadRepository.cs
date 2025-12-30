using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence.Pagination;

namespace EconomIA.Common.Persistence;

public interface IReadRepository<TAggregate> where TAggregate : Aggregate {
	Task<ImmutableArray<TAggregate>> All(CancellationToken cancellationToken = default);
	Task<Result<TAggregate, RepositoryError>> Retrieve(Int64 id, CancellationToken cancellationToken = default);
	Task<Result<TAggregate, RepositoryError>> Find(Specification<TAggregate> filter, CancellationToken cancellationToken = default);
	Task<Result<ImmutableArray<TAggregate>, RepositoryError>> Filter(Specification<TAggregate> filter, CancellationToken cancellationToken = default);
	Task<Result<PaginationResult<TAggregate>, RepositoryError>> Paginate(PaginationParameters? page = null, Specification<TAggregate>? filter = null, CancellationToken cancellationToken = default);
	Task<Boolean> Exists(Specification<TAggregate>? filter = null, CancellationToken cancellationToken = default);
	Task<Int64> Count(Specification<TAggregate>? filter = null, CancellationToken cancellationToken = default);
}

public static class ReadRepositoryExtensions {
	public static Task<Result<PaginationResult<TAggregate>, RepositoryError>> Paginate<TAggregate>(this IReadRepository<TAggregate> repository, PaginationParameters paginationParameters, CancellationToken cancellationToken = default) where TAggregate : Aggregate {
		return repository.Paginate(paginationParameters, null, cancellationToken);
	}

	public static Task<Result<TAggregate, RepositoryError>> Find<TAggregate>(this IReadRepository<TAggregate> repository, Specification<TAggregate> firstFilter, Specification<TAggregate> secondFilter, CancellationToken cancellationToken = default) where TAggregate : Aggregate {
		var filters = SpecificationExtensions.Combine(firstFilter, secondFilter);
		return repository.Find(filters, cancellationToken);
	}

	public static Task<Result<ImmutableArray<TAggregate>, RepositoryError>> Filter<TAggregate>(this IReadRepository<TAggregate> repository, Specification<TAggregate> firstFilter, Specification<TAggregate> secondFilter, CancellationToken cancellationToken = default) where TAggregate : Aggregate {
		var filters = SpecificationExtensions.Combine(firstFilter, secondFilter);
		return repository.Filter(filters, cancellationToken);
	}

	public static Task<Result<PaginationResult<TAggregate>, RepositoryError>> Paginate<TAggregate>(this IReadRepository<TAggregate> repository, PaginationParameters paginationParameters, Specification<TAggregate> firstFilter, Specification<TAggregate> secondFilter, CancellationToken cancellationToken = default) where TAggregate : Aggregate {
		var filters = SpecificationExtensions.Combine(firstFilter, secondFilter);
		return repository.Paginate(paginationParameters, filters, cancellationToken);
	}

	public static Task<Boolean> Exists<TAggregate>(this IReadRepository<TAggregate> repository, Specification<TAggregate> firstFilter, Specification<TAggregate> secondFilter, CancellationToken cancellationToken = default) where TAggregate : Aggregate {
		var filters = SpecificationExtensions.Combine(firstFilter, secondFilter);
		return repository.Exists(filters, cancellationToken);
	}

	public static Task<Int64> Count<TAggregate>(this IReadRepository<TAggregate> repository, Specification<TAggregate> firstFilter, Specification<TAggregate> secondFilter, CancellationToken cancellationToken = default) where TAggregate : Aggregate {
		var filters = SpecificationExtensions.Combine(firstFilter, secondFilter);
		return repository.Count(filters, cancellationToken);
	}
}
