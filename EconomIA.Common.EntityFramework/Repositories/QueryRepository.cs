using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Common.EntityFramework.Repositories;

public abstract class QueryRepository<TContext, TAggregate>(IDbContextFactory<TContext> factory) : EntityFrameworkReadRepository<TAggregate> where TContext : DbContext where TAggregate : Aggregate {
	protected override Task<IDataScope<TAggregate>> CreateScope(CancellationToken cancellationToken = default) {
		var scope = new QueryScope<TContext, TAggregate>(factory, cancellationToken);
		return Task.FromResult<IDataScope<TAggregate>>(scope);
	}
}

file class QueryScope<TContext, TAggregate>(IDbContextFactory<TContext> factory, CancellationToken cancellationToken = default) : IDataScope<TAggregate> where TContext : DbContext where TAggregate : Aggregate {
	private TContext? context;
	private Boolean disposed;

	public async Task<IQueryable<TAggregate>> Query() {
		ObjectDisposedException.ThrowIf(disposed, typeof(QueryScope<TContext, TAggregate>));

		context ??= await factory.CreateDbContextAsync(cancellationToken);
		return context.Set<TAggregate>().AsQueryable().AsExpandableEFCore();
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
