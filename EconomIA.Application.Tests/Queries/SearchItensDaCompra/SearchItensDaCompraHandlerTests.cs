using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Persistence;
using EconomIA.Common.Results;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

using SearchItensDaCompraQuery = EconomIA.Application.Queries.SearchItensDaCompra.SearchItensDaCompra;

namespace EconomIA.Application.Tests.Queries.SearchItensDaCompra;

public class SearchItensDaCompraHandlerTests {
	private readonly IItensDaCompraSearcher searcher;
	private readonly IItensDaCompraReader reader;
	private readonly IAtasReader atasReader;
	private readonly IContratosReader contratosReader;
	private readonly SearchItensDaCompraQuery.Handler handler;

	public SearchItensDaCompraHandlerTests() {
		searcher = Substitute.For<IItensDaCompraSearcher>();
		reader = Substitute.For<IItensDaCompraReader>();
		atasReader = Substitute.For<IAtasReader>();
		contratosReader = Substitute.For<IContratosReader>();
		handler = new SearchItensDaCompraQuery.Handler(searcher, reader, atasReader, contratosReader);
	}

	[Fact]
	public async Task descricao_vazia_retorna_falha() {
		var query = new SearchItensDaCompraQuery.Query("", null, null, null);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Descrição é obrigatória");
	}

	[Fact]
	public async Task descricao_apenas_espacos_retorna_falha() {
		var query = new SearchItensDaCompraQuery.Query("   ", null, null, null);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Descrição é obrigatória");
	}

	[Fact]
	public async Task busca_sem_resultados_retorna_resposta_vazia() {
		var query = new SearchItensDaCompraQuery.Query("teste", null, null, null);
		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);

		searcher.Search(
			Arg.Any<String>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().BeEmpty();
		result.Value.TotalHits.Should().Be(0);
	}

	[Fact]
	public async Task passa_filtros_para_o_buscador() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico",
			null,
			null,
			null,
			new DateTime(2025, 1, 1),
			new DateTime(2025, 12, 31),
			"Prefeitura",
			"MG",
			100m,
			1000m,
			500m,
			5000m);

		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);

		SearchFilters? filtrosCapturados = null;
		searcher.Search(
			Arg.Any<String>(),
			Arg.Do<SearchFilters?>(f => filtrosCapturados = f),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		await handler.Handle(query, CancellationToken.None);

		filtrosCapturados.Should().NotBeNull();
		filtrosCapturados!.DataInclusaoInicio.Should().Be(new DateTime(2025, 1, 1));
		filtrosCapturados.DataInclusaoFim.Should().Be(new DateTime(2025, 12, 31));
		filtrosCapturados.RazaoSocial.Should().Be("Prefeitura");
		filtrosCapturados.UfSigla.Should().Be("MG");
		filtrosCapturados.ValorUnitarioHomologadoMinimo.Should().Be(100m);
		filtrosCapturados.ValorUnitarioHomologadoMaximo.Should().Be(1000m);
		filtrosCapturados.ValorTotalHomologadoMinimo.Should().Be(500m);
		filtrosCapturados.ValorTotalHomologadoMaximo.Should().Be(5000m);
	}

	[Fact]
	public async Task filtra_itens_por_valor_unitario_homologado_minimo() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			500m, null, null, null);

		var ids = ImmutableArray.Create(1L, 2L);
		var searchResult = new SearchResult(ids, 2, false);

		var item1 = CriarItemComResultado(1, 400m, null);
		var item2 = CriarItemComResultado(2, 600m, null);
		var items = ImmutableArray.Create(item1, item2);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(2);
	}

	[Fact]
	public async Task filtra_itens_por_valor_unitario_homologado_maximo() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			null, 500m, null, null);

		var ids = ImmutableArray.Create(1L, 2L);
		var searchResult = new SearchResult(ids, 2, false);

		var item1 = CriarItemComResultado(1, 400m, null);
		var item2 = CriarItemComResultado(2, 600m, null);
		var items = ImmutableArray.Create(item1, item2);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(1);
	}

	[Fact]
	public async Task filtra_itens_por_valor_total_homologado_minimo() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			null, null, 1000m, null);

		var ids = ImmutableArray.Create(1L, 2L);
		var searchResult = new SearchResult(ids, 2, false);

		var item1 = CriarItemComResultado(1, null, 500m);
		var item2 = CriarItemComResultado(2, null, 1500m);
		var items = ImmutableArray.Create(item1, item2);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(2);
	}

	[Fact]
	public async Task filtra_itens_por_valor_total_homologado_maximo() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			null, null, null, 1000m);

		var ids = ImmutableArray.Create(1L, 2L);
		var searchResult = new SearchResult(ids, 2, false);

		var item1 = CriarItemComResultado(1, null, 500m);
		var item2 = CriarItemComResultado(2, null, 1500m);
		var items = ImmutableArray.Create(item1, item2);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(1);
	}

	[Fact]
	public async Task filtra_itens_por_faixa_de_valor() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			100m, 500m, null, null);

		var ids = ImmutableArray.Create(1L, 2L, 3L);
		var searchResult = new SearchResult(ids, 3, false);

		var item1 = CriarItemComResultado(1, 50m, null);
		var item2 = CriarItemComResultado(2, 300m, null);
		var item3 = CriarItemComResultado(3, 600m, null);
		var items = ImmutableArray.Create(item1, item2, item3);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(2);
	}

	[Fact]
	public async Task item_com_multiplos_resultados_filtra_se_algum_atender_criterio() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			500m, null, null, null);

		var ids = ImmutableArray.Create(1L);
		var searchResult = new SearchResult(ids, 1, false);

		var item = CriarItemComMultiplosResultados(1, new[] { 100m, 600m, 200m });
		var items = ImmutableArray.Create(item);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
	}

	[Fact]
	public async Task item_sem_resultados_exclui_do_filtro_de_valor() {
		var query = new SearchItensDaCompraQuery.Query(
			"servico", null, null, null,
			null, null, null, null,
			500m, null, null, null);

		var ids = ImmutableArray.Create(1L, 2L);
		var searchResult = new SearchResult(ids, 2, false);

		var item1 = CriarItemSemResultados(1);
		var item2 = CriarItemComResultado(2, 600m, null);
		var items = ImmutableArray.Create(item1, item2);

		ConfigurarMocks(searchResult, items);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(2);
	}

	private void ConfigurarMocks(SearchResult searchResult, ImmutableArray<ItemDaCompra> items) {
		searcher.Search(
			Arg.Any<String>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		reader.FilterWithCompraAndOrgao(
			Arg.Any<EconomIA.Common.Domain.Specification<ItemDaCompra>>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<ImmutableArray<ItemDaCompra>, RepositoryError>(items));

		atasReader.Filter(
			Arg.Any<EconomIA.Common.Domain.Specification<Ata>>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<ImmutableArray<Ata>, RepositoryError>(ImmutableArray<Ata>.Empty));

		contratosReader.Filter(
			Arg.Any<EconomIA.Common.Domain.Specification<Contrato>>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<ImmutableArray<Contrato>, RepositoryError>(ImmutableArray<Contrato>.Empty));
	}

	private static ItemDaCompra CriarItemComResultado(Int64 id, Decimal? valorUnitario, Decimal? valorTotal) {
		var item = new ItemDaCompra(
			id,
			100,
			1,
			DateTime.UtcNow,
			DateTime.UtcNow,
			"Serviço de teste");

		var resultado = new ResultadoDoItem(
			id * 10,
			id,
			DateTime.UtcNow,
			DateTime.UtcNow,
			valorUnitarioHomologado: valorUnitario,
			valorTotalHomologado: valorTotal);

		item.Resultados.Add(resultado);
		return item;
	}

	private static ItemDaCompra CriarItemComMultiplosResultados(Int64 id, Decimal[] valoresUnitarios) {
		var item = new ItemDaCompra(
			id,
			100,
			1,
			DateTime.UtcNow,
			DateTime.UtcNow,
			"Serviço de teste");

		for (var i = 0; i < valoresUnitarios.Length; i++) {
			var resultado = new ResultadoDoItem(
				id * 10 + i,
				id,
				DateTime.UtcNow,
				DateTime.UtcNow,
				valorUnitarioHomologado: valoresUnitarios[i]);

			item.Resultados.Add(resultado);
		}

		return item;
	}

	private static ItemDaCompra CriarItemSemResultados(Int64 id) {
		return new ItemDaCompra(
			id,
			100,
			1,
			DateTime.UtcNow,
			DateTime.UtcNow,
			"Serviço de teste");
	}
}
