using System;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class Unidade : Entity {
	protected Unidade() {
		CodigoUnidade = null!;
		NomeUnidade = null!;
	}

	public Unidade(
		Int64 id,
		Int64 identificadorDoOrgao,
		String codigoUnidade,
		String nomeUnidade,
		DateTime criadoEm,
		DateTime atualizadoEm,
		String? municipioNome = null,
		String? municipioCodigoIbge = null,
		String? ufSigla = null,
		String? ufNome = null,
		Boolean statusAtivo = true,
		DateTime? dataInclusaoPncp = null,
		DateTime? dataAtualizacaoPncp = null,
		String? justificativaAtualizacao = null) : base(id) {
		IdentificadorDoOrgao = identificadorDoOrgao;
		CodigoUnidade = codigoUnidade;
		NomeUnidade = nomeUnidade;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
		MunicipioNome = municipioNome;
		MunicipioCodigoIbge = municipioCodigoIbge;
		UfSigla = ufSigla;
		UfNome = ufNome;
		StatusAtivo = statusAtivo;
		DataInclusaoPncp = dataInclusaoPncp;
		DataAtualizacaoPncp = dataAtualizacaoPncp;
		JustificativaAtualizacao = justificativaAtualizacao;
	}

	public virtual Int64 IdentificadorDoOrgao { get; protected set; }
	public virtual String CodigoUnidade { get; protected set; }
	public virtual String NomeUnidade { get; protected set; }
	public virtual String? MunicipioNome { get; protected set; }
	public virtual String? MunicipioCodigoIbge { get; protected set; }
	public virtual String? UfSigla { get; protected set; }
	public virtual String? UfNome { get; protected set; }
	public virtual Boolean StatusAtivo { get; protected set; }
	public virtual DateTime? DataInclusaoPncp { get; protected set; }
	public virtual DateTime? DataAtualizacaoPncp { get; protected set; }
	public virtual String? JustificativaAtualizacao { get; protected set; }
	public virtual DateTime CriadoEm { get; protected set; }
	public virtual DateTime AtualizadoEm { get; protected set; }

	public virtual Orgao Orgao { get; protected set; } = null!;
}
