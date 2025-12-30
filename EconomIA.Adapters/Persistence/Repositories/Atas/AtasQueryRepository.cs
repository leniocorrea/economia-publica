using System;
using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.Atas;

public class AtasQueryRepository : QueryRepository<EconomIAQueryDbContext, Ata>, IAtasReader {
	public AtasQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : base(factory) {
	}
}
