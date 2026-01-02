using System;
using EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;
using EconomIA.Domain.Repositories;
using FluentAssertions;
using Xunit;

namespace EconomIA.Adapters.Tests.Persistence.Repositories.ItensDaCompra;

public class ElasticsearchItemSearcherTests {
	[Fact]
	public void apenas_descricao_retorna_bool_query_com_uma_clausula() {
		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", null);

		boolQuery.Should().NotBeNull();
		boolQuery.Must.Should().HaveCount(1);
	}

	[Fact]
	public void com_razao_social_adiciona_match_query_para_orgao() {
		var filtros = new SearchFilters(
			null, null,
			"Prefeitura",
			null, null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_uf_sigla_adiciona_term_query() {
		var filtros = new SearchFilters(
			null, null, null,
			"MG",
			null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_uf_sigla_minuscula_converte_para_maiuscula() {
		var filtros = new SearchFilters(
			null, null, null,
			"mg",
			null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_data_inclusao_inicio_adiciona_date_range_query() {
		var filtros = new SearchFilters(
			new DateTime(2025, 1, 1),
			null, null, null, null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_data_inclusao_fim_adiciona_date_range_query() {
		var filtros = new SearchFilters(
			null,
			new DateTime(2025, 12, 31),
			null, null, null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_faixa_de_data_inclusao_adiciona_unico_date_range_query() {
		var filtros = new SearchFilters(
			new DateTime(2025, 1, 1),
			new DateTime(2025, 12, 31),
			null, null, null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_todos_os_filtros_adiciona_todas_as_queries() {
		var filtros = new SearchFilters(
			new DateTime(2025, 1, 1),
			new DateTime(2025, 12, 31),
			"Prefeitura",
			"MG",
			100m, 1000m, 500m, 5000m);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(4);
	}

	[Fact]
	public void com_razao_social_vazia_nao_adiciona_match_query() {
		var filtros = new SearchFilters(
			null, null,
			"",
			null, null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(1);
	}

	[Fact]
	public void com_razao_social_apenas_espacos_nao_adiciona_match_query() {
		var filtros = new SearchFilters(
			null, null,
			"   ",
			null, null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(1);
	}

	[Fact]
	public void com_uf_sigla_vazia_nao_adiciona_term_query() {
		var filtros = new SearchFilters(
			null, null, null,
			"",
			null, null, null, null);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("servico", filtros);

		boolQuery.Must.Should().HaveCount(1);
	}
}
