using System;
using System.Threading.Tasks;
using EconomIA.Application.Commands.AtualizarConfiguracao;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Configuracao;

public static class AtualizarConfiguracaoEndpoint {
	public static IEndpointRouteBuilder MapAtualizarConfiguracao(this IEndpointRouteBuilder app) {
		app.MapPut("/v1/configuracao", Handle)
			.WithName("AtualizarConfiguracao")
			.WithTags("Configuração");

		return app;
	}

	private record Request(
		String HorarioExecucao,
		Int32[] DiasSemana,
		Boolean Habilitado,
		Int32 DiasRetroativos,
		Int32 DiasCargaInicial,
		Int32 MaxConcorrencia,
		Boolean CarregarCompras,
		Boolean CarregarContratos,
		Boolean CarregarAtas,
		Boolean SincronizarOrgaos,
		String HorarioSincronizacao,
		Int32 DiaSemanasSincronizacao);

	private static async Task<IResult> Handle([FromServices] IMediator mediator, [FromBody] Request request) {
		var command = new AtualizarConfiguracao.Command(
			HorarioExecucao: request.HorarioExecucao,
			DiasSemana: request.DiasSemana,
			Habilitado: request.Habilitado,
			DiasRetroativos: request.DiasRetroativos,
			DiasCargaInicial: request.DiasCargaInicial,
			MaxConcorrencia: request.MaxConcorrencia,
			CarregarCompras: request.CarregarCompras,
			CarregarContratos: request.CarregarContratos,
			CarregarAtas: request.CarregarAtas,
			SincronizarOrgaos: request.SincronizarOrgaos,
			HorarioSincronizacao: request.HorarioSincronizacao,
			DiaSemanasSincronizacao: request.DiaSemanasSincronizacao
		);

		var result = await mediator.Send(command);

		return result.ToNoContent();
	}
}
