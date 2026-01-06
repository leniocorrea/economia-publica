using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.ConfiguracoesCarga;

public class ConfiguracoesCargaQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : QueryRepository<EconomIAQueryDbContext, ConfiguracaoCarga>(factory), IConfiguracoesCargaReader;
