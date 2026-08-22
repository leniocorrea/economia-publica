using System;
using Xunit;

namespace EconomIA.Adapters.Tests.Integracao;

public static class AmbienteDeIntegracao {
	public const String VariavelElasticsearch = "ECONOMIA_TESTES_ELASTICSEARCH_URL";

	public static String? UrlDoElasticsearch => Valor(VariavelElasticsearch);

	private static String? Valor(String variavel) {
		var valor = Environment.GetEnvironmentVariable(variavel);
		return String.IsNullOrWhiteSpace(valor) ? null : valor;
	}
}

public sealed class FatoDeIntegracaoComElasticsearchAttribute : FactAttribute {
	public FatoDeIntegracaoComElasticsearchAttribute() {
		if (AmbienteDeIntegracao.UrlDoElasticsearch is null) {
			Skip = $"Defina {AmbienteDeIntegracao.VariavelElasticsearch} para rodar os testes integrados com Elasticsearch.";
		}
	}
}
