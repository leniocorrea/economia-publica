using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("ata")]
public class Ata {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_do_orgao")]
	public long IdentificadorDoOrgao { get; set; }

	[Column("numero_controle_pncp_ata")]
	public string NumeroControlePncpAta { get; set; } = string.Empty;

	[Column("numero_controle_pncp_compra")]
	public string? NumeroControlePncpCompra { get; set; }

	[Column("numero_ata_registro_preco")]
	public string? NumeroAtaRegistroPreco { get; set; }

	[Column("ano_ata")]
	public int AnoAta { get; set; }

	[Column("objeto_contratacao")]
	public string? ObjetoContratacao { get; set; }

	[Column("cancelado")]
	public bool Cancelado { get; set; }

	[Column("data_cancelamento")]
	public DateTime? DataCancelamento { get; set; }

	[Column("data_assinatura")]
	public DateTime? DataAssinatura { get; set; }

	[Column("vigencia_inicio")]
	public DateTime? VigenciaInicio { get; set; }

	[Column("vigencia_fim")]
	public DateTime? VigenciaFim { get; set; }

	[Column("data_publicacao_pncp")]
	public DateTime? DataPublicacaoPncp { get; set; }

	[Column("data_inclusao")]
	public DateTime? DataInclusao { get; set; }

	[Column("data_atualizacao")]
	public DateTime? DataAtualizacao { get; set; }

	[Column("data_atualizacao_global")]
	public DateTime? DataAtualizacaoGlobal { get; set; }

	[Column("usuario")]
	public string? Usuario { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
