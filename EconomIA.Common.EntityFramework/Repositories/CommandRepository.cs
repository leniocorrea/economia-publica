using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Common.EntityFramework.Repositories;

public abstract class CommandRepository<TContext, TAggregate>(TContext database) : EntityFrameworkReadRepository<TAggregate>, IRepository<TAggregate> where TContext : DbContext where TAggregate : Aggregate {
	protected override Task<IDataScope<TAggregate>> CreateScope(CancellationToken cancellationToken = default) {
		var scope = new CommandScope<TContext, TAggregate>(database);
		return Task.FromResult<IDataScope<TAggregate>>(scope);
	}

	public async Task<Result<TAggregate, RepositoryError>> Add(TAggregate entity, CancellationToken cancellationToken = default) {
		if (entity is null) {
			return Result.Failure<TAggregate, RepositoryError>(new RepositoryError(RepositoryErrorCode.MissingArgument, "Entidade é obrigatória."));
		}

		await database.AddAsync(entity, cancellationToken);
		await database.SaveChangesAsync(cancellationToken);
		return Result.Success<TAggregate, RepositoryError>(entity);
	}

	public async Task<Result<TAggregate, RepositoryError>> Update(TAggregate entity, CancellationToken cancellationToken = default) {
		if (entity is null) {
			return Result.Failure<TAggregate, RepositoryError>(new RepositoryError(RepositoryErrorCode.MissingArgument, "Entidade é obrigatória."));
		}

		database.Update(entity);
		await database.SaveChangesAsync(cancellationToken);
		return Result.Success<TAggregate, RepositoryError>(entity);
	}

	public async Task<UnitResult<RepositoryError>> Remove(TAggregate entity, CancellationToken cancellationToken = default) {
		if (entity is null) {
			return UnitResult.Failure(new RepositoryError(RepositoryErrorCode.MissingArgument, "Entidade é obrigatória."));
		}

		database.Remove(entity);
		await database.SaveChangesAsync(cancellationToken);
		return UnitResult.Success<RepositoryError>();
	}
}

file class CommandScope<TContext, TAggregate>(TContext context) : IDataScope<TAggregate> where TContext : DbContext where TAggregate : Aggregate {
	public Task<IQueryable<TAggregate>> Query() {
		var query = context.Set<TAggregate>().AsQueryable().AsExpandableEFCore();
		return Task.FromResult(query);
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
