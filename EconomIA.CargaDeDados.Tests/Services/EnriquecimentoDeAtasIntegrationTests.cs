using System.Data;
using System.Text.Json;
using EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;
using EconomIA.CargaDeDados.Repositories;
using EconomIA.CargaDeDados.Services;
using EconomIA.CargaDeDados.Tests.Integracao;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Domain.Repositories;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace EconomIA.CargaDeDados.Tests.Services;

public sealed class CenarioDeEnriquecimento : IAsyncLifetime {
	public ElasticsearchClient Client { get; private set; } = null!;
	public IServiceScopeFactory ScopeFactory { get; private set; } = null!;
	public String IndexName { get; } = $"itens-da-compra-testes-{Guid.NewGuid():N}";

	public const Int64 NotebookComAtaPermitida = 10;
	public const Int64 NotebookSemResultado = 11;
	public const Int64 LimpezaComAtaSemInformacaoDeAdesao = 20;
	public const Int64 CadeiraComAtaVencida = 30;
	public const Int64 MesaComAtaCancelada = 40;
	public const Int64 PapelSemAta = 50;
	public const Int64 ImpressoraComDuasAtas = 60;

	public static readonly DateTime VigenciaLonga = new DateTime(2099, 12, 31);
	public static readonly DateTime VigenciaMedia = new DateTime(2099, 6, 30);
	public static readonly DateTime VigenciaCurta = new DateTime(2098, 12, 31);

	public async Task InitializeAsync() {
		if (!AmbienteDeIntegracao.Completo) {
			return;
		}

		Client = new ElasticsearchClient(
			new ElasticsearchClientSettings(new Uri(AmbienteDeIntegracao.UrlDoElasticsearch!)).DefaultIndex(IndexName));

		var connectionString = AmbienteDeIntegracao.ConnectionStringDoPostgres!;
		var services = new ServiceCollection();
		services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(connectionString));
		services.AddTransient<ItensDaCompra>();
		ScopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

		await PrepararBanco(connectionString);
		await PrepararIndice();
	}

	public async Task DisposeAsync() {
		if (Client is not null) {
			await Client.Indices.DeleteAsync(IndexName);
		}
	}

	private static async Task PrepararBanco(String connectionString) {
		await using var conexao = new NpgsqlConnection(connectionString);
		await conexao.OpenAsync();

		await Executar(conexao, await File.ReadAllTextAsync(LocalizarArquivo(Path.Combine("docker", "postgresql", "01_criar_banco.sql"))));
		await Executar(conexao, "alter table public.ata add column if not exists possibilidade_adesao boolean;");
		await Executar(conexao, "truncate table public.ata, public.item_da_compra, public.compra, public.orgao restart identity cascade;");

		await Executar(conexao, @"
			insert into public.orgao (identificador, cnpj, razao_social)
			values (1, '11222333000144', 'ORGAO DE TESTE');

			insert into public.compra (identificador, identificador_do_orgao, numero_controle_pncp, ano_compra, sequencial_compra, modalidade_identificador) values
				(1, 1, '11222333000144-1-000001/2026', 2026, 1, 6),
				(2, 1, '11222333000144-1-000002/2026', 2026, 2, 6),
				(3, 1, '11222333000144-1-000003/2026', 2026, 3, 6),
				(4, 1, '11222333000144-1-000004/2026', 2026, 4, 6),
				(5, 1, '11222333000144-1-000005/2026', 2026, 5, 6),
				(6, 1, '11222333000144-1-000006/2026', 2026, 6, 6);

			insert into public.item_da_compra (identificador, identificador_da_compra, numero_item, descricao, tem_resultado) values
				(10, 1, 1, 'Notebook com ata permitida', true),
				(11, 1, 2, 'Notebook sem resultado homologado', false),
				(20, 2, 1, 'Material de limpeza com ata sem informacao de adesao', true),
				(30, 3, 1, 'Cadeira com ata vencida', true),
				(40, 4, 1, 'Mesa com ata cancelada', true),
				(50, 5, 1, 'Papel sem ata', true),
				(60, 6, 1, 'Impressora com duas atas', true);

			insert into public.ata (identificador_do_orgao, numero_controle_pncp_ata, numero_controle_pncp_compra, ano_ata, cancelado, data_assinatura, vigencia_inicio, vigencia_fim, data_publicacao_pncp, possibilidade_adesao) values
				(1, '11222333000144-1-000001/2026-000001', '11222333000144-1-000001/2026', 2026, false, '2026-08-10', '2026-08-10', '2099-12-31', '2026-08-11 09:00:00', true),
				(1, '11222333000144-1-000002/2026-000001', '11222333000144-1-000002/2026', 2026, false, null, '2026-08-01', '2099-06-30', '2026-08-01 12:00:00', null),
				(1, '11222333000144-1-000003/2026-000001', '11222333000144-1-000003/2026', 2026, false, '2025-01-10', '2025-01-10', '2025-12-31', null, true),
				(1, '11222333000144-1-000004/2026-000001', '11222333000144-1-000004/2026', 2026, true, '2026-08-12', '2026-08-12', '2099-12-31', null, true),
				(1, '11222333000144-1-000006/2026-000001', '11222333000144-1-000006/2026', 2026, false, '2026-07-01', '2026-07-01', '2098-12-31', null, false),
				(1, '11222333000144-1-000006/2026-000002', '11222333000144-1-000006/2026', 2026, false, '2026-08-15', '2026-08-15', '2099-12-31', null, true);
		");
	}

	private static async Task Executar(NpgsqlConnection conexao, String sql) {
		await using var comando = new NpgsqlCommand(sql, conexao);
		await comando.ExecuteNonQueryAsync();
	}

	private static String LocalizarArquivo(String caminhoRelativo) {
		var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

		while (diretorio is not null) {
			var candidato = Path.Combine(diretorio.FullName, caminhoRelativo);

			if (File.Exists(candidato)) {
				return candidato;
			}

			diretorio = diretorio.Parent;
		}

		throw new FileNotFoundException($"Arquivo {caminhoRelativo} não encontrado a partir de {AppContext.BaseDirectory}.");
	}

	private async Task PrepararIndice() {
		var criacao = await Client.Indices.CreateAsync(new CreateIndexRequest(IndexName) {
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
		});
		criacao.IsValidResponse.Should().BeTrue(criacao.DebugInformation);

		var documentos = new List<DocumentoDoIndice> {
			Documento(NotebookComAtaPermitida, "Notebook com ata permitida", "GO", new DateTime(2026, 7, 28)),
			Documento(NotebookSemResultado, "Notebook sem resultado homologado", "GO", new DateTime(2026, 7, 28)),
			Documento(LimpezaComAtaSemInformacaoDeAdesao, "Material de limpeza com ata sem informacao de adesao", "SP", new DateTime(2026, 7, 1), ataAdesaoVigenciaFimDesatualizada: new DateTime(2099, 1, 1)),
			Documento(CadeiraComAtaVencida, "Cadeira com ata vencida", "MG", new DateTime(2024, 12, 1)),
			Documento(MesaComAtaCancelada, "Mesa com ata cancelada", "MG", new DateTime(2026, 8, 1)),
			Documento(PapelSemAta, "Papel sem ata", "RJ", new DateTime(2026, 8, 18)),
			Documento(ImpressoraComDuasAtas, "Impressora com duas atas", "BA", new DateTime(2026, 6, 15))
		};

		var bulk = await Client.BulkAsync(b => b.Index(IndexName).IndexMany(documentos, (op, d) => op.Id(d.Id)));
		bulk.IsValidResponse.Should().BeTrue(bulk.DebugInformation);
		bulk.Errors.Should().BeFalse();

		await Client.Indices.RefreshAsync(IndexName);
	}

	private static DocumentoDoIndice Documento(Int64 id, String descricao, String uf, DateTime dataInclusao, DateTime? ataAdesaoVigenciaFimDesatualizada = null) {
		return new DocumentoDoIndice {
			Id = id,
			Descricao = descricao,
			Orgao = "ORGAO DE TESTE",
			UfSigla = uf,
			DataInclusao = dataInclusao,
			AtaAdesaoVigenciaFim = ataAdesaoVigenciaFimDesatualizada
		};
	}

	public async Task<Dictionary<String, JsonElement>> LerDocumento(Int64 id) {
		var resposta = await Client.GetAsync<Dictionary<String, JsonElement>>(id, g => g.Index(IndexName));
		resposta.IsValidResponse.Should().BeTrue(resposta.DebugInformation);
		resposta.Found.Should().BeTrue($"o documento {id} deveria existir no índice");
		return resposta.Source!;
	}

	public sealed class DocumentoDoIndice {
		public Int64 Id { get; set; }
		public String Descricao { get; set; } = String.Empty;
		public String Orgao { get; set; } = String.Empty;
		public String? UfSigla { get; set; }
		public DateTime? DataInclusao { get; set; }
		public DateTime? AtaAdesaoVigenciaFim { get; set; }
	}
}

public class EnriquecimentoDeAtasIntegrationTests : IClassFixture<CenarioDeEnriquecimento>, IAsyncLifetime {
	private readonly CenarioDeEnriquecimento cenario;
	private ResultadoEnriquecimento resultado = null!;

	public EnriquecimentoDeAtasIntegrationTests(CenarioDeEnriquecimento cenario) {
		this.cenario = cenario;
	}

	public async Task InitializeAsync() {
		if (!AmbienteDeIntegracao.Completo) {
			return;
		}

		var servico = new ServicoCargaBrasil(
			new HttpClient(),
			cenario.Client,
			cenario.ScopeFactory,
			NullLogger<ServicoCargaBrasil>.Instance);

		resultado = await servico.EnriquecerIndiceComAtasAsync();
		await cenario.Client.Indices.RefreshAsync(cenario.IndexName);
	}

	public Task DisposeAsync() {
		return Task.CompletedTask;
	}

	[FatoDeIntegracaoCompleta]
	public void enriquece_somente_itens_com_resultado_e_ata_vigente_nao_cancelada() {
		resultado.DocumentosEnriquecidos.Should().Be(3);
	}

	[FatoDeIntegracaoCompleta]
	public async Task grava_vigencia_data_de_referencia_e_adesao_da_ata_permitida() {
		var documento = await cenario.LerDocumento(CenarioDeEnriquecimento.NotebookComAtaPermitida);

		Data(documento, ElasticsearchItemSearcher.CampoAtaVigenciaFim).Should().Be(CenarioDeEnriquecimento.VigenciaLonga);
		Data(documento, ElasticsearchItemSearcher.CampoAtaDataDeReferencia).Should().Be(new DateTime(2026, 8, 10));
		Data(documento, ElasticsearchItemSearcher.CampoAtaAdesaoVigenciaFim).Should().Be(CenarioDeEnriquecimento.VigenciaLonga);
	}

	[FatoDeIntegracaoCompleta]
	public async Task usa_a_data_de_publicacao_quando_nao_ha_assinatura_e_limpa_adesao_desatualizada() {
		var documento = await cenario.LerDocumento(CenarioDeEnriquecimento.LimpezaComAtaSemInformacaoDeAdesao);

		Data(documento, ElasticsearchItemSearcher.CampoAtaVigenciaFim).Should().Be(CenarioDeEnriquecimento.VigenciaMedia);
		Data(documento, ElasticsearchItemSearcher.CampoAtaDataDeReferencia).Should().Be(new DateTime(2026, 8, 1, 12, 0, 0));
		EstaNuloOuAusente(documento, ElasticsearchItemSearcher.CampoAtaAdesaoVigenciaFim).Should().BeTrue(
			"a ata vigente não informa adesão, então o valor antigo precisa ser apagado do índice");
	}

	[FatoDeIntegracaoCompleta]
	public async Task com_varias_atas_considera_a_mais_recente_e_a_adesao_permitida() {
		var documento = await cenario.LerDocumento(CenarioDeEnriquecimento.ImpressoraComDuasAtas);

		Data(documento, ElasticsearchItemSearcher.CampoAtaVigenciaFim).Should().Be(CenarioDeEnriquecimento.VigenciaLonga);
		Data(documento, ElasticsearchItemSearcher.CampoAtaDataDeReferencia).Should().Be(new DateTime(2026, 8, 15));
		Data(documento, ElasticsearchItemSearcher.CampoAtaAdesaoVigenciaFim).Should().Be(CenarioDeEnriquecimento.VigenciaLonga);
	}

	[FatoDeIntegracaoCompleta]
	public async Task atualizacao_parcial_preserva_os_demais_campos_do_documento() {
		var documento = await cenario.LerDocumento(CenarioDeEnriquecimento.NotebookComAtaPermitida);

		documento["descricao"].GetString().Should().Be("Notebook com ata permitida");
		documento["ufSigla"].GetString().Should().Be("GO");
		Data(documento, "dataInclusao").Should().Be(new DateTime(2026, 7, 28));
	}

	[TeoriaDeIntegracaoCompleta]
	[InlineData(CenarioDeEnriquecimento.NotebookSemResultado)]
	[InlineData(CenarioDeEnriquecimento.CadeiraComAtaVencida)]
	[InlineData(CenarioDeEnriquecimento.MesaComAtaCancelada)]
	[InlineData(CenarioDeEnriquecimento.PapelSemAta)]
	public async Task nao_toca_itens_sem_resultado_sem_ata_ou_com_ata_vencida_ou_cancelada(Int64 id) {
		var documento = await cenario.LerDocumento(id);

		EstaNuloOuAusente(documento, ElasticsearchItemSearcher.CampoAtaVigenciaFim).Should().BeTrue();
		EstaNuloOuAusente(documento, ElasticsearchItemSearcher.CampoAtaDataDeReferencia).Should().BeTrue();
		EstaNuloOuAusente(documento, ElasticsearchItemSearcher.CampoAtaAdesaoVigenciaFim).Should().BeTrue();
	}

	[FatoDeIntegracaoCompleta]
	public async Task busca_sem_descricao_apenas_com_ata_vigente_devolve_os_enriquecidos_pela_data_da_ata() {
		var busca = await Buscar(null, new SearchFilters(null, null, null, null, null, null, null, null, SomenteComAtaVigente: true));

		busca.Ids.Should().Equal(
			CenarioDeEnriquecimento.ImpressoraComDuasAtas,
			CenarioDeEnriquecimento.NotebookComAtaPermitida,
			CenarioDeEnriquecimento.LimpezaComAtaSemInformacaoDeAdesao);
		busca.TotalHits.Should().Be(3);
	}

	[FatoDeIntegracaoCompleta]
	public async Task busca_apenas_com_adesao_ignora_ata_sem_informacao_mesmo_com_valor_antigo_no_indice() {
		var busca = await Buscar(null, new SearchFilters(null, null, null, null, null, null, null, null, SomenteComAdesao: true));

		busca.Ids.Should().Equal(
			CenarioDeEnriquecimento.ImpressoraComDuasAtas,
			CenarioDeEnriquecimento.NotebookComAtaPermitida);
	}

	[FatoDeIntegracaoCompleta]
	public async Task busca_por_periodo_da_ata_usa_a_data_de_referencia_gravada() {
		var desde = await Buscar(null, new SearchFilters(null, null, null, null, null, null, null, null, DataDaAtaInicio: new DateTime(2026, 8, 5)));
		desde.Ids.Should().Equal(CenarioDeEnriquecimento.ImpressoraComDuasAtas, CenarioDeEnriquecimento.NotebookComAtaPermitida);

		var janela = await Buscar(null, new SearchFilters(null, null, null, null, null, null, null, null,
			DataDaAtaInicio: new DateTime(2026, 8, 1),
			DataDaAtaFim: new DateTime(2026, 8, 10)));
		janela.Ids.Should().Equal(CenarioDeEnriquecimento.NotebookComAtaPermitida, CenarioDeEnriquecimento.LimpezaComAtaSemInformacaoDeAdesao);
	}

	[FatoDeIntegracaoCompleta]
	public async Task busca_textual_continua_funcionando_e_respeita_o_filtro_de_ata() {
		var semFiltro = await Buscar("notebook", null);
		semFiltro.Ids.Should().BeEquivalentTo(new[] { CenarioDeEnriquecimento.NotebookComAtaPermitida, CenarioDeEnriquecimento.NotebookSemResultado });

		var comAta = await Buscar("notebook", new SearchFilters(null, null, null, null, null, null, null, null, SomenteComAtaVigente: true));
		comAta.Ids.Should().Equal(CenarioDeEnriquecimento.NotebookComAtaPermitida);
	}

	private async Task<SearchResult> Buscar(String? descricao, SearchFilters? filtros) {
		var searcher = new ElasticsearchItemSearcher(cenario.Client, cenario.IndexName);
		var busca = await searcher.Search(descricao, filtros, PaginationParameters.Create(null, null, 50).Value);

		busca.IsSuccess.Should().BeTrue(busca.IsFailure ? busca.Error.ToString() : String.Empty);
		return busca.Value;
	}

	private static DateTime Data(Dictionary<String, JsonElement> documento, String campo) {
		documento.Should().ContainKey(campo);
		documento[campo].ValueKind.Should().NotBe(JsonValueKind.Null, $"{campo} deveria estar preenchido");
		return DateTime.Parse(documento[campo].GetString()!, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
	}

	private static Boolean EstaNuloOuAusente(Dictionary<String, JsonElement> documento, String campo) {
		return !documento.TryGetValue(campo, out var valor) || valor.ValueKind == JsonValueKind.Null;
	}
}
