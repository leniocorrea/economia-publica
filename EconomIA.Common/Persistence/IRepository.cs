using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;

namespace EconomIA.Common.Persistence;

public interface IRepository<TAggregate> : IReadRepository<TAggregate> where TAggregate : Aggregate {
	Task<Result<TAggregate, RepositoryError>> Add(TAggregate entity, CancellationToken cancellationToken = default);
	Task<Result<TAggregate, RepositoryError>> Update(TAggregate entity, CancellationToken cancellationToken = default);
	Task<UnitResult<RepositoryError>> Remove(TAggregate entity, CancellationToken cancellationToken = default);
}
