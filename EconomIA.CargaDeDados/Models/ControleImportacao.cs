using System.ComponentModel.DataAnnotations.Schema;

namespace EconomIA.CargaDeDados.Models;

[Table("controle_de_importacao")]
public class ControleImportacao {
	[Column("identificador")]
	public long Identificador { get; set; }

	[Column("identificador_do_orgao")]
	public long IdentificadorDoOrgao { get; set; }

	[Column("tipo_dado")]
	public string TipoDado { get; set; } = string.Empty;

	[Column("data_inicial_importada")]
	public DateOnly? DataInicialImportada { get; set; }

	[Column("data_final_importada")]
	public DateOnly? DataFinalImportada { get; set; }

	[Column("ultima_execucao")]
	public DateTime UltimaExecucao { get; set; }

	[Column("registros_importados")]
	public int RegistrosImportados { get; set; }

	[Column("status")]
	public string Status { get; set; } = StatusImportacao.Pendente;

	[Column("mensagem_erro")]
	public string? MensagemErro { get; set; }

	[Column("criado_em")]
	public DateTime CriadoEm { get; set; }

	[Column("atualizado_em")]
	public DateTime AtualizadoEm { get; set; }
}

public static class StatusImportacao {
	public const string Pendente = "pendente";
	public const string EmAndamento = "em_andamento";
	public const string Sucesso = "sucesso";
	public const string Erro = "erro";
}
