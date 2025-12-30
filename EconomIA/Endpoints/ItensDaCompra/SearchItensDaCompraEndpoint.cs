using System;
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
		[FromQuery] Int32? limit) {
		var result = await mediator.Send(new SearchItensDaCompra.Query(descricao, order, cursor, limit));

		return result.ToOk(Response.From);
	}

	private record Response(Response.Item[] Items, Int64 TotalHits, Boolean HasMoreItems, String? NextCursor) {
		public static Response From(SearchItensDaCompra.Response response) {
			var items = response.Items.Select(x => new Item(
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
				x.Compra is not null ? CompraItem.From(x.Compra) : null,
				x.Orgao is not null ? OrgaoItem.From(x.Orgao) : null,
				x.Atas.Select(AtaItem.From).ToArray(),
				x.Contratos.Select(ContratoItem.From).ToArray()
			)).ToArray();

			return new Response(items, response.TotalHits, response.HasMoreItems, response.NextCursor);
		}

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
			AtaItem[] Atas,
			ContratoItem[] Contratos);

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
			DateTime? DataAberturaProposta,
			DateTime? DataEncerramentoProposta,
			String? AmparoLegalNome,
			String? ModoDisputaNome,
			String? LinkPncp) {
			public static CompraItem From(SearchItensDaCompra.Response.CompraItem c) =>
				new CompraItem(
					c.Id,
					c.NumeroControlePncp,
					c.AnoCompra,
					c.SequencialCompra,
					c.ModalidadeNome,
					c.ObjetoCompra,
					c.ValorTotalEstimado,
					c.ValorTotalHomologado,
					c.SituacaoCompraNome,
					c.DataAberturaProposta,
					c.DataEncerramentoProposta,
					c.AmparoLegalNome,
					c.ModoDisputaNome,
					c.LinkPncp);
		}

		public record OrgaoItem(
			Int64 Id,
			String Cnpj,
			String RazaoSocial,
			String? NomeFantasia,
			UnidadeItem[] Unidades) {
			public static OrgaoItem From(SearchItensDaCompra.Response.OrgaoItem o) =>
				new OrgaoItem(
					o.Id,
					o.Cnpj,
					o.RazaoSocial,
					o.NomeFantasia,
					o.Unidades.Select(UnidadeItem.From).ToArray());
		}

		public record UnidadeItem(
			Int64 Id,
			String CodigoUnidade,
			String NomeUnidade,
			String? MunicipioNome,
			String? UfSigla) {
			public static UnidadeItem From(SearchItensDaCompra.Response.UnidadeItem u) =>
				new UnidadeItem(u.Id, u.CodigoUnidade, u.NomeUnidade, u.MunicipioNome, u.UfSigla);
		}

		public record AtaItem(
			Int64 Id,
			String NumeroControlePncpAta,
			String? NumeroAtaRegistroPreco,
			Int32 AnoAta,
			String? ObjetoContratacao,
			Boolean Cancelado,
			DateTime? DataAssinatura,
			DateTime? VigenciaInicio,
			DateTime? VigenciaFim) {
			public static AtaItem From(SearchItensDaCompra.Response.AtaItem a) =>
				new AtaItem(
					a.Id,
					a.NumeroControlePncpAta,
					a.NumeroAtaRegistroPreco,
					a.AnoAta,
					a.ObjetoContratacao,
					a.Cancelado,
					a.DataAssinatura,
					a.VigenciaInicio,
					a.VigenciaFim);
		}

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
			DateTime? DataVigenciaFim) {
			public static ContratoItem From(SearchItensDaCompra.Response.ContratoItem c) =>
				new ContratoItem(
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
					c.DataVigenciaFim);
		}
	}
}
