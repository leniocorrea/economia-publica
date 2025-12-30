using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Common.EntityFramework.Repositories;

public abstract class EntityFrameworkReadRepository<TAggregate> : ReadRepository<TAggregate> where TAggregate : Aggregate {
	protected abstract override Task<IDataScope<TAggregate>> CreateScope(CancellationToken cancellationToken = default);
	protected override async Task<TAggregate[]> List(IQueryable<TAggregate> query, CancellationToken cancellationToken = default) => await query.ToArrayAsync(cancellationToken);
	protected override async Task<TAggregate?> Find(IQueryable<TAggregate> query, CancellationToken cancellationToken = default) => await query.FirstOrDefaultAsync(cancellationToken);
	protected override async Task<Boolean> Exists(IQueryable<TAggregate> query, CancellationToken cancellationToken = default) => await query.AnyAsync(cancellationToken);
	protected override async Task<Int64> Count(IQueryable<TAggregate> query, CancellationToken cancellationToken = default) => await query.LongCountAsync(cancellationToken);
}
