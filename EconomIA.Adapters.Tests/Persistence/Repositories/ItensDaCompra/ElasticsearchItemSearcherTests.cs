using System;
using System.Linq;
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

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void sem_descricao_usa_match_all(String? descricao) {
		var boolQuery = ElasticsearchItemSearcher.BuildQuery(descricao, null);

		boolQuery.Must.Should().HaveCount(1);

		var clausula = boolQuery.Must!.First();
		clausula.TryGet<Elastic.Clients.Elasticsearch.QueryDsl.MatchAllQuery>(out var matchAll).Should().BeTrue();
		matchAll.Should().NotBeNull();
	}

	[Fact]
	public void sem_descricao_mantem_os_demais_filtros() {
		var filtros = new SearchFilters(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 8, 12),
			"Prefeitura",
			"SP",
			null, null, null, null,
			true);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery(null, filtros);

		boolQuery.Must.Should().HaveCount(5);
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

	[Fact]
	public void com_somente_com_adesao_adiciona_date_range_query_de_vigencia() {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			SomenteComAdesao: true);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("notebook", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void sem_somente_com_adesao_nao_adiciona_filtro() {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			SomenteComAdesao: false);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("notebook", filtros);

		boolQuery.Must.Should().HaveCount(1);
	}

	[Fact]
	public void com_somente_com_ata_vigente_adiciona_date_range_query_de_vigencia_da_ata() {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			SomenteComAtaVigente: true);

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("notebook", filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void com_periodo_da_ata_adiciona_unico_date_range_query() {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			DataDaAtaInicio: new DateTime(2026, 7, 20),
			DataDaAtaFim: new DateTime(2026, 8, 19));

		var boolQuery = ElasticsearchItemSearcher.BuildQuery(null, filtros);

		boolQuery.Must.Should().HaveCount(2);
	}

	[Fact]
	public void filtros_de_ata_somam_se_aos_demais_filtros() {
		var filtros = new SearchFilters(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 8, 12),
			"Prefeitura",
			"SP",
			null, null, null, null,
			SomenteComAdesao: true,
			SomenteComAtaVigente: true,
			DataDaAtaInicio: new DateTime(2026, 7, 20));

		var boolQuery = ElasticsearchItemSearcher.BuildQuery("notebook", filtros);

		boolQuery.Must.Should().HaveCount(7);
	}

	[Fact]
	public void com_descricao_nao_define_ordenacao_e_fica_por_relevancia() {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			SomenteComAtaVigente: true);

		ElasticsearchItemSearcher.CampoDeOrdenacao("notebook", filtros).Should().BeNull();
	}

	[Fact]
	public void sem_descricao_e_sem_filtro_de_ata_ordena_por_data_de_inclusao() {
		var filtros = new SearchFilters(null, null, null, "SP", null, null, null, null);

		ElasticsearchItemSearcher.CampoDeOrdenacao(null, filtros).Should().Be(ElasticsearchItemSearcher.CampoDataInclusao);
		ElasticsearchItemSearcher.CampoDeOrdenacao("  ", null).Should().Be(ElasticsearchItemSearcher.CampoDataInclusao);
	}

	[Theory]
	[InlineData(true, null, false, false)]
	[InlineData(null, true, false, false)]
	[InlineData(null, null, true, false)]
	[InlineData(null, null, false, true)]
	public void sem_descricao_com_qualquer_filtro_de_ata_ordena_pela_data_da_ata(Boolean? somenteComAdesao, Boolean? somenteComAtaVigente, Boolean comInicio, Boolean comFim) {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			somenteComAdesao,
			somenteComAtaVigente,
			comInicio ? new DateTime(2026, 7, 20) : null,
			comFim ? new DateTime(2026, 8, 19) : null);

		ElasticsearchItemSearcher.CampoDeOrdenacao(null, filtros).Should().Be(ElasticsearchItemSearcher.CampoAtaDataDeReferencia);
	}

	[Fact]
	public void filtros_de_ata_desligados_nao_contam_como_filtro_de_ata() {
		var filtros = new SearchFilters(
			null, null, null, null, null, null, null, null,
			SomenteComAdesao: false,
			SomenteComAtaVigente: false);

		filtros.FiltraPorAta.Should().BeFalse();
		ElasticsearchItemSearcher.BuildQuery(null, filtros).Must.Should().HaveCount(1);
	}
}
