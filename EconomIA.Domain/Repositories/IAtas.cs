using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using EconomIA.Common.Persistence.Pagination;

namespace EconomIA.Domain.Repositories;

public interface IAtasReader : IReadRepository<Ata> {
	Task<Result<PaginationResult<Ata>, RepositoryError>> PaginarPorDataDeReferencia(
		Specification<Ata> filtro,
		PaginationParameters pagina,
		CancellationToken cancellationToken = default);
}

public static class AtasSpecifications {
	public static Specification<Ata> All() => new All();
	public static Specification<Ata> WithNumeroControlePncpCompra(String numeroControlePncpCompra) =>
		new WithNumeroControlePncpCompra(numeroControlePncpCompra);
	public static Specification<Ata> WithNumerosControlePncpCompra(ImmutableArray<String> numerosControlePncpCompra) =>
		new WithNumerosControlePncpCompra(numerosControlePncpCompra);
	public static Specification<Ata> ComDataDeReferenciaEntre(DateTime inicio, DateTime fim) =>
		new ComDataDeReferenciaEntre(inicio, fim);
	public static Specification<Ata> NaoCanceladas() => new NaoCanceladas();
	public static Specification<Ata> VigentesEm(DateTime data) => new VigentesEm(data);
	public static Specification<Ata> ComPossibilidadeDeAdesao() => new ComPossibilidadeDeAdesao();
	public static Specification<Ata> DoOrgaoComCnpj(String cnpj) => new DoOrgaoComCnpj(cnpj);
}

file class All : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() => x => true;
}

file class WithNumeroControlePncpCompra(String numeroControlePncpCompra) : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() =>
		x => x.NumeroControlePncpCompra == numeroControlePncpCompra;
}

file class WithNumerosControlePncpCompra(ImmutableArray<String> numerosControlePncpCompra) : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() =>
		x => x.NumeroControlePncpCompra != null && numerosControlePncpCompra.Contains(x.NumeroControlePncpCompra);
}

file class ComDataDeReferenciaEntre(DateTime inicio, DateTime fim) : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() =>
		x => (x.DataAssinatura ?? x.DataPublicacaoPncp) >= inicio
			&& (x.DataAssinatura ?? x.DataPublicacaoPncp) <= fim;
}

file class NaoCanceladas : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() => x => !x.Cancelado;
}

file class VigentesEm(DateTime data) : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() => x => x.VigenciaFim >= data;
}

file class ComPossibilidadeDeAdesao : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() => x => x.PossibilidadeAdesao == true;
}

file class DoOrgaoComCnpj(String cnpj) : Specification<Ata> {
	public override Expression<Func<Ata, Boolean>> Rule() =>
		x => x.Orgao != null && x.Orgao.Cnpj == cnpj;
}
