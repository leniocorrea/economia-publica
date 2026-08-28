using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.EntityFramework.Repositories;
using EconomIA.Common.Persistence;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Adapters.Persistence.Repositories.Atas;

public class AtasQueryRepository : QueryRepository<EconomIAQueryDbContext, Ata>, IAtasReader {
	private readonly IDbContextFactory<EconomIAQueryDbContext> factory;

	public AtasQueryRepository(IDbContextFactory<EconomIAQueryDbContext> factory) : base(factory) {
		this.factory = factory;
	}

	public async Task<Result<PaginationResult<Ata>, RepositoryError>> PaginarPorDataDeReferencia(
		Specification<Ata> filtro,
		PaginationParameters pagina,
		String? objetoDaCompra = null,
		CancellationToken cancellationToken = default) {
		CursorDaAta? cursor = null;

		if (!String.IsNullOrWhiteSpace(pagina.Cursor) && !CursorDaAta.TentarLer(pagina.Cursor, out cursor)) {
			return RepositoryError.InvalidFormat("Cursor inválido.");
		}

		await using var context = await factory.CreateDbContextAsync(cancellationToken);

		var consulta = context.Atas
			.Include(x => x.Orgao)
			.AsExpandableEFCore()
			.Where(filtro.Rule());

		if (!String.IsNullOrWhiteSpace(objetoDaCompra)) {
			var termo = objetoDaCompra.Trim().ToLower();

			consulta = consulta.Where(x => context.Compras.Any(c =>
				c.NumeroControlePncp == x.NumeroControlePncpCompra
				&& c.ObjetoCompra != null
				&& c.ObjetoCompra.ToLower().Contains(termo)));
		}

		if (cursor is not null) {
			var dataDoCursor = cursor.DataDeReferencia;
			var identificadorDoCursor = cursor.Id;

			consulta = consulta.Where(x =>
				(x.DataAssinatura ?? x.DataPublicacaoPncp) < dataDoCursor ||
				((x.DataAssinatura ?? x.DataPublicacaoPncp) == dataDoCursor && x.Id < identificadorDoCursor));
		}

		var atas = await consulta
			.OrderByDescending(x => x.DataAssinatura ?? x.DataPublicacaoPncp)
			.ThenByDescending(x => x.Id)
			.Take(pagina.Limit + 1)
			.ToArrayAsync(cancellationToken);

		var temMaisItens = atas.Length > pagina.Limit;
		var itens = temMaisItens ? atas.Take(pagina.Limit).ToArray() : atas;

		String? proximoCursor = null;

		if (temMaisItens && itens.Length > 0) {
			var ultima = itens[^1];
			var dataDeReferencia = ultima.DataAssinatura ?? ultima.DataPublicacaoPncp;

			if (dataDeReferencia.HasValue) {
				proximoCursor = CursorDaAta.Escrever(dataDeReferencia.Value, ultima.Id);
			}
		}

		return new PaginationResult<Ata>(itens, proximoCursor);
	}
}
