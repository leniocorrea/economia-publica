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

namespace EconomIA.Adapters.Persistence.Repositories.OrgaosMonitorados;

public class OrgaosMonitoradosQueryRepository : QueryRepository<EconomIAQueryDbContext, OrgaoMonitorado>, IOrgaosMonitoradosReader {
	private readonly IDbContextFactory<EconomIAQueryDbContext> contextFactory;

	public OrgaosMonitoradosQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : base(factory) {
		contextFactory = factory;
	}

	protected override Task<IDataScope<OrgaoMonitorado>> CreateScope(CancellationToken cancellationToken = default) {
		var scope = new OrgaosMonitoradosQueryScope(contextFactory, cancellationToken);
		return Task.FromResult<IDataScope<OrgaoMonitorado>>(scope);
	}
}

file class OrgaosMonitoradosQueryScope(IDbContextFactory<EconomIAQueryDbContext> factory, CancellationToken cancellationToken = default) : IDataScope<OrgaoMonitorado> {
	private EconomIAQueryDbContext? context;
	private Boolean disposed;

	public async Task<IQueryable<OrgaoMonitorado>> Query() {
		ObjectDisposedException.ThrowIf(disposed, typeof(OrgaosMonitoradosQueryScope));

		context ??= await factory.CreateDbContextAsync(cancellationToken);
		return context.Set<OrgaoMonitorado>()
			.Include(x => x.Orgao)
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
