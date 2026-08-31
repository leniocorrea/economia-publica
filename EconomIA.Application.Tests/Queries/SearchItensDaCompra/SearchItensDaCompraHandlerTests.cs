using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task descricao_ausente_lista_sem_filtro_de_texto(String? descricao) {
		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);

		searcher.Search(
			Arg.Any<String?>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		var result = await handler.Handle(
			new SearchItensDaCompraQuery.Query(descricao, null, null, null),
			CancellationToken.None);

		result.IsSuccess.Should().BeTrue();

		await searcher.Received(1).Search(
			descricao,
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task resposta_respeita_a_ordem_devolvida_pela_busca() {
		var idsNaOrdemDaBusca = ImmutableArray.Create<Int64>(30, 10, 20);

		searcher.Search(
			Arg.Any<String?>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(
			new SearchResult(idsNaOrdemDaBusca, 3, false)));

		reader.FilterWithCompraAndOrgao(
			Arg.Any<EconomIA.Common.Domain.Specification<ItemDaCompra>>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<ImmutableArray<ItemDaCompra>, RepositoryError>(
			ImmutableArray.Create(CriarItem(10), CriarItem(20), CriarItem(30))));

		var result = await handler.Handle(
			new SearchItensDaCompraQuery.Query(null, null, null, null),
			CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Select(x => x.Id).Should().ContainInOrder(30, 10, 20);
	}

	[Fact]
	public async Task filtros_de_ata_chegam_ao_searcher_mesmo_sem_descricao() {
		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);
		var inicio = new DateTime(2026, 7, 20);
		var fim = new DateTime(2026, 8, 19);

		searcher.Search(
			Arg.Any<String?>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			ApenasComAdesao: true,
			ApenasComAtaVigente: true,
			DataDaAtaInicio: inicio,
			DataDaAtaFim: fim);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();

		await searcher.Received(1).Search(
			null,
			Arg.Is<SearchFilters?>(f =>
				f != null
				&& f.SomenteComAdesao == true
				&& f.SomenteComAtaVigente == true
				&& f.DataDaAtaInicio == inicio
				&& f.DataDaAtaFim == fim),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task filtros_de_ata_acompanham_a_busca_textual() {
		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);

		searcher.Search(
			Arg.Any<String?>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		var query = new SearchItensDaCompraQuery.Query(
			"limpeza", null, null, null,
			ApenasComAtaVigente: true);

		await handler.Handle(query, CancellationToken.None);

		await searcher.Received(1).Search(
			"limpeza",
			Arg.Is<SearchFilters?>(f => f != null && f.SomenteComAtaVigente == true),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task periodo_da_ata_invertido_retorna_erro_sem_consultar_o_searcher() {
		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			DataDaAtaInicio: new DateTime(2026, 8, 19),
			DataDaAtaFim: new DateTime(2026, 7, 20));

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Data inicial da ata");

		await searcher.DidNotReceiveWithAnyArgs().Search(default, default, default, default);
	}

	private static ItemDaCompra CriarItem(Int64 id) {
		return new ItemDaCompra(
			id: id,
			identificadorDaCompra: id,
			numeroItem: 1,
			criadoEm: new DateTime(2026, 1, 1),
			atualizadoEm: new DateTime(2026, 1, 1),
			descricao: $"Item {id}",
			temResultado: true);
	}

	[Fact]
	public async Task descricao_ausente_preserva_os_filtros() {
		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);

		searcher.Search(
			Arg.Any<String?>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			new DateTime(2026, 1, 1),
			new DateTime(2026, 8, 12),
			"municipio",
			"SP",
			null, null, null, null,
			true);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();

		await searcher.Received(1).Search(
			null,
			Arg.Is<SearchFilters?>(f =>
				f!.UfSigla == "SP" &&
				f.RazaoSocial == "municipio" &&
				f.SomenteComAdesao == true &&
				f.DataInclusaoInicio == new DateTime(2026, 1, 1) &&
				f.DataInclusaoFim == new DateTime(2026, 8, 12)),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>());
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
	public async Task objeto_da_compra_e_repassado_ao_buscador() {
		var query = new SearchItensDaCompraQuery.Query(
			"notebook",
			null,
			null,
			null,
			ObjetoDaCompra: "aquisicao de equipamentos de informatica");

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
		filtrosCapturados!.ObjetoDaCompra.Should().Be("aquisicao de equipamentos de informatica");
	}

	[Fact]
	public async Task filtros_de_descricao_e_objeto_da_compra_sao_independentes() {
		var query = new SearchItensDaCompraQuery.Query(
			null,
			null,
			null,
			null,
			ObjetoDaCompra: "merenda escolar");

		var searchResult = new SearchResult(ImmutableArray<Int64>.Empty, 0, false);

		searcher.Search(
			Arg.Any<String?>(),
			Arg.Any<SearchFilters?>(),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<SearchResult, RepositoryError>(searchResult));

		await handler.Handle(query, CancellationToken.None);

		await searcher.Received(1).Search(
			null,
			Arg.Is<SearchFilters?>(f => f != null && f.ObjetoDaCompra == "merenda escolar"),
			Arg.Any<EconomIA.Common.Persistence.Pagination.PaginationParameters?>(),
			Arg.Any<CancellationToken>());
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

	[Fact]
	public async Task chave_da_compra_nao_consulta_o_indice() {
		ConfigurarMocks(
			new SearchResult(ImmutableArray<Int64>.Empty, 0, false),
			ImmutableArray.Create(CriarItemDoEdital(1, 1)));

		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			CnpjOrgao: "88600655000141",
			AnoCompra: 2026,
			SequencialCompra: 316);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();

		await searcher.DidNotReceiveWithAnyArgs().Search(default, default, default, default);
		await reader.Received(1).FilterWithCompraAndOrgao(
			Arg.Any<EconomIA.Common.Domain.Specification<ItemDaCompra>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task chave_da_compra_ordena_os_itens_pelo_numero_do_item() {
		var itens = ImmutableArray.Create(
			CriarItemDoEdital(30, 3),
			CriarItemDoEdital(10, 1),
			CriarItemDoEdital(20, 2));

		ConfigurarMocks(new SearchResult(ImmutableArray<Int64>.Empty, 0, false), itens);

		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			CnpjOrgao: "88600655000141",
			AnoCompra: 2026,
			SequencialCompra: 316);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Select(x => x.NumeroItem).Should().ContainInOrder(1, 2, 3);
		result.Value.TotalHits.Should().Be(3);
		result.Value.HasMoreItems.Should().BeFalse();
	}

	[Fact]
	public async Task chave_da_compra_pagina_os_itens_do_edital() {
		var itens = Enumerable.Range(1, 5)
			.Select(numero => CriarItemDoEdital(numero, numero))
			.ToImmutableArray();

		ConfigurarMocks(new SearchResult(ImmutableArray<Int64>.Empty, 0, false), itens);

		var query = new SearchItensDaCompraQuery.Query(
			null, null, Cursor: "2", Limit: 2,
			CnpjOrgao: "88600655000141",
			AnoCompra: 2026,
			SequencialCompra: 316);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Select(x => x.NumeroItem).Should().ContainInOrder(3, 4);
		result.Value.TotalHits.Should().Be(5);
		result.Value.HasMoreItems.Should().BeTrue();
		result.Value.NextCursor.Should().Be("4");
	}

	[Fact]
	public async Task ultima_pagina_do_edital_nao_devolve_cursor() {
		var itens = Enumerable.Range(1, 3)
			.Select(numero => CriarItemDoEdital(numero, numero))
			.ToImmutableArray();

		ConfigurarMocks(new SearchResult(ImmutableArray<Int64>.Empty, 0, false), itens);

		var query = new SearchItensDaCompraQuery.Query(
			null, null, Cursor: "2", Limit: 2,
			CnpjOrgao: "88600655000141",
			AnoCompra: 2026,
			SequencialCompra: 316);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.HasMoreItems.Should().BeFalse();
		result.Value.NextCursor.Should().BeNull();
	}

	[Theory]
	[InlineData("88600655000141", 2026, null)]
	[InlineData("88600655000141", null, 316)]
	[InlineData(null, 2026, 316)]
	[InlineData("88600655000141", null, null)]
	[InlineData(null, null, 316)]
	public async Task chave_da_compra_incompleta_retorna_erro_sem_consultar_nada(String? cnpjOrgao, Int32? anoCompra, Int32? sequencialCompra) {
		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			CnpjOrgao: cnpjOrgao,
			AnoCompra: anoCompra,
			SequencialCompra: sequencialCompra);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("cnpjOrgao, anoCompra e sequencialCompra");

		await searcher.DidNotReceiveWithAnyArgs().Search(default, default, default, default);
		await reader.DidNotReceiveWithAnyArgs().FilterWithCompraAndOrgao(default!, default);
	}

	[Fact]
	public async Task chave_da_compra_mantem_o_filtro_de_valor_homologado() {
		var itens = ImmutableArray.Create(
			CriarItemComResultado(1, 400m, null),
			CriarItemComResultado(2, 600m, null));

		ConfigurarMocks(new SearchResult(ImmutableArray<Int64>.Empty, 0, false), itens);

		var query = new SearchItensDaCompraQuery.Query(
			null, null, null, null,
			null, null, null, null,
			500m, null, null, null,
			CnpjOrgao: "88600655000141",
			AnoCompra: 2026,
			SequencialCompra: 316);

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Id.Should().Be(2);
		result.Value.TotalHits.Should().Be(1);
	}

	private static ItemDaCompra CriarItemDoEdital(Int64 id, Int32 numeroItem) {
		return new ItemDaCompra(
			id,
			316,
			numeroItem,
			DateTime.UtcNow,
			DateTime.UtcNow,
			$"Item {numeroItem} do edital");
	}
}
