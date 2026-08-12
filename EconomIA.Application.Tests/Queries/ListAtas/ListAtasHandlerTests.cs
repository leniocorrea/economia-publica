using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Common.Results;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

using ListAtasQuery = EconomIA.Application.Queries.ListAtas.ListAtas;

namespace EconomIA.Application.Tests.Queries.ListAtas;

public class ListAtasHandlerTests {
	private readonly IAtasReader atas;
	private readonly ListAtasQuery.Handler handler;
	private readonly DateTime hoje = DateTime.UtcNow.AddHours(-3).Date;

	public ListAtasHandlerTests() {
		atas = Substitute.For<IAtasReader>();
		handler = new ListAtasQuery.Handler(atas);
	}

	[Fact]
	public async Task data_inicial_maior_que_final_retorna_falha() {
		var query = new ListAtasQuery.Query(
			DataInicio: new DateTime(2026, 8, 10),
			DataFim: new DateTime(2026, 8, 1));

		var result = await handler.Handle(query, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Data inicial");
	}

	[Fact]
	public async Task sem_datas_aplica_periodo_padrao_de_sete_dias() {
		ConfigurarRetorno();

		var result = await handler.Handle(new ListAtasQuery.Query(), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.PeriodoInicio.Should().Be(hoje.AddDays(-7));
		result.Value.PeriodoFim.Should().Be(hoje.AddDays(1).AddTicks(-1));
	}

	[Fact]
	public async Task datas_informadas_sao_normalizadas_para_utc() {
		ConfigurarRetorno();

		var result = await handler.Handle(new ListAtasQuery.Query(
			DataInicio: new DateTime(2026, 8, 1),
			DataFim: new DateTime(2026, 8, 10)), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.PeriodoInicio.Kind.Should().Be(DateTimeKind.Utc);
		result.Value.PeriodoFim.Kind.Should().Be(DateTimeKind.Utc);
	}

	[Fact]
	public async Task periodo_padrao_tambem_e_utc() {
		ConfigurarRetorno();

		var result = await handler.Handle(new ListAtasQuery.Query(), CancellationToken.None);

		result.Value.PeriodoInicio.Kind.Should().Be(DateTimeKind.Utc);
		result.Value.PeriodoFim.Kind.Should().Be(DateTimeKind.Utc);
	}

	[Fact]
	public async Task periodo_filtra_pela_data_de_assinatura() {
		var filtro = await CapturarFiltro(new ListAtasQuery.Query());

		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-2))).Should().BeTrue();
		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-30))).Should().BeFalse();
	}

	[Fact]
	public async Task ata_sem_assinatura_cai_para_a_data_de_publicacao() {
		var filtro = await CapturarFiltro(new ListAtasQuery.Query());

		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: null, dataPublicacaoPncp: hoje.AddDays(-1))).Should().BeTrue();
		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: null, dataPublicacaoPncp: hoje.AddDays(-30))).Should().BeFalse();
	}

	[Fact]
	public async Task ata_sem_nenhuma_data_fica_de_fora() {
		var filtro = await CapturarFiltro(new ListAtasQuery.Query());

		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: null, dataPublicacaoPncp: null)).Should().BeFalse();
	}

	[Fact]
	public async Task canceladas_sao_excluidas_por_padrao() {
		var filtro = await CapturarFiltro(new ListAtasQuery.Query());

		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-1), cancelado: true)).Should().BeFalse();
	}

	[Fact]
	public async Task canceladas_entram_quando_o_filtro_e_desligado() {
		var filtro = await CapturarFiltro(new ListAtasQuery.Query(ApenasNaoCanceladas: false));

		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-1), cancelado: true)).Should().BeTrue();
	}

	[Fact]
	public async Task apenas_com_adesao_exclui_ata_sem_possibilidade() {
		var filtro = await CapturarFiltro(new ListAtasQuery.Query(ApenasComAdesao: true));

		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-1), possibilidadeAdesao: true)).Should().BeTrue();
		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-1), possibilidadeAdesao: false)).Should().BeFalse();
		filtro.IsSatisfiedBy(CriarAta(dataAssinatura: hoje.AddDays(-1), possibilidadeAdesao: null)).Should().BeFalse();
	}

	[Fact]
	public async Task ata_sem_compra_correspondente_aparece_na_listagem() {
		ConfigurarRetorno(CriarAta(dataAssinatura: hoje.AddDays(-1), numeroControlePncpCompra: null));

		var result = await handler.Handle(new ListAtasQuery.Query(), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].NumeroControlePncpCompra.Should().BeNull();
	}

	[Fact]
	public async Task ata_vigente_com_possibilidade_de_adesao_fica_permitida() {
		ConfigurarRetorno(CriarAta(
			dataAssinatura: hoje.AddDays(-1),
			vigenciaFim: hoje.AddDays(60),
			possibilidadeAdesao: true));

		var result = await handler.Handle(new ListAtasQuery.Query(), CancellationToken.None);

		var adesao = result.Value.Items[0].Adesao;
		adesao.Situacao.Should().Be("permitida");
		adesao.Disponivel.Should().BeTrue();
		adesao.DiasRestantes.Should().Be(60);
	}

	[Fact]
	public async Task data_de_referencia_prefere_a_assinatura_sobre_a_publicacao() {
		var assinatura = hoje.AddDays(-1);
		ConfigurarRetorno(CriarAta(dataAssinatura: assinatura, dataPublicacaoPncp: hoje.AddDays(-3)));

		var result = await handler.Handle(new ListAtasQuery.Query(), CancellationToken.None);

		result.Value.Items[0].DataDeReferencia.Should().Be(assinatura);
	}

	[Fact]
	public async Task erro_do_repositorio_vira_falha() {
		atas.PaginarPorDataDeReferencia(
			Arg.Any<Specification<Ata>>(),
			Arg.Any<PaginationParameters>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Failure<PaginationResult<Ata>, RepositoryError>(
			RepositoryError.InvalidFormat("Cursor inválido.")));

		var result = await handler.Handle(new ListAtasQuery.Query(), CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Cursor inválido");
	}

	[Fact]
	public async Task limite_invalido_retorna_falha() {
		var result = await handler.Handle(new ListAtasQuery.Query(Limit: 0), CancellationToken.None);

		result.IsFailure.Should().BeTrue();
	}

	private async Task<Specification<Ata>> CapturarFiltro(ListAtasQuery.Query query) {
		Specification<Ata>? filtro = null;

		atas.PaginarPorDataDeReferencia(
			Arg.Do<Specification<Ata>>(x => filtro = x),
			Arg.Any<PaginationParameters>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<PaginationResult<Ata>, RepositoryError>(
			new PaginationResult<Ata>(Array.Empty<Ata>(), null)));

		await handler.Handle(query, CancellationToken.None);

		filtro.Should().NotBeNull();
		return filtro!;
	}

	private void ConfigurarRetorno(params Ata[] resultado) {
		atas.PaginarPorDataDeReferencia(
			Arg.Any<Specification<Ata>>(),
			Arg.Any<PaginationParameters>(),
			Arg.Any<CancellationToken>()
		).Returns(Result.Success<PaginationResult<Ata>, RepositoryError>(
			new PaginationResult<Ata>(resultado, null)));
	}

	private static Ata CriarAta(
		DateTime? dataAssinatura = null,
		DateTime? dataPublicacaoPncp = null,
		DateTime? vigenciaFim = null,
		Boolean cancelado = false,
		Boolean? possibilidadeAdesao = null,
		String? numeroControlePncpCompra = "00000000000191-1-000001/2026") {
		return new Ata(
			id: 1,
			identificadorDoOrgao: 1,
			numeroControlePncpAta: "00000000000191-1-000001/2026-001",
			anoAta: 2026,
			numeroControlePncpCompra: numeroControlePncpCompra,
			cancelado: cancelado,
			dataAssinatura: dataAssinatura,
			vigenciaFim: vigenciaFim,
			dataPublicacaoPncp: dataPublicacaoPncp,
			possibilidadeAdesao: possibilidadeAdesao);
	}
}
