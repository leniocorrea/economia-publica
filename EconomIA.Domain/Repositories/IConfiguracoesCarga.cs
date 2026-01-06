using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;

namespace EconomIA.Domain.Repositories;

public interface IConfiguracoesCarga : IRepository<ConfiguracaoCarga> {
	Task<Result<ConfiguracaoCarga, RepositoryError>> ObterOuCriarPadrao(CancellationToken cancellationToken = default);
}

public interface IConfiguracoesCargaReader : IReadRepository<ConfiguracaoCarga>;

public static class ConfiguracoesCargaSpecifications {
	public static Specification<ConfiguracaoCarga> All() => new All();
	public static Specification<ConfiguracaoCarga> WithId(Int64 id) => new WithId(id);
}

file class All : Specification<ConfiguracaoCarga> {
	public override Expression<Func<ConfiguracaoCarga, Boolean>> Rule() => x => true;
}

file class WithId(Int64 id) : Specification<ConfiguracaoCarga> {
	public override Expression<Func<ConfiguracaoCarga, Boolean>> Rule() => x => x.Id == id;
}
