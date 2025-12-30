using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("contrato")]
public class Contrato {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_do_orgao")]
	public long IdentificadorDoOrgao { get; set; }

	[Column("numero_controle_pncp")]
	public string NumeroControlePncp { get; set; } = string.Empty;

	[Column("numero_controle_pncp_compra")]
	public string? NumeroControlePncpCompra { get; set; }

	[Column("ano_contrato")]
	public int AnoContrato { get; set; }

	[Column("sequencial_contrato")]
	public int SequencialContrato { get; set; }

	[Column("numero_contrato_empenho")]
	public string? NumeroContratoEmpenho { get; set; }

	[Column("processo")]
	public string? Processo { get; set; }

	[Column("objeto_contrato")]
	public string? ObjetoContrato { get; set; }

	[Column("tipo_contrato_id")]
	public int? TipoContratoId { get; set; }

	[Column("tipo_contrato_nome")]
	public string? TipoContratoNome { get; set; }

	[Column("categoria_processo_id")]
	public int? CategoriaProcessoId { get; set; }

	[Column("categoria_processo_nome")]
	public string? CategoriaProcessoNome { get; set; }

	[Column("ni_fornecedor")]
	public string? NiFornecedor { get; set; }

	[Column("nome_razao_social_fornecedor")]
	public string? NomeRazaoSocialFornecedor { get; set; }

	[Column("tipo_pessoa")]
	public string? TipoPessoa { get; set; }

	[Column("valor_inicial")]
	public decimal? ValorInicial { get; set; }

	[Column("valor_global")]
	public decimal? ValorGlobal { get; set; }

	[Column("valor_parcela")]
	public decimal? ValorParcela { get; set; }

	[Column("valor_acumulado")]
	public decimal? ValorAcumulado { get; set; }

	[Column("numero_parcelas")]
	public int? NumeroParcelas { get; set; }

	[Column("data_assinatura")]
	public DateTime? DataAssinatura { get; set; }

	[Column("data_vigencia_inicio")]
	public DateTime? DataVigenciaInicio { get; set; }

	[Column("data_vigencia_fim")]
	public DateTime? DataVigenciaFim { get; set; }

	[Column("data_publicacao_pncp")]
	public DateTime? DataPublicacaoPncp { get; set; }

	[Column("data_atualizacao")]
	public DateTime? DataAtualizacao { get; set; }

	[Column("data_atualizacao_global")]
	public DateTime? DataAtualizacaoGlobal { get; set; }

	[Column("receita")]
	public bool Receita { get; set; }

	[Column("informacao_complementar")]
	public string? InformacaoComplementar { get; set; }

	[Column("usuario_nome")]
	public string? UsuarioNome { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}
