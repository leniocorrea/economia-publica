using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Common.EntityFramework;

public class QueryDbContext(DbContextOptions options) : ApplicationDbContext(options) {
	public override Int32 SaveChanges() {
		throw new QueryDbContextDoesNotAllowWrites();
	}

	public override Int32 SaveChanges(Boolean acceptAllChangesOnSuccess) {
		throw new QueryDbContextDoesNotAllowWrites();
	}

	public override Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default) {
		throw new QueryDbContextDoesNotAllowWrites();
	}

	public override Task<Int32> SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
		throw new QueryDbContextDoesNotAllowWrites();
	}
}

public class QueryDbContextDoesNotAllowWrites() : Exception($"""The "{nameof(QueryDbContext)}" does not allow writes.""");
