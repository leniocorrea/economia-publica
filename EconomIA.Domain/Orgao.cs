using System;
using System.Collections.Generic;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class Orgao : Aggregate {
	protected Orgao() {
		Cnpj = null!;
		RazaoSocial = null!;
		Unidades = new List<Unidade>();
	}

	public Orgao(
		Int64 id,
		String cnpj,
		String razaoSocial,
		DateTime criadoEm,
		DateTime atualizadoEm,
		String? nomeFantasia = null,
		String? codigoNaturezaJuridica = null,
		String? descricaoNaturezaJuridica = null,
		String? poderId = null,
		String? esferaId = null,
		String? situacaoCadastral = null,
		String? motivoSituacaoCadastral = null,
		DateTime? dataSituacaoCadastral = null,
		DateTime? dataValidacao = null,
		Boolean validado = false,
		DateTime? dataInclusaoPncp = null,
		DateTime? dataAtualizacaoPncp = null,
		Boolean statusAtivo = true,
		String? justificativaAtualizacao = null) : base(id) {
		Cnpj = cnpj;
		RazaoSocial = razaoSocial;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
		NomeFantasia = nomeFantasia;
		CodigoNaturezaJuridica = codigoNaturezaJuridica;
		DescricaoNaturezaJuridica = descricaoNaturezaJuridica;
		PoderId = poderId;
		EsferaId = esferaId;
		SituacaoCadastral = situacaoCadastral;
		MotivoSituacaoCadastral = motivoSituacaoCadastral;
		DataSituacaoCadastral = dataSituacaoCadastral;
		DataValidacao = dataValidacao;
		Validado = validado;
		DataInclusaoPncp = dataInclusaoPncp;
		DataAtualizacaoPncp = dataAtualizacaoPncp;
		StatusAtivo = statusAtivo;
		JustificativaAtualizacao = justificativaAtualizacao;
		Unidades = new List<Unidade>();
	}

	public virtual String Cnpj { get; protected set; }
	public virtual String RazaoSocial { get; protected set; }
	public virtual DateTime CriadoEm { get; protected set; }
	public virtual DateTime AtualizadoEm { get; protected set; }
	public virtual String? NomeFantasia { get; protected set; }
	public virtual String? CodigoNaturezaJuridica { get; protected set; }
	public virtual String? DescricaoNaturezaJuridica { get; protected set; }
	public virtual String? PoderId { get; protected set; }
	public virtual String? EsferaId { get; protected set; }
	public virtual String? SituacaoCadastral { get; protected set; }
	public virtual String? MotivoSituacaoCadastral { get; protected set; }
	public virtual DateTime? DataSituacaoCadastral { get; protected set; }
	public virtual DateTime? DataValidacao { get; protected set; }
	public virtual Boolean Validado { get; protected set; }
	public virtual DateTime? DataInclusaoPncp { get; protected set; }
	public virtual DateTime? DataAtualizacaoPncp { get; protected set; }
	public virtual Boolean StatusAtivo { get; protected set; }
	public virtual String? JustificativaAtualizacao { get; protected set; }

	public virtual ICollection<Unidade> Unidades { get; protected set; }
}
