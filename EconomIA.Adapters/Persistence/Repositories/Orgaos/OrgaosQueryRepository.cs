using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EconomIA.Common.Domain;
using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Common.Persistence;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.Orgaos;

public class OrgaosQueryRepository : QueryRepository<EconomIAQueryDbContext, Orgao>, IOrgaosReader {
	private readonly IDbContextFactory<EconomIAQueryDbContext> contextFactory;

	public OrgaosQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : base(factory) {
		contextFactory = factory;
	}

	protected override Task<IDataScope<Orgao>> CreateScope(CancellationToken cancellationToken = default) {
		var scope = new OrgaosQueryScope(contextFactory, cancellationToken);
		return Task.FromResult<IDataScope<Orgao>>(scope);
	}
}

file class OrgaosQueryScope(IDbContextFactory<EconomIAQueryDbContext> factory, CancellationToken cancellationToken = default) : IDataScope<Orgao> {
	private EconomIAQueryDbContext? context;
	private Boolean disposed;

	public async Task<IQueryable<Orgao>> Query() {
		ObjectDisposedException.ThrowIf(disposed, typeof(OrgaosQueryScope));

		context ??= await factory.CreateDbContextAsync(cancellationToken);
		return context.Set<Orgao>()
			.Include(x => x.Unidades)
			.AsQueryable()
			.AsExpandableEFCore();
	}

	public async ValueTask DisposeAsync() {
		if (disposed) {
			return;
		}

		disposed = true;

		if (context is not null) {
			await context.DisposeAsync();
		}
	}
}
