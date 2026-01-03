using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;

namespace EconomIA.Adapters.Persistence.Repositories.OrgaosMonitorados;

public class OrgaosMonitoradosCommandRepository(EconomIACommandDbContext database) : CommandRepository<EconomIACommandDbContext, OrgaoMonitorado>(database), IOrgaosMonitorados;
