using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using EconomIA.Application.Adesao;
using EconomIA.Application.Extensions;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Common.Results;
using EconomIA.Domain.Repositories;
using static EconomIA.Domain.Results.EconomIAErrorCodes;

namespace EconomIA.Application.Queries.ListAtas;

public static class ListAtas {
	private const Int32 DiasDoPeriodoPadrao = 7;

	public record Query(
		DateTime? DataInicio = null,
		DateTime? DataFim = null,
		DateTime? VigentesEm = null,
		Boolean? ApenasNaoCanceladas = null,
		Boolean? ApenasComAdesao = null,
		String? CnpjOrgao = null,
		String? Cursor = null,
		Int32? Limit = null) : IQuery<Response>;

	public record Response(
		Response.Item[] Items,
		Boolean HasMoreItems,
		String? NextCursor,
		DateTime PeriodoInicio,
		DateTime PeriodoFim) {

		public record Item(
			Int64 Id,
			String NumeroControlePncpAta,
			String? NumeroAtaRegistroPreco,
			Int32 AnoAta,
			String? ObjetoContratacao,
			Boolean Cancelado,
			DateTime? DataAssinatura,
			DateTime? DataPublicacaoPncp,
			DateTime? DataDeReferencia,
			DateTime? VigenciaInicio,
			DateTime? VigenciaFim,
			Boolean? PossibilidadeAdesao,
			String? NumeroControlePncpCompra,
			OrgaoItem? Orgao,
			SituacaoDaAdesao Adesao);

		public record OrgaoItem(
			Int64 Id,
			String Cnpj,
			String RazaoSocial,
			String? NomeFantasia,
			String? PoderId,
			String? EsferaId);
	}

	public class Handler(IAtasReader atas) : QueryHandler<Query, Response> {
		private static DateTime ComoInicioDoDiaEmUtc(DateTime valor) {
			return DateTime.SpecifyKind(valor.Date, DateTimeKind.Utc);
		}

		public override async Task<Result<Response, HandlerResultError>> Handle(Query query, CancellationToken cancellationToken = default) {
			var hoje = DateTime.UtcNow.AddHours(-3).Date;
			var fim = ComoInicioDoDiaEmUtc(query.DataFim ?? hoje);
			var inicio = ComoInicioDoDiaEmUtc(query.DataInicio ?? fim.AddDays(-DiasDoPeriodoPadrao));

			if (inicio > fim) {
				return Failure(InvalidArgument, "Data inicial não pode ser maior que a data final.");
			}

			var fimInclusivo = fim.AddDays(1).AddTicks(-1);

			var paginationResult = PaginationParameters.Create(null, query.Cursor, query.Limit);

			if (paginationResult.IsFailure) {
				return Failure(InvalidArgument, paginationResult.Error);
			}

			var pagination = paginationResult.Value;

			var filtro = AtasSpecifications.ComDataDeReferenciaEntre(inicio, fimInclusivo);

			if (query.ApenasNaoCanceladas ?? true) {
				filtro = filtro + AtasSpecifications.NaoCanceladas();
			}

			if (query.VigentesEm.HasValue) {
				filtro = filtro + AtasSpecifications.VigentesEm(ComoInicioDoDiaEmUtc(query.VigentesEm.Value));
			}

			if (query.ApenasComAdesao == true) {
				filtro = filtro + AtasSpecifications.ComPossibilidadeDeAdesao();
			}

			if (!String.IsNullOrWhiteSpace(query.CnpjOrgao)) {
				filtro = filtro + AtasSpecifications.DoOrgaoComCnpj(query.CnpjOrgao.Trim());
			}

			var resultado = await atas.PaginarPorDataDeReferencia(filtro, pagination, cancellationToken);

			if (resultado.IsFailure) {
				return Failure(resultado.Error.ToAtaError());
			}

			var pagina = resultado.Value;
			var dataDeReferenciaDaAdesao = DateOnly.FromDateTime(hoje);

			var items = pagina.Items
				.Select(x => {
					var orgao = x.Orgao is not null
						? new Response.OrgaoItem(
							x.Orgao.Id,
							x.Orgao.Cnpj,
							x.Orgao.RazaoSocial,
							x.Orgao.NomeFantasia,
							x.Orgao.PoderId,
							x.Orgao.EsferaId)
						: null;

					var adesao = AvaliadorDeAdesao.Avaliar(
						new[] {
							new AtaParaAvaliacao(x.NumeroControlePncpAta, x.Cancelado, x.VigenciaFim, x.PossibilidadeAdesao)
						},
						true,
						dataDeReferenciaDaAdesao);

					return new Response.Item(
						x.Id,
						x.NumeroControlePncpAta,
						x.NumeroAtaRegistroPreco,
						x.AnoAta,
						x.ObjetoContratacao,
						x.Cancelado,
						x.DataAssinatura,
						x.DataPublicacaoPncp,
						x.DataAssinatura ?? x.DataPublicacaoPncp,
						x.VigenciaInicio,
						x.VigenciaFim,
						x.PossibilidadeAdesao,
						x.NumeroControlePncpCompra,
						orgao,
						adesao);
				})
				.ToArray();

			return Success(new Response(items, pagina.HasMoreItems, pagina.NextCursor, inicio, fimInclusivo));
		}
	}
}
