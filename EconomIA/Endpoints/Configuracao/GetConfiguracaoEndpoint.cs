using System.Threading.Tasks;
using EconomIA.Application.Queries.GetConfiguracao;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Configuracao;

public static class GetConfiguracaoEndpoint {
	public static IEndpointRouteBuilder MapGetConfiguracao(this IEndpointRouteBuilder app) {
		app.MapGet("/v1/configuracao", Handle)
			.WithName("GetConfiguracao")
			.WithTags("Configuração");

		return app;
	}

	private static async Task<IResult> Handle([FromServices] IMediator mediator) {
		var query = new GetConfiguracao.Query();
		var result = await mediator.Send(query);

		return result.ToOk(response => response);
	}
}
