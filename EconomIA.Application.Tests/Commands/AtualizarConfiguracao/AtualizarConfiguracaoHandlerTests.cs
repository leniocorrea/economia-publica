using System;
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

using AtualizarConfiguracaoCommand = EconomIA.Application.Commands.AtualizarConfiguracao.AtualizarConfiguracao;

namespace EconomIA.Application.Tests.Commands.AtualizarConfiguracao;

public class AtualizarConfiguracaoHandlerTests {
	private readonly IConfiguracoesCarga configuracoesCarga;
	private readonly AtualizarConfiguracaoCommand.Handler handler;

	public AtualizarConfiguracaoHandlerTests() {
		configuracoesCarga = Substitute.For<IConfiguracoesCarga>();
		handler = new AtualizarConfiguracaoCommand.Handler(configuracoesCarga);
	}

	[Fact]
	public async Task deve_atualizar_configuracao_quando_valida() {
		var config = CriarConfiguracaoPadrao();
		configuracoesCarga.ObterOuCriarPadrao(Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));
		configuracoesCarga.Update(Arg.Any<ConfiguracaoCarga>(), Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));

		var command = CriarCommandValido();
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		await configuracoesCarga.Received(1).Update(Arg.Any<ConfiguracaoCarga>(), Arg.Any<CancellationToken>());
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("25:00")]
	[InlineData("")]
	public async Task deve_falhar_quando_horario_execucao_invalido(String horarioInvalido) {
		var command = CriarCommandValido() with { HorarioExecucao = horarioInvalido };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Horário de execução");
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("25:00")]
	[InlineData("")]
	public async Task deve_falhar_quando_horario_sincronizacao_invalido(String horarioInvalido) {
		var command = CriarCommandValido() with { HorarioSincronizacao = horarioInvalido };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Horário de sincronização");
	}

	[Fact]
	public async Task deve_falhar_quando_dias_semana_vazio() {
		var command = CriarCommandValido() with { DiasSemana = [] };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("dia da semana");
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(7)]
	[InlineData(10)]
	public async Task deve_falhar_quando_dias_semana_tem_valor_invalido(Int32 diaInvalido) {
		var command = CriarCommandValido() with { DiasSemana = new[] { 1, diaInvalido, 3 } };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Dias da semana devem estar entre 0");
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(7)]
	public async Task deve_falhar_quando_dia_semana_sincronizacao_invalido(Int32 diaInvalido) {
		var command = CriarCommandValido() with { DiaSemanasSincronizacao = diaInvalido };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Dia de sincronização");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(366)]
	public async Task deve_falhar_quando_dias_retroativos_fora_do_intervalo(Int32 diasInvalido) {
		var command = CriarCommandValido() with { DiasRetroativos = diasInvalido };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Dias retroativos");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(366)]
	public async Task deve_falhar_quando_dias_carga_inicial_fora_do_intervalo(Int32 diasInvalido) {
		var command = CriarCommandValido() with { DiasCargaInicial = diasInvalido };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Dias de carga inicial");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(21)]
	public async Task deve_falhar_quando_max_concorrencia_fora_do_intervalo(Int32 concorrenciaInvalida) {
		var command = CriarCommandValido() with { MaxConcorrencia = concorrenciaInvalida };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.ResultError.ToProblemString().Should().Contain("Concorrência máxima");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(6)]
	public async Task deve_aceitar_dia_semana_sincronizacao_valido(Int32 diaValido) {
		var config = CriarConfiguracaoPadrao();
		configuracoesCarga.ObterOuCriarPadrao(Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));
		configuracoesCarga.Update(Arg.Any<ConfiguracaoCarga>(), Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));

		var command = CriarCommandValido() with { DiaSemanasSincronizacao = diaValido };
		var result = await handler.Handle(command, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task deve_aceitar_todas_combinacoes_booleanas() {
		var config = CriarConfiguracaoPadrao();
		configuracoesCarga.ObterOuCriarPadrao(Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));
		configuracoesCarga.Update(Arg.Any<ConfiguracaoCarga>(), Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));

		var command = CriarCommandValido() with {
			Habilitado = false,
			CarregarCompras = false,
			CarregarContratos = false,
			CarregarAtas = false,
			SincronizarOrgaos = false
		};

		var result = await handler.Handle(command, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
	}

	private static AtualizarConfiguracaoCommand.Command CriarCommandValido() {
		return new AtualizarConfiguracaoCommand.Command(
			HorarioExecucao: "03:30",
			DiasSemana: [1, 2, 3, 4, 5],
			Habilitado: true,
			DiasRetroativos: 2,
			DiasCargaInicial: 60,
			MaxConcorrencia: 8,
			CarregarCompras: true,
			CarregarContratos: true,
			CarregarAtas: true,
			SincronizarOrgaos: true,
			HorarioSincronizacao: "01:00",
			DiaSemanasSincronizacao: 0
		);
	}

	private static ConfiguracaoCarga CriarConfiguracaoPadrao() {
		return new ConfiguracaoCarga(
			id: 1,
			horarioExecucao: new TimeOnly(2, 0),
			diasSemana: [0, 1, 2, 3, 4, 5, 6],
			habilitado: true,
			diasRetroativos: 1,
			diasCargaInicial: 90,
			maxConcorrencia: 4,
			carregarCompras: true,
			carregarContratos: true,
			carregarAtas: true,
			sincronizarOrgaos: true,
			horarioSincronizacao: new TimeOnly(0, 0),
			diaSemanasSincronizacao: 0,
			atualizadoEm: DateTime.UtcNow,
			atualizadoPor: null
		);
	}
}
