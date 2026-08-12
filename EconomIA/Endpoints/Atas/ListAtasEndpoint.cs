using System;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Application.Queries.ListAtas;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Atas;

public static class ListAtasEndpoint {
	public static IEndpointRouteBuilder MapListAtas(this IEndpointRouteBuilder app) {
		app.MapGet("/v1/atas", Handle)
			.WithName("ListAtas")
			.WithTags("Atas");

		return app;
	}

	private static async Task<IResult> Handle(
		[FromServices] IMediator mediator,
		[FromQuery] DateTime? dataInicio,
		[FromQuery] DateTime? dataFim,
		[FromQuery] DateTime? vigentesEm,
		[FromQuery] Boolean? apenasNaoCanceladas,
		[FromQuery] Boolean? apenasComAdesao,
		[FromQuery] String? cnpjOrgao,
		[FromQuery] String? cursor,
		[FromQuery] Int32? limit) {
		var result = await mediator.Send(new ListAtas.Query(
			dataInicio,
			dataFim,
			vigentesEm,
			apenasNaoCanceladas,
			apenasComAdesao,
			cnpjOrgao,
			cursor,
			limit));

		return result.ToOk(Response.From);
	}

	private record Response(
		Item[] Items,
		Boolean HasMoreItems,
		String? NextCursor,
		Periodo Periodo) {

		public static Response From(ListAtas.Response response) {
			var items = response.Items.Select(x => new Item(
				x.NumeroControlePncpAta,
				x.NumeroAtaRegistroPreco,
				x.AnoAta,
				x.ObjetoContratacao,
				x.Cancelado,
				x.DataAssinatura,
				x.DataPublicacaoPncp,
				x.DataDeReferencia,
				x.VigenciaInicio,
				x.VigenciaFim,
				x.PossibilidadeAdesao,
				x.NumeroControlePncpCompra,
				x.Orgao is not null
					? new OrgaoEntidade(
						x.Orgao.Cnpj,
						x.Orgao.RazaoSocial,
						x.Orgao.NomeFantasia,
						x.Orgao.PoderId,
						x.Orgao.EsferaId)
					: null,
				new Adesao(
					x.Adesao.Situacao,
					x.Adesao.Disponivel,
					x.Adesao.VigenciaFim,
					x.Adesao.DiasRestantes,
					x.Adesao.NumeroControlePncpAta,
					x.Adesao.SaldoDisponivel)
			)).ToArray();

			return new Response(
				items,
				response.HasMoreItems,
				response.NextCursor,
				new Periodo(response.PeriodoInicio, response.PeriodoFim));
		}
	}

	private record Periodo(
		DateTime Inicio,
		DateTime Fim);

	private record Item(
		String NumeroControlePncpAta,
		String? NumeroAtaRegistroPreco,
		Int32 AnoAta,
		String? ObjetoContratacao,
		Boolean Cancelada,
		DateTime? DataAssinatura,
		DateTime? DataPublicacaoPncp,
		DateTime? DataDeReferencia,
		DateTime? VigenciaInicio,
		DateTime? VigenciaFim,
		Boolean? PossibilidadeAdesao,
		String? NumeroControlePncpCompra,
		OrgaoEntidade? OrgaoEntidade,
		Adesao Adesao);

	private record OrgaoEntidade(
		String Cnpj,
		String RazaoSocial,
		String? NomeFantasia,
		String? PoderId,
		String? EsferaId);

	private record Adesao(
		String Situacao,
		Boolean Disponivel,
		DateTime? VigenciaFim,
		Int32? DiasRestantes,
		String? NumeroControlePncpAta,
		Decimal? SaldoDisponivel);
}
