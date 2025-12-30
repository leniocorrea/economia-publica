using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Common.Persistence;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;

public class ItensDaCompraQueryRepository : QueryRepository<EconomIAQueryDbContext, ItemDaCompra>, IItensDaCompraReader {
	private readonly IDbContextFactory<EconomIAQueryDbContext> factory;

	public ItensDaCompraQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : base(factory) {
		this.factory = factory;
	}

	public async Task<Result<ImmutableArray<ItemDaCompra>, RepositoryError>> FilterWithCompraAndOrgao(
		Specification<ItemDaCompra> filter,
		CancellationToken cancellationToken = default) {
		await using var context = await factory.CreateDbContextAsync(cancellationToken);

		var items = await context.ItensDaCompra
			.Include(x => x.Compra)
				.ThenInclude(c => c!.Orgao)
					.ThenInclude(o => o!.Unidades)
			.AsExpandableEFCore()
			.Where(filter.Rule())
			.ToArrayAsync(cancellationToken);

		return items.ToImmutableArray();
	}
}
