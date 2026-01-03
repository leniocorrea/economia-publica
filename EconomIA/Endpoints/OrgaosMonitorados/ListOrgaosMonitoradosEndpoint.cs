using System;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Application.Queries.ListOrgaosMonitorados;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.OrgaosMonitorados;

public static class ListOrgaosMonitoradosEndpoint {
	public static IEndpointRouteBuilder MapListOrgaosMonitorados(this IEndpointRouteBuilder app) {
		app.MapGet("/v1/orgaos-monitorados", Handle)
			.WithName("ListOrgaosMonitorados")
			.WithTags("Órgãos Monitorados");

		return app;
	}

	private static async Task<IResult> Handle(
		[FromServices] IMediator mediator,
		[FromQuery] Boolean? apenasAtivos,
		[FromQuery] String? order,
		[FromQuery] String? cursor,
		[FromQuery] Int32? limit) {
		var query = new ListOrgaosMonitorados.Query(apenasAtivos, order, cursor, limit);
		var result = await mediator.Send(query);

		return result.ToOk(Response.From);
	}

	private record Response(
		Response.Item[] Items,
		Boolean HasMoreItems,
		String? NextCursor) {
		public static Response From(ListOrgaosMonitorados.Response r) {
			return new Response(
				r.Items.Select(x => new Item(
					x.Id,
					x.IdentificadorDoOrgao,
					x.Cnpj,
					x.RazaoSocial,
					x.Ativo,
					x.CriadoEm,
					x.AtualizadoEm)).ToArray(),
				r.HasMoreItems,
				r.NextCursor);
		}

		public record Item(
			Int64 Id,
			Int64 IdentificadorDoOrgao,
			String Cnpj,
			String RazaoSocial,
			Boolean Ativo,
			DateTime CriadoEm,
			DateTime AtualizadoEm);
	}
}
