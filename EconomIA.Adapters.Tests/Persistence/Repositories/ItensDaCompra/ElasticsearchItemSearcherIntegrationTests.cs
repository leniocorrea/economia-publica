using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;
using EconomIA.Adapters.Tests.Integracao;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Domain.Repositories;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using FluentAssertions;
using Xunit;

namespace EconomIA.Adapters.Tests.Persistence.Repositories.ItensDaCompra;

public sealed class IndiceDeItensParaTeste : IAsyncLifetime {
	public ElasticsearchClient Client { get; private set; } = null!;
	public String IndexName { get; } = $"itens-da-compra-testes-{Guid.NewGuid():N}";

	public const Int64 NotebookRecenteSemAta = 1;
	public const Int64 NotebookComAdesaoPermitida = 2;
	public const Int64 LimpezaComAtaSemInformacaoDeAdesao = 3;
	public const Int64 NotebookComAtaVencida = 4;
	public const Int64 NotebookComAtaVigenteEmSp = 5;

	public async Task InitializeAsync() {
		var url = AmbienteDeIntegracao.UrlDoElasticsearch;

		if (url is null) {
			return;
		}

		Client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(url)).DefaultIndex(IndexName));

		await CriarIndiceComMapeamentoDeProducao(Client, IndexName);
		await IndexarDocumentos();
	}

	public async Task DisposeAsync() {
		if (Client is not null) {
			await Client.Indices.DeleteAsync(IndexName);
		}
	}

	public static async Task CriarIndiceComMapeamentoDeProducao(ElasticsearchClient client, String indexName) {
		var request = new CreateIndexRequest(indexName) {
			Mappings = new TypeMapping {
				Properties = new Properties {
					{ "id", new LongNumberProperty() },
					{ "descricao", new TextProperty() },
					{ "orgao", new TextProperty() },
					{ "ufSigla", new KeywordProperty() },
					{ "dataInclusao", new DateProperty() },
					{ ElasticsearchItemSearcher.CampoAtaAdesaoVigenciaFim, new DateProperty() },
					{ ElasticsearchItemSearcher.CampoAtaVigenciaFim, new DateProperty() },
					{ ElasticsearchItemSearcher.CampoAtaDataDeReferencia, new DateProperty() }
				}
			}
		};

		var response = await client.Indices.CreateAsync(request);
		response.IsValidResponse.Should().BeTrue(response.DebugInformation);
	}

	private async Task IndexarDocumentos() {
		var documentos = new List<DocumentoDoIndice> {
			new DocumentoDoIndice {
				Id = NotebookRecenteSemAta,
				Descricao = "Notebook Dell i7",
				Orgao = "PREFEITURA DE SAO PAULO",
				UfSigla = "SP",
				DataInclusao = new DateTime(2026, 8, 18)
			},
			new DocumentoDoIndice {
				Id = NotebookComAdesaoPermitida,
				Descricao = "Notebook Lenovo",
				Orgao = "PREFEITURA DE BELO HORIZONTE",
				UfSigla = "MG",
				DataInclusao = new DateTime(2026, 5, 10),
				AtaVigenciaFim = new DateTime(2099, 12, 31),
				AtaDataDeReferencia = new DateTime(2026, 8, 10, 15, 30, 0),
				AtaAdesaoVigenciaFim = new DateTime(2099, 12, 31)
			},
			new DocumentoDoIndice {
				Id = LimpezaComAtaSemInformacaoDeAdesao,
				Descricao = "Material de limpeza",
				Orgao = "PREFEITURA DE GOIANIA",
				UfSigla = "GO",
				DataInclusao = new DateTime(2026, 3, 1),
				AtaVigenciaFim = new DateTime(2099, 6, 30),
				AtaDataDeReferencia = new DateTime(2026, 8, 1)
			},
			new DocumentoDoIndice {
				Id = NotebookComAtaVencida,
				Descricao = "Notebook Acer",
				Orgao = "PREFEITURA DO RIO DE JANEIRO",
				UfSigla = "RJ",
				DataInclusao = new DateTime(2026, 1, 15),
				AtaVigenciaFim = new DateTime(2020, 1, 31),
				AtaDataDeReferencia = new DateTime(2019, 12, 1)
			},
			new DocumentoDoIndice {
				Id = NotebookComAtaVigenteEmSp,
				Descricao = "Notebook HP",
				Orgao = "TRIBUNAL DE CONTAS DE SAO PAULO",
				UfSigla = "SP",
				DataInclusao = new DateTime(2026, 2, 1),
				AtaVigenciaFim = new DateTime(2099, 1, 31),
				AtaDataDeReferencia = new DateTime(2026, 7, 25)
			}
		};

		var bulk = await Client.BulkAsync(b => b
			.Index(IndexName)
			.IndexMany(documentos, (op, d) => op.Id(d.Id)));
		bulk.IsValidResponse.Should().BeTrue(bulk.DebugInformation);
		bulk.Errors.Should().BeFalse();

		await Client.Indices.RefreshAsync(IndexName);
	}

	public sealed class DocumentoDoIndice {
		public Int64 Id { get; set; }
		public String Descricao { get; set; } = String.Empty;
		public String Orgao { get; set; } = String.Empty;
		public String? UfSigla { get; set; }
		public DateTime? DataInclusao { get; set; }
		public DateTime? AtaAdesaoVigenciaFim { get; set; }
		public DateTime? AtaVigenciaFim { get; set; }
		public DateTime? AtaDataDeReferencia { get; set; }
	}
}

public class ElasticsearchItemSearcherIntegrationTests : IClassFixture<IndiceDeItensParaTeste> {
	private readonly IndiceDeItensParaTeste indice;

	public ElasticsearchItemSearcherIntegrationTests(IndiceDeItensParaTeste indice) {
		this.indice = indice;
	}

	private ElasticsearchItemSearcher CriarSearcher() {
		return new ElasticsearchItemSearcher(indice.Client, indice.IndexName);
	}

	private static SearchFilters Filtros(
		Boolean? somenteComAdesao = null,
		Boolean? somenteComAtaVigente = null,
		DateTime? dataDaAtaInicio = null,
		DateTime? dataDaAtaFim = null,
		String? ufSigla = null,
		DateTime? dataInclusaoInicio = null,
		DateTime? dataInclusaoFim = null) {
		return new SearchFilters(
			dataInclusaoInicio, dataInclusaoFim, null, ufSigla, null, null, null, null,
			somenteComAdesao, somenteComAtaVigente, dataDaAtaInicio, dataDaAtaFim);
	}

	private static async Task<SearchResult> Buscar(ElasticsearchItemSearcher searcher, String? descricao, SearchFilters? filtros, Int32 limit = 50, String? cursor = null) {
		var paginacao = PaginationParameters.Create(null, cursor, limit).Value;
		var resultado = await searcher.Search(descricao, filtros, paginacao);

		resultado.IsSuccess.Should().BeTrue(resultado.IsFailure ? resultado.Error.ToString() : String.Empty);
		return resultado.Value;
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task busca_textual_sem_filtro_de_ata_segue_ignorando_as_atas() {
		var resultado = await Buscar(CriarSearcher(), "notebook", null);

		resultado.Ids.Should().BeEquivalentTo(new[] {
			IndiceDeItensParaTeste.NotebookRecenteSemAta,
			IndiceDeItensParaTeste.NotebookComAdesaoPermitida,
			IndiceDeItensParaTeste.NotebookComAtaVencida,
			IndiceDeItensParaTeste.NotebookComAtaVigenteEmSp
		});
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task busca_textual_com_filtro_de_ata_vigente_mantem_o_texto_e_descarta_sem_ata_e_vencidas() {
		var resultado = await Buscar(CriarSearcher(), "notebook", Filtros(somenteComAtaVigente: true));

		resultado.Ids.Should().BeEquivalentTo(new[] {
			IndiceDeItensParaTeste.NotebookComAdesaoPermitida,
			IndiceDeItensParaTeste.NotebookComAtaVigenteEmSp
		});
		resultado.TotalHits.Should().Be(2);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task sem_descricao_e_sem_filtro_de_ata_ordena_por_data_de_inclusao_decrescente() {
		var resultado = await Buscar(CriarSearcher(), null, null);

		resultado.Ids.Should().Equal(
			IndiceDeItensParaTeste.NotebookRecenteSemAta,
			IndiceDeItensParaTeste.NotebookComAdesaoPermitida,
			IndiceDeItensParaTeste.LimpezaComAtaSemInformacaoDeAdesao,
			IndiceDeItensParaTeste.NotebookComAtaVigenteEmSp,
			IndiceDeItensParaTeste.NotebookComAtaVencida);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task sem_descricao_apenas_com_ata_vigente_ordena_pela_data_da_ata_mais_recente() {
		var resultado = await Buscar(CriarSearcher(), null, Filtros(somenteComAtaVigente: true));

		resultado.Ids.Should().Equal(
			IndiceDeItensParaTeste.NotebookComAdesaoPermitida,
			IndiceDeItensParaTeste.LimpezaComAtaSemInformacaoDeAdesao,
			IndiceDeItensParaTeste.NotebookComAtaVigenteEmSp);
		resultado.TotalHits.Should().Be(3);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task periodo_da_ata_inclui_o_dia_final_por_inteiro() {
		var resultado = await Buscar(CriarSearcher(), null, Filtros(
			dataDaAtaInicio: new DateTime(2026, 8, 1),
			dataDaAtaFim: new DateTime(2026, 8, 10)));

		resultado.Ids.Should().Equal(
			IndiceDeItensParaTeste.NotebookComAdesaoPermitida,
			IndiceDeItensParaTeste.LimpezaComAtaSemInformacaoDeAdesao);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task periodo_da_ata_aceita_apenas_a_data_inicial() {
		var resultado = await Buscar(CriarSearcher(), null, Filtros(dataDaAtaInicio: new DateTime(2026, 8, 5)));

		resultado.Ids.Should().Equal(IndiceDeItensParaTeste.NotebookComAdesaoPermitida);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task apenas_com_adesao_continua_filtrando_pela_vigencia_da_ata_permitida() {
		var resultado = await Buscar(CriarSearcher(), null, Filtros(somenteComAdesao: true));

		resultado.Ids.Should().Equal(IndiceDeItensParaTeste.NotebookComAdesaoPermitida);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task filtros_de_ata_combinam_com_uf_e_periodo_de_inclusao_da_compra() {
		var porUf = await Buscar(CriarSearcher(), null, Filtros(somenteComAtaVigente: true, ufSigla: "sp"));
		porUf.Ids.Should().Equal(IndiceDeItensParaTeste.NotebookComAtaVigenteEmSp);

		var porInclusao = await Buscar(CriarSearcher(), null, Filtros(
			somenteComAtaVigente: true,
			dataInclusaoInicio: new DateTime(2026, 4, 1),
			dataInclusaoFim: new DateTime(2026, 12, 31)));
		porInclusao.Ids.Should().Equal(IndiceDeItensParaTeste.NotebookComAdesaoPermitida);
	}

	[FatoDeIntegracaoComElasticsearch]
	public async Task paginacao_sem_descricao_respeita_limit_e_cursor_com_ordem_estavel() {
		var searcher = CriarSearcher();

		var primeira = await Buscar(searcher, null, null, limit: 2);
		primeira.Ids.Should().Equal(IndiceDeItensParaTeste.NotebookRecenteSemAta, IndiceDeItensParaTeste.NotebookComAdesaoPermitida);
		primeira.HasMoreItems.Should().BeTrue();

		var segunda = await Buscar(searcher, null, null, limit: 2, cursor: "2");
		segunda.Ids.Should().Equal(IndiceDeItensParaTeste.LimpezaComAtaSemInformacaoDeAdesao, IndiceDeItensParaTeste.NotebookComAtaVigenteEmSp);
		segunda.HasMoreItems.Should().BeTrue();

		var terceira = await Buscar(searcher, null, null, limit: 2, cursor: "4");
		terceira.Ids.Should().Equal(IndiceDeItensParaTeste.NotebookComAtaVencida);
		terceira.HasMoreItems.Should().BeFalse();
	}
}
