using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("resultado_do_item")]
public class ResultadoItem {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_do_item_da_compra")]
	public long IdentificadorDoItemDaCompra { get; set; }

	[Column("ni_fornecedor")]
	public string? NiFornecedor { get; set; }

	[Column("nome_razao_social_fornecedor")]
	public string? NomeRazaoSocialFornecedor { get; set; }

	[Column("quantidade_homologada")]
	public decimal? QuantidadeHomologada { get; set; }

	[Column("valor_unitario_homologado")]
	public decimal? ValorUnitarioHomologado { get; set; }

	[Column("valor_total_homologado")]
	public decimal? ValorTotalHomologado { get; set; }

	[Column("situacao_compra_item_resultado_nome")]
	public string? SituacaoCompraItemResultadoNome { get; set; }

	[Column("data_resultado")]
	public DateTime? DataResultado { get; set; }

	[Column("data_atualizacao")]
	public DateTime? DataAtualizacao { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
