using System;
using System.Linq.Expressions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;

namespace EconomIA.Domain.Repositories;

public interface IOrgaosMonitorados : IRepository<OrgaoMonitorado>;

public interface IOrgaosMonitoradosReader : IReadRepository<OrgaoMonitorado>;

public static class OrgaosMonitoradosSpecifications {
	public static Specification<OrgaoMonitorado> All() => new All();
	public static Specification<OrgaoMonitorado> WithId(Int64 id) => new WithId(id);
	public static Specification<OrgaoMonitorado> ComOrgao(Int64 identificadorDoOrgao) => new ComOrgao(identificadorDoOrgao);
	public static Specification<OrgaoMonitorado> Ativos() => new Ativos();
	public static Specification<OrgaoMonitorado> Inativos() => new Inativos();
}

file class All : Specification<OrgaoMonitorado> {
	public override Expression<Func<OrgaoMonitorado, Boolean>> Rule() => x => true;
}

file class WithId(Int64 id) : Specification<OrgaoMonitorado> {
	public override Expression<Func<OrgaoMonitorado, Boolean>> Rule() => x => x.Id == id;
}

file class ComOrgao(Int64 identificadorDoOrgao) : Specification<OrgaoMonitorado> {
	public override Expression<Func<OrgaoMonitorado, Boolean>> Rule() => x => x.IdentificadorDoOrgao == identificadorDoOrgao;
}

file class Ativos : Specification<OrgaoMonitorado> {
	public override Expression<Func<OrgaoMonitorado, Boolean>> Rule() => x => x.Ativo;
}

file class Inativos : Specification<OrgaoMonitorado> {
	public override Expression<Func<OrgaoMonitorado, Boolean>> Rule() => x => !x.Ativo;
}
