using System;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class Ata : Aggregate {
	protected Ata() {
		NumeroControlePncpAta = null!;
	}

	public Ata(
		Int64 id,
		Int64 identificadorDoOrgao,
		String numeroControlePncpAta,
		Int32 anoAta,
		DateTime? criadoEm = null,
		DateTime? atualizadoEm = null,
		String? numeroControlePncpCompra = null,
		String? numeroAtaRegistroPreco = null,
		String? objetoContratacao = null,
		Boolean cancelado = false,
		DateTime? dataCancelamento = null,
		DateTime? dataAssinatura = null,
		DateTime? vigenciaInicio = null,
		DateTime? vigenciaFim = null,
		DateTime? dataPublicacaoPncp = null,
		DateTime? dataInclusao = null,
		DateTime? dataAtualizacao = null,
		DateTime? dataAtualizacaoGlobal = null,
		String? usuario = null) : base(id) {
		IdentificadorDoOrgao = identificadorDoOrgao;
		NumeroControlePncpAta = numeroControlePncpAta;
		AnoAta = anoAta;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
		NumeroControlePncpCompra = numeroControlePncpCompra;
		NumeroAtaRegistroPreco = numeroAtaRegistroPreco;
		ObjetoContratacao = objetoContratacao;
		Cancelado = cancelado;
		DataCancelamento = dataCancelamento;
		DataAssinatura = dataAssinatura;
		VigenciaInicio = vigenciaInicio;
		VigenciaFim = vigenciaFim;
		DataPublicacaoPncp = dataPublicacaoPncp;
		DataInclusao = dataInclusao;
		DataAtualizacao = dataAtualizacao;
		DataAtualizacaoGlobal = dataAtualizacaoGlobal;
		Usuario = usuario;
	}

	public virtual Int64 IdentificadorDoOrgao { get; protected set; }
	public virtual String NumeroControlePncpAta { get; protected set; }
	public virtual String? NumeroControlePncpCompra { get; protected set; }
	public virtual String? NumeroAtaRegistroPreco { get; protected set; }
	public virtual Int32 AnoAta { get; protected set; }
	public virtual String? ObjetoContratacao { get; protected set; }
	public virtual Boolean Cancelado { get; protected set; }
	public virtual DateTime? DataCancelamento { get; protected set; }
	public virtual DateTime? DataAssinatura { get; protected set; }
	public virtual DateTime? VigenciaInicio { get; protected set; }
	public virtual DateTime? VigenciaFim { get; protected set; }
	public virtual DateTime? DataPublicacaoPncp { get; protected set; }
	public virtual DateTime? DataInclusao { get; protected set; }
	public virtual DateTime? DataAtualizacao { get; protected set; }
	public virtual DateTime? DataAtualizacaoGlobal { get; protected set; }
	public virtual String? Usuario { get; protected set; }
	public virtual DateTime? CriadoEm { get; protected set; }
	public virtual DateTime? AtualizadoEm { get; protected set; }

	public virtual Orgao? Orgao { get; protected set; }
}
