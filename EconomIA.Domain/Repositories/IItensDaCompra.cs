using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using EconomIA.Common.Persistence.Pagination;

namespace EconomIA.Domain.Repositories;

public interface IItensDaCompraReader : IReadRepository<ItemDaCompra> {
	Task<Result<ImmutableArray<ItemDaCompra>, RepositoryError>> FilterWithCompraAndOrgao(
		Specification<ItemDaCompra> filter,
		CancellationToken cancellationToken = default);
}

public interface IItensDaCompraSearcher {
	Task<Result<SearchResult, RepositoryError>> Search(
		String? query,
		SearchFilters? filters = null,
		PaginationParameters? pagination = null,
		CancellationToken cancellationToken = default);
}

public record SearchFilters(
	DateTime? DataInclusaoInicio,
	DateTime? DataInclusaoFim,
	String? RazaoSocial,
	String? UfSigla,
	Decimal? ValorUnitarioHomologadoMinimo,
	Decimal? ValorUnitarioHomologadoMaximo,
	Decimal? ValorTotalHomologadoMinimo,
	Decimal? ValorTotalHomologadoMaximo,
	Boolean? SomenteComAdesao = null,
	Boolean? SomenteComAtaVigente = null,
	DateTime? DataDaAtaInicio = null,
	DateTime? DataDaAtaFim = null,
	String? ObjetoDaCompra = null) {

	public Boolean FiltraPorAta =>
		SomenteComAdesao == true
		|| SomenteComAtaVigente == true
		|| DataDaAtaInicio.HasValue
		|| DataDaAtaFim.HasValue;
}

public record SearchResult(ImmutableArray<Int64> Ids, Int64 TotalHits, Boolean HasMoreItems);

public static class ItensDaCompraSpecifications {
	public static Specification<ItemDaCompra> All() => new All();
	public static Specification<ItemDaCompra> WithId(Int64 id) => new WithId(id);
	public static Specification<ItemDaCompra> WithIds(ImmutableArray<Int64> ids) => new WithIds(ids);
	public static Specification<ItemDaCompra> WithCompra(Int64 identificadorDaCompra) => new WithCompra(identificadorDaCompra);
	public static Specification<ItemDaCompra> ComResultado() => new ComResultado();
	public static Specification<ItemDaCompra> DaCompraDoOrgao(String cnpjDoOrgao, Int32 anoCompra, Int32 sequencialCompra) =>
		new DaCompraDoOrgao(SomenteDigitos(cnpjDoOrgao), anoCompra, sequencialCompra);
	public static Specification<ItemDaCompra> ComDescricaoContendo(String termo) =>
		new ComDescricaoContendo(termo.Trim().ToLower());
	public static Specification<ItemDaCompra> ComObjetoDaCompraContendo(String termo) =>
		new ComObjetoDaCompraContendo(termo.Trim().ToLower());

	private static String SomenteDigitos(String valor) {
		return new String(valor.Where(Char.IsDigit).ToArray());
	}
}

file class All : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => true;
}

file class WithId(Int64 id) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => x.Id == id;
}

file class WithIds(ImmutableArray<Int64> ids) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => ids.Contains(x.Id);
}

file class WithCompra(Int64 identificadorDaCompra) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => x.IdentificadorDaCompra == identificadorDaCompra;
}

file class ComResultado : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() => x => x.TemResultado;
}

file class DaCompraDoOrgao(String cnpjDoOrgao, Int32 anoCompra, Int32 sequencialCompra) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() =>
		x => x.Compra != null
			&& x.Compra.Orgao != null
			&& x.Compra.Orgao.Cnpj == cnpjDoOrgao
			&& x.Compra.AnoCompra == anoCompra
			&& x.Compra.SequencialCompra == sequencialCompra;
}

file class ComDescricaoContendo(String termo) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() =>
		x => x.Descricao != null && x.Descricao.ToLower().Contains(termo);
}

file class ComObjetoDaCompraContendo(String termo) : Specification<ItemDaCompra> {
	public override Expression<Func<ItemDaCompra, Boolean>> Rule() =>
		x => x.Compra != null && x.Compra.ObjetoCompra != null && x.Compra.ObjetoCompra.ToLower().Contains(termo);
}
