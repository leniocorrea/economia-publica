using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EconomIA.Adapters.Persistence;
using EconomIA.Adapters.Persistence.Repositories.Atas;
using EconomIA.Adapters.Tests.Integracao;
using EconomIA.Common.Domain;
using EconomIA.Common.Persistence.Pagination;
using EconomIA.Domain;
using EconomIA.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace EconomIA.Adapters.Tests.Persistence.Repositories.Atas;

file class FabricaDeContextoDeConsulta(DbContextOptions<EconomIAQueryDbContext> opcoes) : IDbContextFactory<EconomIAQueryDbContext> {
	public EconomIAQueryDbContext CreateDbContext() {
		return new EconomIAQueryDbContext(opcoes);
	}
}

public sealed class BancoDeAtasParaTeste : IAsyncLifetime {
	private readonly String nomeDoBanco = $"economia_testes_atas_{Guid.NewGuid():N}";

	public IDbContextFactory<EconomIAQueryDbContext>? Factory { get; private set; }

	public const String AtaDeMerendaComCompraDeAlimentacao = "11222333000144-1-000001/2026-000001";
	public const String AtaDeInformaticaComCompraDeEquipamentos = "11222333000144-1-000002/2026-000001";
	public const String AtaDeMerendaSemCompra = "11222333000144-1-000003/2026-000001";

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
		await Executar(conexao, "alter table public.ata add column if not exists possibilidade_adesao boolean;");

		await Executar(conexao, @"
			insert into public.orgao (identificador, cnpj, razao_social)
			values (1, '11222333000144', 'ORGAO DE TESTE');

			insert into public.compra (identificador, identificador_do_orgao, numero_controle_pncp, ano_compra, sequencial_compra, modalidade_identificador, objeto_compra) values
				(1, 1, '11222333000144-1-000001/2026', 2026, 1, 6, 'Contratacao de empresa para fornecimento de ALIMENTACAO escolar'),
				(2, 1, '11222333000144-1-000002/2026', 2026, 2, 6, 'Aquisicao de equipamentos de informatica');

			insert into public.ata (identificador_do_orgao, numero_controle_pncp_ata, numero_controle_pncp_compra, ano_ata, objeto_contratacao, cancelado, data_assinatura, vigencia_inicio, vigencia_fim, possibilidade_adesao) values
				(1, '11222333000144-1-000001/2026-000001', '11222333000144-1-000001/2026', 2026, 'Registro de precos para MERENDA escolar', false, '2026-08-10', '2026-08-10', '2099-12-31', true),
				(1, '11222333000144-1-000002/2026-000001', '11222333000144-1-000002/2026', 2026, 'Registro de precos para notebooks', false, '2026-08-11', '2026-08-11', '2099-12-31', true),
				(1, '11222333000144-1-000003/2026-000001', null, 2026, 'Registro de precos para merenda sem compra vinculada', false, '2026-08-12', '2026-08-12', '2099-12-31', true);
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

public class AtasQueryRepositoryIntegrationTests : IClassFixture<BancoDeAtasParaTeste> {
	private static readonly DateTime Inicio = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime Fim = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);

	private readonly BancoDeAtasParaTeste banco;

	public AtasQueryRepositoryIntegrationTests(BancoDeAtasParaTeste banco) {
		this.banco = banco;
	}

	private async Task<String[]> Buscar(Specification<Ata>? filtroAdicional = null, String? objetoDaCompra = null) {
		var repositorio = new AtasQueryRepository(banco.Factory!);
		var filtro = AtasSpecifications.ComDataDeReferenciaEntre(Inicio, Fim);

		if (filtroAdicional is not null) {
			filtro = filtro + filtroAdicional;
		}

		var paginacao = PaginationParameters.Create(null, null, 50).Value;
		var resultado = await repositorio.PaginarPorDataDeReferencia(filtro, paginacao, objetoDaCompra);

		resultado.IsSuccess.Should().BeTrue(resultado.IsFailure ? resultado.Error.Message : String.Empty);
		return resultado.Value.Items.Select(x => x.NumeroControlePncpAta).ToArray();
	}

	[FatoDeIntegracaoComPostgres]
	public async Task sem_filtro_textual_traz_todas_as_atas_do_periodo() {
		var atas = await Buscar();

		atas.Should().BeEquivalentTo(new[] {
			BancoDeAtasParaTeste.AtaDeMerendaComCompraDeAlimentacao,
			BancoDeAtasParaTeste.AtaDeInformaticaComCompraDeEquipamentos,
			BancoDeAtasParaTeste.AtaDeMerendaSemCompra
		});
	}

	[FatoDeIntegracaoComPostgres]
	public async Task filtro_por_objeto_da_ata_ignora_maiusculas() {
		var atas = await Buscar(AtasSpecifications.ComObjetoContratacaoContendo("MeReNdA"));

		atas.Should().BeEquivalentTo(new[] {
			BancoDeAtasParaTeste.AtaDeMerendaComCompraDeAlimentacao,
			BancoDeAtasParaTeste.AtaDeMerendaSemCompra
		});
	}

	[FatoDeIntegracaoComPostgres]
	public async Task filtro_por_objeto_da_compra_encontra_a_ata_pela_compra_vinculada() {
		var atas = await Buscar(objetoDaCompra: "alimentacao escolar");

		atas.Should().Equal(BancoDeAtasParaTeste.AtaDeMerendaComCompraDeAlimentacao);
	}

	[FatoDeIntegracaoComPostgres]
	public async Task ata_sem_compra_vinculada_fica_de_fora_do_filtro_por_objeto_da_compra() {
		var atas = await Buscar(objetoDaCompra: "merenda");

		atas.Should().BeEmpty();
	}

	[FatoDeIntegracaoComPostgres]
	public async Task objeto_da_ata_e_objeto_da_compra_se_combinam() {
		var atas = await Buscar(AtasSpecifications.ComObjetoContratacaoContendo("merenda"), "informatica");

		atas.Should().BeEmpty();
	}

	[FatoDeIntegracaoComPostgres]
	public async Task objeto_da_compra_em_branco_nao_filtra() {
		var atas = await Buscar(objetoDaCompra: "   ");

		atas.Should().HaveCount(3);
	}
}
