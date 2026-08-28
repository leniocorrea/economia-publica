using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class ExecucaoCarga : Aggregate {
	public String ModoExecucao { get; set; } = String.Empty;
	public String TipoGatilho { get; set; } = String.Empty;
	public DateTime? InicioEm { get; set; }
	public DateTime? FimEm { get; set; }
	public Int64? DuracaoTotalMs { get; set; }
	public String Status { get; set; } = String.Empty;
	public String? MensagemErro { get; set; }
	public String? StackTrace { get; set; }
	public Int32 TotalOrgaosProcessados { get; set; }
	public Int32 TotalOrgaosComErro { get; set; }
	public Int32 TotalComprasProcessadas { get; set; }
	public Int32 TotalContratosProcessados { get; set; }
	public Int32 TotalAtasProcessadas { get; set; }
	public Int32 TotalItensIndexados { get; set; }
	public String? VersaoAplicacao { get; set; }
	public String? Hostname { get; set; }
	public DateTime CriadoEm { get; set; }
	public String? ParametrosJson { get; set; }

	public ICollection<ExecucaoCargaOrgao> Orgaos { get; set; } = new List<ExecucaoCargaOrgao>();

	public static ExecucaoCarga CriarPendente(String modoExecucao, String tipoGatilho, ParametrosExecucao? parametros = null) {
		return new ExecucaoCarga {
			ModoExecucao = modoExecucao,
			TipoGatilho = tipoGatilho,
			Status = StatusExecucao.Pendente,
			ParametrosJson = parametros is not null ? JsonSerializer.Serialize(parametros) : null,
			VersaoAplicacao = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
			Hostname = Environment.MachineName,
			CriadoEm = DateTime.UtcNow
		};
	}

	public Boolean PodeCancelar() => Status == StatusExecucao.Pendente || Status == StatusExecucao.EmAndamento;

	public void Cancelar() {
		Status = StatusExecucao.Cancelado;
		FimEm = DateTime.UtcNow;
		MensagemErro = "Cancelado pelo usuário";
	}
}

public record ParametrosExecucao(
	Int32? DiasRetroativos,
	String[]? Cnpjs,
	Boolean CarregarCompras = true,
	Boolean CarregarContratos = true,
	Boolean CarregarAtas = true
);

public static class StatusExecucao {
	public const String Pendente = "pendente";
	public const String EmAndamento = "em_andamento";
	public const String Sucesso = "sucesso";
	public const String Erro = "erro";
	public const String Parcial = "parcial";
	public const String Cancelado = "cancelado";
}

public static class ModoExecucaoTipo {
	public const String Diaria = "diaria";
	public const String Incremental = "incremental";
	public const String Manual = "manual";
	public const String Orgaos = "orgaos";
	public const String Brasil = "brasil";
	public const String Reconciliacao = "reconciliacao";
	public const String Enriquecimento = "enriquecimento";
	public const String EnriquecimentoObjeto = "enriquecimento_objeto";

	public static readonly String[] ModosCargaAutomatica = [Incremental, Brasil];

	public static Boolean EhModoCargaAutomaticaValido(String modo) => ModosCargaAutomatica.Contains(modo);
}

public static class TipoGatilhoTipo {
	public const String Scheduler = "scheduler";
	public const String Cli = "cli";
	public const String Api = "api";
}

public class ExecucaoCargaOrgao : Entity {
	public Int64 IdentificadorDaExecucao { get; set; }
	public Int64 IdentificadorDoOrgao { get; set; }
	public DateTime InicioEm { get; set; }
	public DateTime? FimEm { get; set; }
	public Int64? DuracaoMs { get; set; }
	public String Status { get; set; } = String.Empty;
	public String? MensagemErro { get; set; }
	public Int32 ComprasProcessadas { get; set; }
	public Int64 ComprasDuracaoMs { get; set; }
	public Int32 ContratosProcessados { get; set; }
	public Int64 ContratosDuracaoMs { get; set; }
	public Int32 AtasProcessadas { get; set; }
	public Int64 AtasDuracaoMs { get; set; }
	public Int32 ItensProcessados { get; set; }
	public DateTime? DataInicialProcessada { get; set; }
	public DateTime? DataFinalProcessada { get; set; }
	public DateTime CriadoEm { get; set; }

	public ExecucaoCarga? Execucao { get; set; }
	public Orgao? Orgao { get; set; }
}
