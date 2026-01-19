using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("execucao_carga")]
public class ExecucaoCarga {
	[Column("identificador")]
	public Int64 Identificador { get; set; }

	[Column("modo_execucao")]
	public String ModoExecucao { get; set; } = String.Empty;

	[Column("tipo_gatilho")]
	public String TipoGatilho { get; set; } = String.Empty;

	[Column("inicio_em")]
	public DateTime InicioEm { get; set; }

	[Column("fim_em")]
	public DateTime? FimEm { get; set; }

	[Column("duracao_total_ms")]
	public Int64? DuracaoTotalMs { get; set; }

	[Column("status")]
	public String Status { get; set; } = StatusExecucao.EmAndamento;

	[Column("mensagem_erro")]
	public String? MensagemErro { get; set; }

	[Column("stack_trace")]
	public String? StackTrace { get; set; }

	[Column("total_orgaos_processados")]
	public Int32 TotalOrgaosProcessados { get; set; }

	[Column("total_orgaos_com_erro")]
	public Int32 TotalOrgaosComErro { get; set; }

	[Column("total_compras_processadas")]
	public Int32 TotalComprasProcessadas { get; set; }

	[Column("total_contratos_processados")]
	public Int32 TotalContratosProcessados { get; set; }

	[Column("total_atas_processadas")]
	public Int32 TotalAtasProcessadas { get; set; }

	[Column("total_itens_indexados")]
	public Int32 TotalItensIndexados { get; set; }

	[Column("versao_aplicacao")]
	public String? VersaoAplicacao { get; set; }

	[Column("hostname")]
	public String? Hostname { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("parametros")]
	public String? ParametrosJson { get; set; }

	public ParametrosExecucao? Parametros => String.IsNullOrEmpty(ParametrosJson)
		? null
		: System.Text.Json.JsonSerializer.Deserialize<ParametrosExecucao>(ParametrosJson);
}

public record ParametrosExecucao(
	Int32? DiasRetroativos,
	String[]? Cnpjs
) {
	public String ToJson() => System.Text.Json.JsonSerializer.Serialize(this);
}

public static class StatusExecucao {
	public const String Pendente = "pendente";
	public const String EmAndamento = "em_andamento";
	public const String Sucesso = "sucesso";
	public const String Erro = "erro";
	public const String Parcial = "parcial";
	public const String Cancelado = "cancelado";
}

public static class ModoExecucao {
	public const String Diaria = "diaria";
	public const String Incremental = "incremental";
	public const String Manual = "manual";
	public const String Orgaos = "orgaos";
	public const String Brasil = "brasil";
}

public static class TipoGatilho {
	public const String Scheduler = "scheduler";
	public const String Cli = "cli";
	public const String Api = "api";
}
