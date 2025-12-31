using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("compra")]
public class Compra {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_do_orgao")]
	public long IdentificadorDoOrgao { get; set; }

	[Column("numero_controle_pncp")]
	public string NumeroControlePncp { get; set; } = string.Empty;

	[Column("ano_compra")]
	public int AnoCompra { get; set; }

	[Column("sequencial_compra")]
	public int SequencialCompra { get; set; }

	[Column("modalidade_identificador")]
	public int ModalidadeIdentificador { get; set; }

	[Column("modalidade_nome")]
	public string? ModalidadeNome { get; set; }

	[Column("objeto_compra")]
	public string? ObjetoCompra { get; set; }

	[Column("valor_total_estimado")]
	public decimal? ValorTotalEstimado { get; set; }

	[Column("valor_total_homologado")]
	public decimal? ValorTotalHomologado { get; set; }

	[Column("situacao_compra_nome")]
	public string? SituacaoCompraNome { get; set; }

	[Column("data_inclusao")]
	public DateTime? DataInclusao { get; set; }

	[Column("data_abertura_proposta")]
	public DateTime? DataAberturaProposta { get; set; }

	[Column("data_encerramento_proposta")]
	public DateTime? DataEncerramentoProposta { get; set; }

	[Column("amparo_legal_nome")]
	public string? AmparoLegalNome { get; set; }

	[Column("modo_disputa_nome")]
	public string? ModoDisputaNome { get; set; }

	[Column("link_pncp")]
	public string? LinkPncp { get; set; }

	[Column("data_atualizacao_global")]
	public DateTime? DataAtualizacaoGlobal { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("itens_carregados")]
	public bool ItensCarregados { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
