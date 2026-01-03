using System;
using System.Threading.Tasks;
using EconomIA.Application.Commands.DesmonitorarOrgao;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.OrgaosMonitorados;

public static class DesmonitorarOrgaoEndpoint {
	public static IEndpointRouteBuilder MapDesmonitorarOrgao(this IEndpointRouteBuilder app) {
		app.MapDelete("/v1/orgaos-monitorados/{cnpj}", Handle)
			.WithName("DesmonitorarOrgao")
			.WithTags("Órgãos Monitorados");

		return app;
	}

	private static async Task<IResult> Handle([FromServices] IMediator mediator, [FromRoute] String cnpj) {
		var command = new DesmonitorarOrgao.Command(cnpj);
		var result = await mediator.Send(command);

		return result.ToNoContent();
	}
}
