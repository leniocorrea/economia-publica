using System.Collections.Concurrent;
using System.Diagnostics;

namespace EconomIA.CargaDeDados.Observability;

public class MetricasExecucao {
	private readonly ConcurrentDictionary<Int64, MetricasOrgao> metricasPorOrgao = new();
	private readonly Stopwatch cronometro = Stopwatch.StartNew();

	private Int32? totalComprasOverride;
	private Int32? totalContratosOverride;
	private Int32? totalAtasOverride;
	private Int32? totalItensOverride;

	public Int64 DuracaoTotalMs => cronometro.ElapsedMilliseconds;

	public Int32 TotalOrgaosProcessados => metricasPorOrgao.Count;
	public Int32 TotalOrgaosComErro => metricasPorOrgao.Values.Count(x => x.Status == "erro");

	public Int32 TotalComprasProcessadas {
		get => totalComprasOverride ?? metricasPorOrgao.Values.Sum(x => x.ComprasProcessadas);
		set => totalComprasOverride = value;
	}

	public Int32 TotalContratosProcessados {
		get => totalContratosOverride ?? metricasPorOrgao.Values.Sum(x => x.ContratosProcessados);
		set => totalContratosOverride = value;
	}

	public Int32 TotalAtasProcessadas {
		get => totalAtasOverride ?? metricasPorOrgao.Values.Sum(x => x.AtasProcessadas);
		set => totalAtasOverride = value;
	}

	public Int32 TotalItensIndexados {
		get => totalItensOverride ?? metricasPorOrgao.Values.Sum(x => x.ItensProcessados);
		set => totalItensOverride = value;
	}

	public MetricasOrgao ObterOuCriarMetricasOrgao(Int64 identificadorDoOrgao) {
		return metricasPorOrgao.GetOrAdd(identificadorDoOrgao, id => new MetricasOrgao(id));
	}

	public IEnumerable<MetricasOrgao> ObterTodasMetricas() => metricasPorOrgao.Values;

	public void Finalizar() {
		cronometro.Stop();
	}
}

public class MetricasOrgao {
	public Int64 IdentificadorDoOrgao { get; }
	public String Status { get; set; } = "em_andamento";
	public String? MensagemErro { get; set; }

	public DateTime InicioEm { get; set; } = DateTime.UtcNow;
	public DateTime? FimEm { get; set; }
	public Int64 DuracaoMs => FimEm.HasValue ? (Int64)(FimEm.Value - InicioEm).TotalMilliseconds : 0;

	public Int32 ComprasProcessadas { get; set; }
	public Int64 ComprasDuracaoMs { get; set; }

	public Int32 ContratosProcessados { get; set; }
	public Int64 ContratosDuracaoMs { get; set; }

	public Int32 AtasProcessadas { get; set; }
	public Int64 AtasDuracaoMs { get; set; }

	public Int32 ItensProcessados { get; set; }

	public DateTime? DataInicialProcessada { get; set; }
	public DateTime? DataFinalProcessada { get; set; }

	public MetricasOrgao(Int64 identificadorDoOrgao) {
		IdentificadorDoOrgao = identificadorDoOrgao;
	}

	public void Finalizar(String status, String? mensagemErro = null) {
		FimEm = DateTime.UtcNow;
		Status = status;
		MensagemErro = mensagemErro;
	}
}
