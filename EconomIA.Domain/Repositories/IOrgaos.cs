using System;
using System.Linq.Expressions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;

namespace EconomIA.Domain.Repositories;

public interface IOrgaosReader : IReadRepository<Orgao>;

public static class OrgaosSpecifications {
	public static Specification<Orgao> All() => new All();
	public static Specification<Orgao> WithId(Int64 id) => new WithId(id);
	public static Specification<Orgao> WithCnpj(String cnpj) => new WithCnpj(cnpj);
	public static Specification<Orgao> Ativos() => new Ativos();
}

file class All : Specification<Orgao> {
	public override Expression<Func<Orgao, Boolean>> Rule() => x => true;
}

file class WithId(Int64 id) : Specification<Orgao> {
	public override Expression<Func<Orgao, Boolean>> Rule() => x => x.Id == id;
}

file class WithCnpj(String cnpj) : Specification<Orgao> {
	public override Expression<Func<Orgao, Boolean>> Rule() => x => x.Cnpj == cnpj;
}

file class Ativos : Specification<Orgao> {
	public override Expression<Func<Orgao, Boolean>> Rule() => x => x.StatusAtivo;
}
