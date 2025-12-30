using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("orgao")]
public class Orgao {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("cnpj")]
	public string Cnpj { get; set; } = string.Empty;

	[Column("razao_social")]
	public string RazaoSocial { get; set; } = string.Empty;

	[Column("nome_fantasia")]
	public string? NomeFantasia { get; set; }

	[Column("codigo_natureza_juridica")]
	public string? CodigoNaturezaJuridica { get; set; }

	[Column("descricao_natureza_juridica")]
	public string? DescricaoNaturezaJuridica { get; set; }

	[Column("poder_id")]
	public string? PoderId { get; set; }

	[Column("esfera_id")]
	public string? EsferaId { get; set; }

	[Column("situacao_cadastral")]
	public string? SituacaoCadastral { get; set; }

	[Column("motivo_situacao_cadastral")]
	public string? MotivoSituacaoCadastral { get; set; }

	[Column("data_situacao_cadastral")]
	public DateTime? DataSituacaoCadastral { get; set; }

	[Column("data_validacao")]
	public DateTime? DataValidacao { get; set; }

	[Column("validado")]
	public bool Validado { get; set; }

	[Column("data_inclusao_pncp")]
	public DateTime? DataInclusaoPncp { get; set; }

	[Column("data_atualizacao_pncp")]
	public DateTime? DataAtualizacaoPncp { get; set; }

	[Column("status_ativo")]
	public bool StatusAtivo { get; set; } = true;

	[Column("justificativa_atualizacao")]
	public string? JustificativaAtualizacao { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
