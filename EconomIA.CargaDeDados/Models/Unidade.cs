using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("unidade")]
public class Unidade {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_do_orgao")]
	public long IdentificadorDoOrgao { get; set; }

	[Column("codigo_unidade")]
	public string CodigoUnidade { get; set; } = string.Empty;

	[Column("nome_unidade")]
	public string NomeUnidade { get; set; } = string.Empty;

	[Column("municipio_nome")]
	public string? MunicipioNome { get; set; }

	[Column("municipio_codigo_ibge")]
	public string? MunicipioCodigoIbge { get; set; }

	[Column("uf_sigla")]
	public string? UfSigla { get; set; }

	[Column("uf_nome")]
	public string? UfNome { get; set; }

	[Column("status_ativo")]
	public bool StatusAtivo { get; set; } = true;

	[Column("data_inclusao_pncp")]
	public DateTime? DataInclusaoPncp { get; set; }

	[Column("data_atualizacao_pncp")]
	public DateTime? DataAtualizacaoPncp { get; set; }

	[Column("justificativa_atualizacao")]
	public string? JustificativaAtualizacao { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
