using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Application.Extensions;
using EconomIA.Common.Results;
using EconomIA.Domain.Repositories;

namespace EconomIA.Application.Queries.GetOrgao;

public static class GetOrgao {
	public record Query(String Cnpj) : IQuery<Response>;

	public record Response(
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

	public class Handler(IOrgaosReader orgaos) : QueryHandler<Query, Response> {
		public override async Task<Result<Response, HandlerResultError>> Handle(Query query, CancellationToken cancellationToken = default) {
			var orgaoResult = await orgaos.Find(OrgaosSpecifications.WithCnpj(query.Cnpj), cancellationToken);

			if (orgaoResult.IsFailure) {
				return Failure(orgaoResult.Error.ToOrgaoError());
			}

			var orgao = orgaoResult.Value;
			var response = new Response(
				orgao.Id,
				orgao.Cnpj,
				orgao.RazaoSocial,
				orgao.NomeFantasia,
				orgao.CodigoNaturezaJuridica,
				orgao.DescricaoNaturezaJuridica,
				orgao.PoderId,
				orgao.EsferaId,
				orgao.SituacaoCadastral,
				orgao.MotivoSituacaoCadastral,
				orgao.DataSituacaoCadastral,
				orgao.DataValidacao,
				orgao.Validado,
				orgao.DataInclusaoPncp,
				orgao.DataAtualizacaoPncp,
				orgao.StatusAtivo,
				orgao.JustificativaAtualizacao,
				orgao.CriadoEm,
				orgao.AtualizadoEm,
				orgao.Unidades.Select(u => new Response.UnidadeItem(
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

			return Success(response);
		}
	}
}
