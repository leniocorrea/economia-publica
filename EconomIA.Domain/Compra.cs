using System;
using System.Collections.Generic;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class Compra : Aggregate {
	protected Compra() {
		NumeroControlePncp = null!;
		ItensDaCompra = new List<ItemDaCompra>();
	}

	public Compra(
		Int64 id,
		Int64 identificadorDoOrgao,
		String numeroControlePncp,
		Int32 anoCompra,
		Int32 sequencialCompra,
		Int32 modalidadeIdentificador,
		DateTime criadoEm,
		DateTime atualizadoEm,
		String? modalidadeNome = null,
		String? objetoCompra = null,
		Decimal? valorTotalEstimado = null,
		Decimal? valorTotalHomologado = null,
		String? situacaoCompraNome = null,
		DateTime? dataAberturaProposta = null,
		DateTime? dataEncerramentoProposta = null,
		String? amparoLegalNome = null,
		String? modoDisputaNome = null,
		String? linkPncp = null,
		DateTime? dataAtualizacaoGlobal = null,
		Boolean itensCarregados = false) : base(id) {
		IdentificadorDoOrgao = identificadorDoOrgao;
		NumeroControlePncp = numeroControlePncp;
		AnoCompra = anoCompra;
		SequencialCompra = sequencialCompra;
		ModalidadeIdentificador = modalidadeIdentificador;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
		ModalidadeNome = modalidadeNome;
		ObjetoCompra = objetoCompra;
		ValorTotalEstimado = valorTotalEstimado;
		ValorTotalHomologado = valorTotalHomologado;
		SituacaoCompraNome = situacaoCompraNome;
		DataAberturaProposta = dataAberturaProposta;
		DataEncerramentoProposta = dataEncerramentoProposta;
		AmparoLegalNome = amparoLegalNome;
		ModoDisputaNome = modoDisputaNome;
		LinkPncp = linkPncp;
		DataAtualizacaoGlobal = dataAtualizacaoGlobal;
		ItensCarregados = itensCarregados;
		ItensDaCompra = new List<ItemDaCompra>();
	}

	public virtual Int64 IdentificadorDoOrgao { get; protected set; }
	public virtual String NumeroControlePncp { get; protected set; }
	public virtual Int32 AnoCompra { get; protected set; }
	public virtual Int32 SequencialCompra { get; protected set; }
	public virtual Int32 ModalidadeIdentificador { get; protected set; }
	public virtual String? ModalidadeNome { get; protected set; }
	public virtual String? ObjetoCompra { get; protected set; }
	public virtual Decimal? ValorTotalEstimado { get; protected set; }
	public virtual Decimal? ValorTotalHomologado { get; protected set; }
	public virtual String? SituacaoCompraNome { get; protected set; }
	public virtual DateTime? DataAberturaProposta { get; protected set; }
	public virtual DateTime? DataEncerramentoProposta { get; protected set; }
	public virtual String? AmparoLegalNome { get; protected set; }
	public virtual String? ModoDisputaNome { get; protected set; }
	public virtual String? LinkPncp { get; protected set; }
	public virtual DateTime? DataAtualizacaoGlobal { get; protected set; }
	public virtual Boolean ItensCarregados { get; protected set; }
	public virtual DateTime CriadoEm { get; protected set; }
	public virtual DateTime AtualizadoEm { get; protected set; }

	public virtual Orgao? Orgao { get; protected set; }
	public virtual ICollection<ItemDaCompra> ItensDaCompra { get; protected set; }
}
