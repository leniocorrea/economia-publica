using System;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Application.Queries.ListOrgaos;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Orgaos;

public static class ListOrgaosEndpoint {
	public static IEndpointRouteBuilder MapListOrgaos(this IEndpointRouteBuilder app) {
		app.MapGet("/v1/orgaos", Handle)
			.WithName("ListOrgaos")
			.WithTags("Órgãos");

		return app;
	}

	private static async Task<IResult> Handle(
		[FromServices] IMediator mediator,
		[FromQuery] String? cnpj,
		[FromQuery] String? order,
		[FromQuery] String? cursor,
		[FromQuery] Int32? limit) {
		var result = await mediator.Send(new ListOrgaos.Query(cnpj, order, cursor, limit));

		return result.ToOk(Response.From);
	}

	private record Response(Response.Item[] Items, Boolean HasMoreItems, String? NextCursor) {
		public static Response From(ListOrgaos.Response response) {
			var items = response.Items.Select(x => new Item(
				x.Id,
				x.Cnpj,
				x.RazaoSocial,
				x.NomeFantasia,
				x.CodigoNaturezaJuridica,
				x.DescricaoNaturezaJuridica,
				x.PoderId,
				x.EsferaId,
				x.SituacaoCadastral,
				x.StatusAtivo,
				x.Validado,
				x.Unidades.Select(u => new UnidadeItem(
					u.Id,
					u.CodigoUnidade,
					u.NomeUnidade,
					u.MunicipioNome,
					u.MunicipioCodigoIbge,
					u.UfSigla,
					u.UfNome,
					u.StatusAtivo)).ToArray()
			)).ToArray();

			return new Response(items, response.HasMoreItems, response.NextCursor);
		}

		public record Item(
			Int64 Id,
			String Cnpj,
			String RazaoSocial,
			String? NomeFantasia,
			String? CodigoNaturezaJuridica,
			String? DescricaoNaturezaJuridica,
			String? PoderId,
			String? EsferaId,
			String? SituacaoCadastral,
			Boolean StatusAtivo,
			Boolean Validado,
			UnidadeItem[] Unidades);

		public record UnidadeItem(
			Int64 Id,
			String CodigoUnidade,
			String NomeUnidade,
			String? MunicipioNome,
			String? MunicipioCodigoIbge,
			String? UfSigla,
			String? UfNome,
			Boolean StatusAtivo);
	}
}
