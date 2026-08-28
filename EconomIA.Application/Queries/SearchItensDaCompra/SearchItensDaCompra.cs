using System;
using System.Collections.Immutable;
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

namespace EconomIA.Application.Queries.SearchItensDaCompra;

public static class SearchItensDaCompra {
	public record Query(
		String? Descricao,
		String? Order,
		String? Cursor,
		Int32? Limit,
		DateTime? DataInclusaoInicio = null,
		DateTime? DataInclusaoFim = null,
		String? RazaoSocial = null,
		String? UfSigla = null,
		Decimal? ValorUnitarioHomologadoMinimo = null,
		Decimal? ValorUnitarioHomologadoMaximo = null,
		Decimal? ValorTotalHomologadoMinimo = null,
		Decimal? ValorTotalHomologadoMaximo = null,
		Boolean? ApenasComAdesao = null,
		Boolean? ApenasComAtaVigente = null,
		DateTime? DataDaAtaInicio = null,
		DateTime? DataDaAtaFim = null,
		String? ObjetoDaCompra = null) : IQuery<Response>;

	public record Response(Response.Item[] Items, Int64 TotalHits, Boolean HasMoreItems, String? NextCursor) {
		public record Item(
			Int64 Id,
			Int64 IdentificadorDaCompra,
			Int32 NumeroItem,
			String? Descricao,
			Decimal? Quantidade,
			String? UnidadeMedida,
			Decimal? ValorUnitarioEstimado,
			Decimal? ValorTotal,
			String? CriterioJulgamentoNome,
			String? SituacaoCompraItemNome,
			Boolean TemResultado,
			DateTime? DataAtualizacao,
			DateTime CriadoEm,
			DateTime AtualizadoEm,
			CompraItem? Compra,
			OrgaoItem? Orgao,
			ResultadoItem[] Resultados,
			AtaItem[] Atas,
			ContratoItem[] Contratos,
			SituacaoDaAdesao Adesao);

		public record CompraItem(
			Int64 Id,
			String NumeroControlePncp,
			Int32 AnoCompra,
			Int32 SequencialCompra,
			String? ModalidadeNome,
			String? ObjetoCompra,
			Decimal? ValorTotalEstimado,
			Decimal? ValorTotalHomologado,
			String? SituacaoCompraNome,
			DateTime? DataInclusao,
			DateTime? DataAberturaProposta,
			DateTime? DataEncerramentoProposta,
			String? AmparoLegalNome,
			String? ModoDisputaNome,
			String? LinkPncp);

		public record OrgaoItem(
			Int64 Id,
			String Cnpj,
			String RazaoSocial,
			String? NomeFantasia,
			String? PoderId,
			String? EsferaId,
			UnidadeItem[] Unidades);

		public record UnidadeItem(
			Int64 Id,
			String CodigoUnidade,
			String NomeUnidade,
			String? MunicipioNome,
			String? MunicipioCodigoIbge,
			String? UfSigla,
			String? UfNome);

		public record AtaItem(
			Int64 Id,
			String NumeroControlePncpAta,
			String? NumeroAtaRegistroPreco,
			Int32 AnoAta,
			String? ObjetoContratacao,
			Boolean Cancelado,
			DateTime? DataAssinatura,
			DateTime? VigenciaInicio,
			DateTime? VigenciaFim,
			Boolean? PossibilidadeAdesao);

		public record ContratoItem(
			Int64 Id,
			String NumeroControlePncp,
			Int32 AnoContrato,
			Int32 SequencialContrato,
			String? NumeroContratoEmpenho,
			String? ObjetoContrato,
			String? TipoContratoNome,
			String? NiFornecedor,
			String? NomeRazaoSocialFornecedor,
			Decimal? ValorInicial,
			Decimal? ValorGlobal,
			DateTime? DataAssinatura,
			DateTime? DataVigenciaInicio,
			DateTime? DataVigenciaFim);

		public record ResultadoItem(
			Int64 Id,
			String? NiFornecedor,
			String? NomeRazaoSocialFornecedor,
			Decimal? QuantidadeHomologada,
			Decimal? ValorUnitarioHomologado,
			Decimal? ValorTotalHomologado,
			String? SituacaoCompraItemResultadoNome,
			DateTime? DataResultado);
	}

	public class Handler(
		IItensDaCompraSearcher searcher,
		IItensDaCompraReader reader,
		IAtasReader atasReader,
		IContratosReader contratosReader) : QueryHandler<Query, Response> {

		public override async Task<Result<Response, HandlerResultError>> Handle(Query query, CancellationToken cancellationToken = default) {
			var paginationResult = PaginationParameters.Create(query.Order, query.Cursor, query.Limit);

			if (paginationResult.IsFailure) {
				return Failure(InvalidArgument, paginationResult.Error);
			}

			var pagination = paginationResult.Value;

			if (query.DataDaAtaInicio.HasValue && query.DataDaAtaFim.HasValue && query.DataDaAtaInicio.Value.Date > query.DataDaAtaFim.Value.Date) {
				return Failure(InvalidArgument, "Data inicial da ata não pode ser maior que a data final.");
			}

			var filters = new SearchFilters(
				query.DataInclusaoInicio,
				query.DataInclusaoFim,
				query.RazaoSocial,
				query.UfSigla,
				query.ValorUnitarioHomologadoMinimo,
				query.ValorUnitarioHomologadoMaximo,
				query.ValorTotalHomologadoMinimo,
				query.ValorTotalHomologadoMaximo,
				query.ApenasComAdesao,
				query.ApenasComAtaVigente,
				query.DataDaAtaInicio,
				query.DataDaAtaFim,
				query.ObjetoDaCompra);

			var searchResult = await searcher.Search(query.Descricao, filters, pagination, cancellationToken);

			if (searchResult.IsFailure) {
				return Failure(searchResult.Error.ToItemError());
			}

			var search = searchResult.Value;

			if (search.Ids.Length == 0) {
				return Success(new Response(Array.Empty<Response.Item>(), 0, false, null));
			}

			var filter = ItensDaCompraSpecifications.WithIds(search.Ids);
			var itemsResult = await reader.FilterWithCompraAndOrgao(filter, cancellationToken);

			if (itemsResult.IsFailure) {
				return Failure(itemsResult.Error.ToItemError());
			}

			var itensDoElastic = itemsResult.Value;

			var posicaoNaBusca = search.Ids
				.Select((identificador, posicao) => new { identificador, posicao })
				.ToDictionary(x => x.identificador, x => x.posicao);

			var itensFiltrados = itensDoElastic
				.OrderBy(x => posicaoNaBusca.TryGetValue(x.Id, out var posicao) ? posicao : Int32.MaxValue)
				.AsEnumerable();

			if (query.ValorUnitarioHomologadoMinimo.HasValue) {
				itensFiltrados = itensFiltrados.Where(x =>
					x.Resultados.Any(r => r.ValorUnitarioHomologado >= query.ValorUnitarioHomologadoMinimo.Value));
			}

			if (query.ValorUnitarioHomologadoMaximo.HasValue) {
				itensFiltrados = itensFiltrados.Where(x =>
					x.Resultados.Any(r => r.ValorUnitarioHomologado <= query.ValorUnitarioHomologadoMaximo.Value));
			}

			if (query.ValorTotalHomologadoMinimo.HasValue) {
				itensFiltrados = itensFiltrados.Where(x =>
					x.Resultados.Any(r => r.ValorTotalHomologado >= query.ValorTotalHomologadoMinimo.Value));
			}

			if (query.ValorTotalHomologadoMaximo.HasValue) {
				itensFiltrados = itensFiltrados.Where(x =>
					x.Resultados.Any(r => r.ValorTotalHomologado <= query.ValorTotalHomologadoMaximo.Value));
			}

			var itensParaProcessar = itensFiltrados.ToImmutableArray();

			var numerosControlePncp = itensParaProcessar
				.Where(x => x.Compra is not null)
				.Select(x => x.Compra!.NumeroControlePncp)
				.Distinct()
				.ToImmutableArray();

			var atas = Array.Empty<Domain.Ata>();
			var contratos = Array.Empty<Domain.Contrato>();

			if (numerosControlePncp.Length > 0) {
				var atasFilter = AtasSpecifications.WithNumerosControlePncpCompra(numerosControlePncp);
				var atasResult = await atasReader.Filter(atasFilter, cancellationToken);

				if (atasResult.IsSuccess) {
					atas = atasResult.Value.ToArray();
				}

				var contratosFilter = ContratosSpecifications.WithNumerosControlePncpCompra(numerosControlePncp);
				var contratosResult = await contratosReader.Filter(contratosFilter, cancellationToken);

				if (contratosResult.IsSuccess) {
					contratos = contratosResult.Value.ToArray();
				}
			}

			var hoje = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

			var items = itensParaProcessar
				.Select(x => {
					var compraItem = x.Compra is not null
						? new Response.CompraItem(
							x.Compra.Id,
							x.Compra.NumeroControlePncp,
							x.Compra.AnoCompra,
							x.Compra.SequencialCompra,
							x.Compra.ModalidadeNome,
							x.Compra.ObjetoCompra,
							x.Compra.ValorTotalEstimado,
							x.Compra.ValorTotalHomologado,
							x.Compra.SituacaoCompraNome,
							x.Compra.DataInclusao,
							x.Compra.DataAberturaProposta,
							x.Compra.DataEncerramentoProposta,
							x.Compra.AmparoLegalNome,
							x.Compra.ModoDisputaNome,
							x.Compra.LinkPncp)
						: null;

					var orgaoItem = x.Compra?.Orgao is not null
						? new Response.OrgaoItem(
							x.Compra.Orgao.Id,
							x.Compra.Orgao.Cnpj,
							x.Compra.Orgao.RazaoSocial,
							x.Compra.Orgao.NomeFantasia,
							x.Compra.Orgao.PoderId,
							x.Compra.Orgao.EsferaId,
							x.Compra.Orgao.Unidades
								.Select(u => new Response.UnidadeItem(
									u.Id,
									u.CodigoUnidade,
									u.NomeUnidade,
									u.MunicipioNome,
									u.MunicipioCodigoIbge,
									u.UfSigla,
									u.UfNome))
								.ToArray())
						: null;

					var atasDoItem = x.Compra is not null
						? atas
							.Where(a => a.NumeroControlePncpCompra == x.Compra.NumeroControlePncp)
							.Select(a => new Response.AtaItem(
								a.Id,
								a.NumeroControlePncpAta,
								a.NumeroAtaRegistroPreco,
								a.AnoAta,
								a.ObjetoContratacao,
								a.Cancelado,
								a.DataAssinatura,
								a.VigenciaInicio,
								a.VigenciaFim,
								a.PossibilidadeAdesao))
							.ToArray()
						: Array.Empty<Response.AtaItem>();

					var contratosDoItem = x.Compra is not null
						? contratos
							.Where(c => c.NumeroControlePncpCompra == x.Compra.NumeroControlePncp)
							.Select(c => new Response.ContratoItem(
								c.Id,
								c.NumeroControlePncp,
								c.AnoContrato,
								c.SequencialContrato,
								c.NumeroContratoEmpenho,
								c.ObjetoContrato,
								c.TipoContratoNome,
								c.NiFornecedor,
								c.NomeRazaoSocialFornecedor,
								c.ValorInicial,
								c.ValorGlobal,
								c.DataAssinatura,
								c.DataVigenciaInicio,
								c.DataVigenciaFim))
							.ToArray()
						: Array.Empty<Response.ContratoItem>();

					var adesao = AvaliadorDeAdesao.Avaliar(
						atasDoItem
							.Select(a => new AtaParaAvaliacao(a.NumeroControlePncpAta, a.Cancelado, a.VigenciaFim, a.PossibilidadeAdesao))
							.ToArray(),
						x.TemResultado,
						hoje);

					var resultadosDoItem = x.Resultados.Count > 0
						? x.Resultados
							.Select(r => new Response.ResultadoItem(
								r.Id,
								r.NiFornecedor,
								r.NomeRazaoSocialFornecedor,
								r.QuantidadeHomologada,
								r.ValorUnitarioHomologado,
								r.ValorTotalHomologado,
								r.SituacaoCompraItemResultadoNome,
								r.DataResultado))
							.ToArray()
						: Array.Empty<Response.ResultadoItem>();

					return new Response.Item(
						x.Id,
						x.IdentificadorDaCompra,
						x.NumeroItem,
						x.Descricao,
						x.Quantidade,
						x.UnidadeMedida,
						x.ValorUnitarioEstimado,
						x.ValorTotal,
						x.CriterioJulgamentoNome,
						x.SituacaoCompraItemNome,
						x.TemResultado,
						x.DataAtualizacao,
						x.CriadoEm,
						x.AtualizadoEm,
						compraItem,
						orgaoItem,
						resultadosDoItem,
						atasDoItem,
						contratosDoItem,
						adesao);
				})
				.ToArray();

			var nextCursor = search.HasMoreItems
				? (Int32.TryParse(query.Cursor, out var current) ? current + pagination.Limit : pagination.Limit).ToString()
				: null;

			return Success(new Response(items, search.TotalHits, search.HasMoreItems, nextCursor));
		}
	}
}
