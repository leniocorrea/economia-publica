using System;
using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.Contratos;

public class ContratosQueryRepository : QueryRepository<EconomIAQueryDbContext, Contrato>, IContratosReader {
	public ContratosQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : base(factory) {
	}
}
