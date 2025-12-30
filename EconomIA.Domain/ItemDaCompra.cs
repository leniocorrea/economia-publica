using System;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class ItemDaCompra : Aggregate {
	protected ItemDaCompra() {
	}

	public ItemDaCompra(
		Int64 id,
		Int64 identificadorDaCompra,
		Int32 numeroItem,
		DateTime criadoEm,
		DateTime atualizadoEm,
		String? descricao = null,
		Decimal? quantidade = null,
		String? unidadeMedida = null,
		Decimal? valorUnitarioEstimado = null,
		Decimal? valorTotal = null,
		String? criterioJulgamentoNome = null,
		String? situacaoCompraItemNome = null,
		Boolean temResultado = false,
		DateTime? dataAtualizacao = null) : base(id) {
		IdentificadorDaCompra = identificadorDaCompra;
		NumeroItem = numeroItem;
		CriadoEm = criadoEm;
		AtualizadoEm = atualizadoEm;
		Descricao = descricao;
		Quantidade = quantidade;
		UnidadeMedida = unidadeMedida;
		ValorUnitarioEstimado = valorUnitarioEstimado;
		ValorTotal = valorTotal;
		CriterioJulgamentoNome = criterioJulgamentoNome;
		SituacaoCompraItemNome = situacaoCompraItemNome;
		TemResultado = temResultado;
		DataAtualizacao = dataAtualizacao;
	}

	public virtual Int64 IdentificadorDaCompra { get; protected set; }
	public virtual Int32 NumeroItem { get; protected set; }
	public virtual String? Descricao { get; protected set; }
	public virtual Decimal? Quantidade { get; protected set; }
	public virtual String? UnidadeMedida { get; protected set; }
	public virtual Decimal? ValorUnitarioEstimado { get; protected set; }
	public virtual Decimal? ValorTotal { get; protected set; }
	public virtual String? CriterioJulgamentoNome { get; protected set; }
	public virtual String? SituacaoCompraItemNome { get; protected set; }
	public virtual Boolean TemResultado { get; protected set; }
	public virtual DateTime? DataAtualizacao { get; protected set; }
	public virtual DateTime CriadoEm { get; protected set; }
	public virtual DateTime AtualizadoEm { get; protected set; }

	public virtual Compra? Compra { get; protected set; }
}
