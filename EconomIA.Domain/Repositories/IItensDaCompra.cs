using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using EconomIA.Common.Persistence.Pagination;

namespace EconomIA.Domain.Repositories;

public interface IItensDaCompraReader : IReadRepository<ItemDaCompra> {
	Task<Result<ImmutableArray<ItemDaCompra>, RepositoryError>> FilterWithCompraAndOrgao(
		Specification<ItemDaCompra> filter,
		CancellationToken cancellationToken = default);
}

public interface IItensDaCompraSearcher {
	Task<Result<SearchResult, RepositoryError>> Search(
		String query,
		PaginationParameters? pagination = null,
		CancellationToken cancellationToken = default);
}

public record SearchResult(ImmutableArray<Int64> Ids, Int64 TotalHits, Boolean HasMoreItems);

public static class ItensDaCompraSpecifications {
	public static Specification<ItemDaCompra> All() => new All();
	public static Specification<ItemDaCompra> WithId(Int64 id) => new WithId(id);
	public static Specification<ItemDaCompra> WithIds(ImmutableArray<Int64> ids) => new WithIds(ids);
	public static Specification<ItemDaCompra> WithCompra(Int64 identificadorDaCompra) => new WithCompra(identificadorDaCompra);
	public static Specification<ItemDaCompra> ComResultado() => new ComResultado();
}

file class All : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => true;
}

file class WithId(Int64 id) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => x.Id == id;
}

file class WithIds(ImmutableArray<Int64> ids) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => ids.Contains(x.Id);
}

file class WithCompra(Int64 identificadorDaCompra) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => x.IdentificadorDaCompra == identificadorDaCompra;
}

file class ComResultado : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => x.TemResultado;
}
