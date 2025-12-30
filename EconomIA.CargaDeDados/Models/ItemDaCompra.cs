using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("item_da_compra")]
public class ItemDaCompra {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_da_compra")]
	public long IdentificadorDaCompra { get; set; }

	[Column("numero_item")]
	public int NumeroItem { get; set; }

	[Column("descricao")]
	public string? Descricao { get; set; }

	[Column("quantidade")]
	public decimal? Quantidade { get; set; }

	[Column("unidade_medida")]
	public string? UnidadeMedida { get; set; }

	[Column("valor_unitario_estimado")]
	public decimal? ValorUnitarioEstimado { get; set; }

	[Column("valor_total")]
	public decimal? ValorTotal { get; set; }

	[Column("criterio_julgamento_nome")]
	public string? CriterioJulgamentoNome { get; set; }

	[Column("situacao_compra_item_nome")]
	public string? SituacaoCompraItemNome { get; set; }

	[Column("tem_resultado")]
	public bool TemResultado { get; set; }

	[Column("data_atualizacao")]
	public DateTime? DataAtualizacao { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
