using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Application.Extensions;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Common.Results;
using EconomIA.Domain.Repositories;
using static EconomIA.Domain.Results.EconomIAErrorCodes;

namespace EconomIA.Application.Queries.ListOrgaos;

public static class ListOrgaos {
	public record Query(String? Cnpj, String? Order, String? Cursor, Int32? Limit) : IQuery<Response>;

	public record Response(Response.Item[] Items, Boolean HasMoreItems, String? NextCursor) {
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
			Response.UnidadeItem[] Unidades);

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

	public class Handler(IOrgaosReader orgaos) : QueryHandler<Query, Response> {
		public override async Task<Result<Response, HandlerResultError>> Handle(Query query, CancellationToken cancellationToken = default) {
			var paginationResult = PaginationParameters.Create(query.Order, query.Cursor, query.Limit);

			if (paginationResult.IsFailure) {
				return Failure(InvalidArgument, paginationResult.Error);
			}

			var pagination = paginationResult.Value;

			var filter = OrgaosSpecifications.All();

			if (!String.IsNullOrWhiteSpace(query.Cnpj)) {
				filter = OrgaosSpecifications.WithCnpj(query.Cnpj);
			}

			var result = await orgaos.Paginate(pagination, filter, cancellationToken);

			if (result.IsFailure) {
				return Failure(result.Error.ToOrgaoError());
			}

			var page = result.Value;
			var items = page.Items.Select(x => new Response.Item(
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
				x.Unidades.Select(u => new Response.UnidadeItem(
					u.Id,
					u.CodigoUnidade,
					u.NomeUnidade,
					u.MunicipioNome,
					u.MunicipioCodigoIbge,
					u.UfSigla,
					u.UfNome,
					u.StatusAtivo)).ToArray()
			)).ToArray();

			return Success(new Response(items, page.HasMoreItems, page.NextCursor));
		}
	}
}
