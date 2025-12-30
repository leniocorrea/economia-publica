using System;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Application.Queries.GetOrgao;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.Orgaos;

public static class GetOrgaoEndpoint {
	public static IEndpointRouteBuilder MapGetOrgao(this IEndpointRouteBuilder app) {
		app.MapGet("/v1/orgaos/{cnpj}", Handle)
			.WithName("GetOrgao")
			.WithTags("Órgãos");

		return app;
	}

	private static async Task<IResult> Handle([FromServices] IMediator mediator, [FromRoute] String cnpj) {
		var result = await mediator.Send(new GetOrgao.Query(cnpj));

		return result.ToOk(Response.From);
	}

	private record Response(
		Int64 Id,
		String Cnpj,
		String RazaoSocial,
		String? NomeFantasia,
		String? CodigoNaturezaJuridica,
		String? DescricaoNaturezaJuridica,
		String? PoderId,
		String? EsferaId,
		String? SituacaoCadastral,
		String? MotivoSituacaoCadastral,
		DateTime? DataSituacaoCadastral,
		DateTime? DataValidacao,
		Boolean Validado,
		DateTime? DataInclusaoPncp,
		DateTime? DataAtualizacaoPncp,
		Boolean StatusAtivo,
		String? JustificativaAtualizacao,
		DateTime CriadoEm,
		DateTime AtualizadoEm,
		Response.UnidadeItem[] Unidades) {
		public static Response From(GetOrgao.Response r) {
			return new Response(
				r.Id,
				r.Cnpj,
				r.RazaoSocial,
				r.NomeFantasia,
				r.CodigoNaturezaJuridica,
				r.DescricaoNaturezaJuridica,
				r.PoderId,
				r.EsferaId,
				r.SituacaoCadastral,
				r.MotivoSituacaoCadastral,
				r.DataSituacaoCadastral,
				r.DataValidacao,
				r.Validado,
				r.DataInclusaoPncp,
				r.DataAtualizacaoPncp,
				r.StatusAtivo,
				r.JustificativaAtualizacao,
				r.CriadoEm,
				r.AtualizadoEm,
				r.Unidades.Select(u => new UnidadeItem(
					u.Id,
					u.CodigoUnidade,
					u.NomeUnidade,
					u.MunicipioNome,
					u.MunicipioCodigoIbge,
					u.UfSigla,
					u.UfNome,
					u.StatusAtivo,
					u.DataInclusaoPncp,
					u.DataAtualizacaoPncp,
					u.CriadoEm,
					u.AtualizadoEm)).ToArray()
			);
		}

		public record UnidadeItem(
			Int64 Id,
			String CodigoUnidade,
			String NomeUnidade,
			String? MunicipioNome,
			String? MunicipioCodigoIbge,
			String? UfSigla,
			String? UfNome,
			Boolean StatusAtivo,
			DateTime? DataInclusaoPncp,
			DateTime? DataAtualizacaoPncp,
			DateTime CriadoEm,
			DateTime AtualizadoEm);
	}
}
