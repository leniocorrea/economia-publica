using System;
using System.Threading.Tasks;
using EconomIA.Application.Commands.MonitorarOrgao;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.OrgaosMonitorados;

public static class MonitorarOrgaoEndpoint {
	public static IEndpointRouteBuilder MapMonitorarOrgao(this IEndpointRouteBuilder app) {
		app.MapPost("/v1/orgaos-monitorados/{cnpj}", Handle)
			.WithName("MonitorarOrgao")
			.WithTags("Órgãos Monitorados");

		return app;
	}

	private static async Task<IResult> Handle([FromServices] IMediator mediator, [FromRoute] String cnpj) {
		var command = new MonitorarOrgao.Command(cnpj);
		var result = await mediator.Send(command);

		return result.ToCreated($"/v1/orgaos-monitorados/{cnpj}");
	}
}
