using Xunit;

namespace EconomIA.CargaDeDados.Tests.Integracao;

public static class AmbienteDeIntegracao {
	public const String VariavelElasticsearch = "ECONOMIA_TESTES_ELASTICSEARCH_URL";
	public const String VariavelPostgres = "ECONOMIA_TESTES_POSTGRES";

	public static String? UrlDoElasticsearch => Valor(VariavelElasticsearch);
	public static String? ConnectionStringDoPostgres => Valor(VariavelPostgres);

	public static Boolean Completo => UrlDoElasticsearch is not null && ConnectionStringDoPostgres is not null;

	public static String MotivoDoSkip =>
		$"Defina {VariavelElasticsearch} e {VariavelPostgres} para rodar os testes integrados de enriquecimento.";

	private static String? Valor(String variavel) {
		var valor = Environment.GetEnvironmentVariable(variavel);
		return String.IsNullOrWhiteSpace(valor) ? null : valor;
	}
}

public sealed class FatoDeIntegracaoCompletaAttribute : FactAttribute {
	public FatoDeIntegracaoCompletaAttribute() {
		if (!AmbienteDeIntegracao.Completo) {
			Skip = AmbienteDeIntegracao.MotivoDoSkip;
		}
	}
}

public sealed class TeoriaDeIntegracaoCompletaAttribute : TheoryAttribute {
	public TeoriaDeIntegracaoCompletaAttribute() {
		if (!AmbienteDeIntegracao.Completo) {
			Skip = AmbienteDeIntegracao.MotivoDoSkip;
		}
	}
}
