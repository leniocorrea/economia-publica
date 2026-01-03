using System;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class OrgaoMonitorado : Aggregate {
	protected OrgaoMonitorado() {
	}

	public OrgaoMonitorado(
		Int64 id,
		Int64 identificadorDoOrgao,
		Boolean ativo,
		DateTime criadoEm,
		DateTime atualizadoEm) : base(id) {
		IdentificadorDoOrgao = identificadorDoOrgao;
		Ativo = ativo;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
	}

	public static OrgaoMonitorado Criar(Int64 identificadorDoOrgao) {
		var agora = DateTime.UtcNow;
		return new OrgaoMonitorado(0, identificadorDoOrgao, true, agora, agora);
	}

	public virtual Int64 IdentificadorDoOrgao { get; protected set; }
	public virtual Boolean Ativo { get; protected set; }
	public virtual DateTime CriadoEm { get; protected set; }
	public virtual DateTime AtualizadoEm { get; protected set; }

	public virtual Orgao? Orgao { get; protected set; }

	public void Ativar() {
		Ativo = true;
		AtualizadoEm = DateTime.UtcNow;
	}

	public void Desativar() {
		Ativo = false;
		AtualizadoEm = DateTime.UtcNow;
	}
}
