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

namespace EconomIA.Application.Queries.ListOrgaosMonitorados;

public static class ListOrgaosMonitorados {
	public record Query(Boolean? ApenasAtivos, String? Order, String? Cursor, Int32? Limit) : IQuery<Response>;

	public record Response(Response.Item[] Items, Boolean HasMoreItems, String? NextCursor) {
		public record Item(
			Int64 Id,
			Int64 IdentificadorDoOrgao,
			String Cnpj,
			String RazaoSocial,
			Boolean Ativo,
			DateTime CriadoEm,
			DateTime AtualizadoEm);
	}

	public class Handler(IOrgaosMonitoradosReader orgaosMonitorados) : QueryHandler<Query, Response> {
		public override async Task<Result<Response, HandlerResultError>> Handle(Query query, CancellationToken cancellationToken = default) {
			var paginationResult = PaginationParameters.Create(query.Order, query.Cursor, query.Limit);

			if (paginationResult.IsFailure) {
				return Failure(InvalidArgument, paginationResult.Error);
			}

			var pagination = paginationResult.Value;

			var filter = OrgaosMonitoradosSpecifications.All();

			if (query.ApenasAtivos == true) {
				filter = OrgaosMonitoradosSpecifications.Ativos();
			}

			var result = await orgaosMonitorados.Paginate(pagination, filter, cancellationToken);

			if (result.IsFailure) {
				return Failure(result.Error.ToOrgaoMonitoradoError());
			}

			var page = result.Value;
			var items = page.Items.Select(x => new Response.Item(
				x.Id,
				x.IdentificadorDoOrgao,
				x.Orgao?.Cnpj ?? "",
				x.Orgao?.RazaoSocial ?? "",
				x.Ativo,
				x.CriadoEm,
				x.AtualizadoEm
			)).ToArray();

			return Success(new Response(items, page.HasMoreItems, page.NextCursor));
		}
	}
}
