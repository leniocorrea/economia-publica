using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Application.Queries.SearchItensDaCompra;
using EconomIA.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EconomIA.Endpoints.ItensDaCompra;

public static class SearchItensDaCompraEndpoint {
	public static IEndpointRouteBuilder MapSearchItensDaCompra(this IEndpointRouteBuilder app) {
		app.MapGet("/v1/itens-da-compra", Handle)
			.WithName("SearchItensDaCompra")
			.WithTags("Itens da Compra");

		return app;
	}

	private static async Task<IResult> Handle(
		[FromServices] IMediator mediator,
		[FromQuery] String descricao,
		[FromQuery] String? order,
		[FromQuery] String? cursor,
		[FromQuery] Int32? limit,
		[FromQuery] DateTime? dataInclusaoInicio,
		[FromQuery] DateTime? dataInclusaoFim,
		[FromQuery] String? razaoSocial,
		[FromQuery] String? ufSigla,
		[FromQuery] Decimal? valorUnitarioHomologadoMinimo,
		[FromQuery] Decimal? valorUnitarioHomologadoMaximo,
		[FromQuery] Decimal? valorTotalHomologadoMinimo,
		[FromQuery] Decimal? valorTotalHomologadoMaximo) {
		var result = await mediator.Send(new SearchItensDaCompra.Query(
			descricao,
			order,
			cursor,
			limit,
			dataInclusaoInicio,
			dataInclusaoFim,
			razaoSocial,
			ufSigla,
			valorUnitarioHomologadoMinimo,
			valorUnitarioHomologadoMaximo,
			valorTotalHomologadoMinimo,
			valorTotalHomologadoMaximo));

		return result.ToOk(Response.From);
	}

	private record Response(Resultado[] Resultado, Int64 TotalHits, Boolean HasMoreItems, String? NextCursor) {
		public static Response From(SearchItensDaCompra.Response response) {
			var comprasAgrupadas = response.Items
				.Where(x => x.Compra is not null)
				.GroupBy(x => x.Compra!.NumeroControlePncp)
				.Select(g => {
					var primeiroItem = g.First();
					var compra = primeiroItem.Compra!;
					var orgao = primeiroItem.Orgao;

					var unidade = orgao?.Unidades.FirstOrDefault();

					return new Resultado(
						orgao is not null ? new OrgaoEntidade(
							orgao.Cnpj,
							orgao.RazaoSocial,
							orgao.PoderId,
							orgao.EsferaId,
							unidade is not null ? new UnidadeDoOrgao(unidade.MunicipioCodigoIbge) : null
						) : null,
						new Compra(
							compra.AnoCompra,
							compra.SequencialCompra,
							$"{compra.AnoCompra}/{compra.SequencialCompra}",
							null,
							compra.ObjetoCompra,
							unidade?.UfNome ?? null,
							compra.DataInclusao,
							compra.DataAberturaProposta,
							compra.DataEncerramentoProposta,
							compra.LinkPncp,
							compra.NumeroControlePncp,
							compra.ModalidadeNome,
							compra.SituacaoCompraNome,
							unidade?.UfSigla,
							g.Select(item => new ItemDaCompra(
								item.NumeroItem,
								item.Descricao,
								null,
								item.ValorUnitarioEstimado,
								item.ValorTotal,
								item.Quantidade,
								item.UnidadeMedida,
								item.CriterioJulgamentoNome,
								item.SituacaoCompraItemNome,
								item.Resultados.Select(r => new ResultadoItem(
									item.NumeroItem,
									r.NiFornecedor,
									null,
									r.NomeRazaoSocialFornecedor,
									null,
									r.QuantidadeHomologada,
									r.ValorUnitarioHomologado,
									r.ValorTotalHomologado,
									null,
									compra.NumeroControlePncp
								)).ToArray()
							)).ToArray()
						)
					);
				}).ToArray();

			return new Response(comprasAgrupadas, response.TotalHits, response.HasMoreItems, response.NextCursor);
		}
	}

	private record Resultado(
		OrgaoEntidade? OrgaoEntidade,
		Compra Compra);

	private record OrgaoEntidade(
		String Cnpj,
		String RazaoSocial,
		String? PoderId,
		String? EsferaId,
		UnidadeDoOrgao? UnidadeDoOrgao);

	private record UnidadeDoOrgao(
		String? CodigoIbge);

	private record Compra(
		Int32 AnoCompra,
		Int32 SequencialCompra,
		String NumeroCompra,
		String? Processo,
		String? ObjetoCompra,
		String? UfNome,
		DateTime? DataInclusao,
		DateTime? DataAberturaProposta,
		DateTime? DataEncerramentoProposta,
		String? LinkSistemaOrigem,
		String NumeroControlePNCP,
		String? ModalidadeNome,
		String? SituacaoCompraNome,
		String? UfSigla,
		ItemDaCompra[] ItemDaCompra);

	private record ItemDaCompra(
		Int32 NumeroItem,
		String? Descricao,
		String? MaterialOuServico,
		Decimal? ValorUnitarioEstimado,
		Decimal? ValorTotal,
		Decimal? Quantidade,
		String? UnidadeMedida,
		String? CriterioJulgamentoNome,
		String? SituacaoCompraItemNome,
		ResultadoItem[] Resultado);

	private record ResultadoItem(
		Int32 NumeroItem,
		String? NiFornecedor,
		String? TipoPessoa,
		String? NomeRazaoSocialFornecedor,
		String? PorteFornecedorId,
		Decimal? QuantidadeHomologada,
		Decimal? ValorUnitarioHomologado,
		Decimal? ValorTotalHomologado,
		Int32? OrdemClassificacaoSrp,
		String NumeroControlePNCPCompra);
}
