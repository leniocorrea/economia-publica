using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Common.Persistence;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

using GetConfiguracaoQuery = EconomIA.Application.Queries.GetConfiguracao.GetConfiguracao;

namespace EconomIA.Application.Tests.Queries.GetConfiguracao;

public class GetConfiguracaoHandlerTests {
	private readonly IConfiguracoesCarga configuracoesCarga;
	private readonly GetConfiguracaoQuery.Handler handler;

	public GetConfiguracaoHandlerTests() {
		configuracoesCarga = Substitute.For<IConfiguracoesCarga>();
		handler = new GetConfiguracaoQuery.Handler(configuracoesCarga);
	}

	[Fact]
	public async Task deve_retornar_configuracao_quando_existir() {
		var config = CriarConfiguracaoPadrao();
		configuracoesCarga.ObterOuCriarPadrao(Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));

		var result = await handler.Handle(new GetConfiguracaoQuery.Query(), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.HorarioExecucao.Should().Be("02:00");
		result.Value.DiasSemana.Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6 });
		result.Value.Habilitado.Should().BeTrue();
		result.Value.DiasRetroativos.Should().Be(1);
		result.Value.DiasCargaInicial.Should().Be(90);
		result.Value.MaxConcorrencia.Should().Be(4);
		result.Value.CarregarCompras.Should().BeTrue();
		result.Value.CarregarContratos.Should().BeTrue();
		result.Value.CarregarAtas.Should().BeTrue();
	}

	[Fact]
	public async Task deve_formatar_horario_corretamente() {
		var config = new ConfiguracaoCarga(
			id: 1,
			horarioExecucao: new TimeOnly(14, 30),
			diasSemana: [1, 2, 3],
			habilitado: true,
			diasRetroativos: 1,
			diasCargaInicial: 90,
			maxConcorrencia: 4,
			carregarCompras: true,
			carregarContratos: true,
			carregarAtas: true,
			sincronizarOrgaos: true,
			horarioSincronizacao: new TimeOnly(23, 45),
			diaSemanasSincronizacao: 0,
			atualizadoEm: DateTime.UtcNow,
			atualizadoPor: null
		);

		configuracoesCarga.ObterOuCriarPadrao(Arg.Any<CancellationToken>())
			.Returns(Result.Success<ConfiguracaoCarga, RepositoryError>(config));

		var result = await handler.Handle(new GetConfiguracaoQuery.Query(), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.HorarioExecucao.Should().Be("14:30");
		result.Value.HorarioSincronizacao.Should().Be("23:45");
	}

	[Fact]
	public async Task deve_retornar_falha_quando_repository_falhar() {
		configuracoesCarga.ObterOuCriarPadrao(Arg.Any<CancellationToken>())
			.Returns(Result.Failure<ConfiguracaoCarga, RepositoryError>(
				new RepositoryError(RepositoryErrorCode.NotFound, "Configuração não encontrada")));

		var result = await handler.Handle(new GetConfiguracaoQuery.Query(), CancellationToken.None);

		result.IsFailure.Should().BeTrue();
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
