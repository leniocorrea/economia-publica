using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Common.Persistence;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.ConfiguracoesCarga;

public class ConfiguracoesCargaCommandRepository : CommandRepository<EconomIACommandDbContext, ConfiguracaoCarga>, IConfiguracoesCarga {
	private readonly EconomIACommandDbContext database;

	public ConfiguracoesCargaCommandRepository(EconomIACommandDbContext database) : base(database) {
		this.database = database;
	}

	public async Task<Result<ConfiguracaoCarga, RepositoryError>> ObterOuCriarPadrao(CancellationToken cancellationToken = default) {
		var config = await database.Set<ConfiguracaoCarga>().FirstOrDefaultAsync(cancellationToken);

		if (config is not null) {
			return Result.Success<ConfiguracaoCarga, RepositoryError>(config);
		}

		var novaPadrao = ConfiguracaoCarga.CriarPadrao();
		await database.AddAsync(novaPadrao, cancellationToken);
		await database.SaveChangesAsync(cancellationToken);

		return Result.Success<ConfiguracaoCarga, RepositoryError>(novaPadrao);
	}
}
