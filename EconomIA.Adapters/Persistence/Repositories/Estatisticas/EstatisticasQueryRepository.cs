using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EconomIA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.Estatisticas;

public class EstatisticasQueryRepository(IDbContextFactory<EconomIAQueryDbContext> contextFactory) : IEstatisticas {
	public async Task<EstatisticasGerais> ObterEstatisticasGeraisAsync(CancellationToken cancellationToken = default) {
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var totalCompras = await context.Compras.LongCountAsync(cancellationToken);
		var totalContratos = await context.Contratos.LongCountAsync(cancellationToken);
		var totalAtas = await context.Atas.LongCountAsync(cancellationToken);
		var totalItens = await context.ItensDaCompra.LongCountAsync(cancellationToken);
		var totalOrgaosMonitorados = await context.OrgaosMonitorados.Where(x => x.Ativo).LongCountAsync(cancellationToken);

		return new EstatisticasGerais(
			TotalCompras: totalCompras,
			TotalContratos: totalContratos,
			TotalAtas: totalAtas,
			TotalItens: totalItens,
			TotalOrgaosMonitorados: totalOrgaosMonitorados);
	}
}
