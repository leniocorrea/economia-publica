using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;

namespace EconomIA.Domain.Repositories;

public interface IContratosReader : IReadRepository<Contrato>;

public static class ContratosSpecifications {
	public static Specification<Contrato> All() => new All();
	public static Specification<Contrato> WithNumeroControlePncpCompra(String numeroControlePncpCompra) =>
		new WithNumeroControlePncpCompra(numeroControlePncpCompra);
	public static Specification<Contrato> WithNumerosControlePncpCompra(ImmutableArray<String> numerosControlePncpCompra) =>
		new WithNumerosControlePncpCompra(numerosControlePncpCompra);
}

file class All : Specification<Contrato> {
	public override Expression<Func<Contrato, Boolean>> Rule() => x => true;
}

file class WithNumeroControlePncpCompra(String numeroControlePncpCompra) : Specification<Contrato> {
	public override Expression<Func<Contrato, Boolean>> Rule() =>
		x => x.NumeroControlePncpCompra == numeroControlePncpCompra;
}

file class WithNumerosControlePncpCompra(ImmutableArray<String> numerosControlePncpCompra) : Specification<Contrato> {
	public override Expression<Func<Contrato, Boolean>> Rule() =>
		x => x.NumeroControlePncpCompra != null && numerosControlePncpCompra.Contains(x.NumeroControlePncpCompra);
}
