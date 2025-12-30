using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;

namespace EconomIA.Domain.Repositories;

public interface IAtasReader : IReadRepository<Ata>;

public static class AtasSpecifications {
	public static Specification<Ata> All() => new All();
	public static Specification<Ata> WithNumeroControlePncpCompra(String numeroControlePncpCompra) =>
		new WithNumeroControlePncpCompra(numeroControlePncpCompra);
	public static Specification<Ata> WithNumerosControlePncpCompra(ImmutableArray<String> numerosControlePncpCompra) =>
		new WithNumerosControlePncpCompra(numerosControlePncpCompra);
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
