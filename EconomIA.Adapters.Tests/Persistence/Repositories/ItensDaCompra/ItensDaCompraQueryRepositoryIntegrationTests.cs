using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Adapters.Persistence;
using EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;
using EconomIA.Adapters.Tests.Integracao;
using EconomIA.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace EconomIA.Adapters.Tests.Persistence.Repositories.ItensDaCompra;

file class FabricaDeContextoDeConsulta(DbContextOptions<EconomIAQueryDbContext> opcoes) : IDbContextFactory<EconomIAQueryDbContext> {
	public EconomIAQueryDbContext CreateDbContext() {
		return new EconomIAQueryDbContext(opcoes);
	}
}

public sealed class BancoDeItensParaTeste : IAsyncLifetime {
	private readonly String nomeDoBanco = $"economia_testes_itens_{Guid.NewGuid():N}";

	public IDbContextFactory<EconomIAQueryDbContext>? Factory { get; private set; }

	public const String CnpjDoOrgao = "88600655000141";
	public const Int32 AnoDaCompraDeMerenda = 2026;
	public const Int32 SequencialDaCompraDeMerenda = 316;

	public async Task InitializeAsync() {
		var connectionString = AmbienteDeIntegracao.ConnectionStringDoPostgres;

		if (connectionString is null) {
			return;
		}

		await CriarBanco(connectionString);

		var opcoes = new DbContextOptionsBuilder<EconomIAQueryDbContext>()
			.UseNpgsql(ConexaoComOBancoDoTeste(connectionString))
			.Options;

		Factory = new FabricaDeContextoDeConsulta(opcoes);
	}

	public async Task DisposeAsync() {
		var connectionString = AmbienteDeIntegracao.ConnectionStringDoPostgres;

		if (connectionString is null) {
			return;
		}

		await using var conexao = new NpgsqlConnection(connectionString);
		await conexao.OpenAsync();
		await Executar(conexao, $"drop database if exists {nomeDoBanco} with (force);");
	}

	private String ConexaoComOBancoDoTeste(String connectionString) {
		return new NpgsqlConnectionStringBuilder(connectionString) { Database = nomeDoBanco }.ConnectionString;
	}

	private async Task CriarBanco(String connectionString) {
		await using (var administrativa = new NpgsqlConnection(connectionString)) {
			await administrativa.OpenAsync();
			await Executar(administrativa, $"create database {nomeDoBanco};");
		}

		await using var conexao = new NpgsqlConnection(ConexaoComOBancoDoTeste(connectionString));
		await conexao.OpenAsync();

		await Executar(conexao, await File.ReadAllTextAsync(LocalizarArquivo(Path.Combine("docker", "postgresql", "01_criar_banco.sql"))));

		await Executar(conexao, @"
			insert into public.orgao (identificador, cnpj, razao_social) values
				(1, '88600655000141', 'PREFEITURA DE TESTE'),
				(2, '11222333000144', 'OUTRO ORGAO');

			insert into public.compra (identificador, identificador_do_orgao, numero_controle_pncp, ano_compra, sequencial_compra, modalidade_identificador, objeto_compra) values
				(1, 1, '88600655000141-1-000316/2026', 2026, 316, 6, 'Aquisicao de generos alimenticios para a MERENDA escolar'),
				(2, 1, '88600655000141-1-000317/2026', 2026, 317, 6, 'Aquisicao de equipamentos de informatica'),
				(3, 1, '88600655000141-1-000316/2025', 2025, 316, 6, 'Compra do ano anterior com o mesmo sequencial'),
				(4, 2, '11222333000144-1-000316/2026', 2026, 316, 6, 'Compra de outro orgao com a mesma chave parcial');

			insert into public.item_da_compra (identificador, identificador_da_compra, numero_item, descricao, tem_resultado) values
				(10, 1, 3, 'Arroz tipo 1 pacote 5kg', true),
				(11, 1, 1, 'Feijao carioca pacote 1kg', true),
				(12, 1, 2, 'Cadeira escolar empilhavel', false),
				(20, 2, 1, 'Notebook 16GB', true),
				(30, 3, 1, 'Arroz do ano anterior', true),
				(40, 4, 1, 'Arroz de outro orgao', true);

			insert into public.resultado_do_item (identificador_do_item_da_compra, valor_unitario_homologado, valor_total_homologado) values
				(10, 25.00, 2500.00),
				(11, 8.00, 800.00);
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
}

public class ItensDaCompraQueryRepositoryIntegrationTests : IClassFixture<BancoDeItensParaTeste> {
	private readonly BancoDeItensParaTeste banco;

	public ItensDaCompraQueryRepositoryIntegrationTests(BancoDeItensParaTeste banco) {
		this.banco = banco;
	}

	[FatoDeIntegracaoComPostgres]
	public async Task chave_da_compra_traz_apenas_os_itens_daquele_edital() {
		var repositorio = CriarRepositorio();

		var filtro = ItensDaCompraSpecifications.DaCompraDoOrgao(
			BancoDeItensParaTeste.CnpjDoOrgao,
			BancoDeItensParaTeste.AnoDaCompraDeMerenda,
			BancoDeItensParaTeste.SequencialDaCompraDeMerenda);

		var resultado = await repositorio.FilterWithCompraAndOrgao(filtro);

		resultado.IsSuccess.Should().BeTrue();
		resultado.Value.Select(x => x.Id).Should().BeEquivalentTo(new[] { 10L, 11L, 12L });
	}

	[FatoDeIntegracaoComPostgres]
	public async Task cnpj_com_mascara_encontra_a_mesma_compra() {
		var repositorio = CriarRepositorio();

		var filtro = ItensDaCompraSpecifications.DaCompraDoOrgao(
			"88.600.655/0001-41",
			BancoDeItensParaTeste.AnoDaCompraDeMerenda,
			BancoDeItensParaTeste.SequencialDaCompraDeMerenda);

		var resultado = await repositorio.FilterWithCompraAndOrgao(filtro);

		resultado.IsSuccess.Should().BeTrue();
		resultado.Value.Select(x => x.Id).Should().BeEquivalentTo(new[] { 10L, 11L, 12L });
	}

	[FatoDeIntegracaoComPostgres]
	public async Task chave_da_compra_carrega_compra_orgao_e_resultados() {
		var repositorio = CriarRepositorio();

		var filtro = ItensDaCompraSpecifications.DaCompraDoOrgao(
			BancoDeItensParaTeste.CnpjDoOrgao,
			BancoDeItensParaTeste.AnoDaCompraDeMerenda,
			BancoDeItensParaTeste.SequencialDaCompraDeMerenda);

		var resultado = await repositorio.FilterWithCompraAndOrgao(filtro);

		var arroz = resultado.Value.Single(x => x.Id == 10);
		arroz.Compra.Should().NotBeNull();
		arroz.Compra!.NumeroControlePncp.Should().Be("88600655000141-1-000316/2026");
		arroz.Compra.Orgao.Should().NotBeNull();
		arroz.Compra.Orgao!.Cnpj.Should().Be(BancoDeItensParaTeste.CnpjDoOrgao);
		arroz.Resultados.Should().ContainSingle();
	}

	[FatoDeIntegracaoComPostgres]
	public async Task descricao_filtra_dentro_do_edital() {
		var repositorio = CriarRepositorio();

		var filtro = ItensDaCompraSpecifications.DaCompraDoOrgao(
				BancoDeItensParaTeste.CnpjDoOrgao,
				BancoDeItensParaTeste.AnoDaCompraDeMerenda,
				BancoDeItensParaTeste.SequencialDaCompraDeMerenda)
			+ ItensDaCompraSpecifications.ComDescricaoContendo("CADEIRA");

		var resultado = await repositorio.FilterWithCompraAndOrgao(filtro);

		resultado.IsSuccess.Should().BeTrue();
		resultado.Value.Select(x => x.Id).Should().BeEquivalentTo(new[] { 12L });
	}

	[FatoDeIntegracaoComPostgres]
	public async Task objeto_da_compra_que_nao_bate_zera_o_edital() {
		var repositorio = CriarRepositorio();

		var filtro = ItensDaCompraSpecifications.DaCompraDoOrgao(
				BancoDeItensParaTeste.CnpjDoOrgao,
				BancoDeItensParaTeste.AnoDaCompraDeMerenda,
				BancoDeItensParaTeste.SequencialDaCompraDeMerenda)
			+ ItensDaCompraSpecifications.ComObjetoDaCompraContendo("informatica");

		var resultado = await repositorio.FilterWithCompraAndOrgao(filtro);

		resultado.IsSuccess.Should().BeTrue();
		resultado.Value.Should().BeEmpty();
	}

	private ItensDaCompraQueryRepository CriarRepositorio() {
		banco.Factory.Should().NotBeNull();
		return new ItensDaCompraQueryRepository(banco.Factory!);
	}
}
