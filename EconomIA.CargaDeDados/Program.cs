using System.Data;
using EconomIA.CargaDeDados.Repositories;
using EconomIA.CargaDeDados.Services;
using Polly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EconomIA.CargaDeDados;

public class Program {
	public static async Task Main(String[] args) {
		Console.WriteLine("=== EconomIA - Sistema de Carga de Dados PNCP ===");
		Console.WriteLine();

		var construtor = Host.CreateApplicationBuilder(args);

		var connectionString = construtor.Configuration.GetConnectionString("PostgreSQL");
		construtor.Services.AddTransient<IDbConnection>(sp => new NpgsqlConnection(connectionString));

		var elasticUri = construtor.Configuration["ElasticSearch:Uri"] ?? "http://economia-elasticsearch:9200";
		var elasticSettings = new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(elasticUri))
			.DefaultIndex("itens-da-compra");
		construtor.Services.AddSingleton(new Elastic.Clients.Elasticsearch.ElasticsearchClient(elasticSettings));

		construtor.Services.AddHttpClient<ServicoCarga>(client => {
				client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
				client.DefaultRequestHeaders.Add("Accept", "application/json");
			})
			.AddTransientHttpErrorPolicy(builder => builder
				.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				.WaitAndRetryAsync(new[] {
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10),
					TimeSpan.FromSeconds(20),
					TimeSpan.FromMinutes(1)
				}));

		construtor.Services.AddTransient<Orgaos>();
		construtor.Services.AddTransient<OrgaosMonitorados>();
		construtor.Services.AddTransient<Unidades>();
		construtor.Services.AddTransient<Compras>();
		construtor.Services.AddTransient<ItensDaCompra>();
		construtor.Services.AddTransient<ResultadosItens>();
		construtor.Services.AddTransient<Contratos>();
		construtor.Services.AddTransient<Atas>();
		construtor.Services.AddTransient<ControlesImportacao>();

		construtor.Services.AddHttpClient<ServicoCargaOrgaos>()
			.AddTransientHttpErrorPolicy(builder => builder
				.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				.WaitAndRetryAsync(new[] {
					TimeSpan.FromMilliseconds(500),
					TimeSpan.FromSeconds(1),
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10)
				}));

		construtor.Services.AddHttpClient<ServicoCargaContratosAtas>(client => {
				client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
				client.DefaultRequestHeaders.Add("Accept", "application/json");
			})
			.AddTransientHttpErrorPolicy(builder => builder
				.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				.WaitAndRetryAsync(new[] {
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10),
					TimeSpan.FromSeconds(20),
					TimeSpan.FromMinutes(1)
				}));

		construtor.Services.AddTransient<ServicoOrquestradorImportacao>();

		using var host = construtor.Build();

		using var escopo = host.Services.CreateScope();
		var servicos = escopo.ServiceProvider;

		try {
			var comando = args.Length > 0 ? args[0].ToLower() : "diaria";
			var cnpjsFiltro = ObterCnpjsFiltro(args);
			var diasRetroativos = ObterDiasRetroativos(args);

			switch (comando) {
				case "orgaos":
					var servicoCargaOrgaos = servicos.GetRequiredService<ServicoCargaOrgaos>();
					if (cnpjsFiltro is not null && cnpjsFiltro.Length > 0) {
						foreach (var cnpj in cnpjsFiltro) {
							await servicoCargaOrgaos.ImportarOrgaoPorCnpjAsync(cnpj);
						}
					} else {
						await servicoCargaOrgaos.CarregarTodosOrgaosEUnidadesAsync();
					}
					break;

				case "diaria":
					var orquestrador = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
					await orquestrador.ExecutarImportacaoDiariaAsync(cnpjsFiltro, diasRetroativos);
					break;

				case "incremental":
					var orquestradorIncremental = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
					await orquestradorIncremental.ExecutarImportacaoIncrementalAsync(cnpjsFiltro);
					break;

				case "status":
					var orquestradorStatus = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
					await orquestradorStatus.ExibirStatusImportacaoAsync(cnpjsFiltro);
					break;

				case "help":
				case "--help":
				case "-h":
					ExibirAjuda();
					break;

				default:
					Console.WriteLine($"Comando desconhecido: {comando}");
					ExibirAjuda();
					break;
			}

			Console.WriteLine("\nAplicacao finalizada com sucesso!");
		} catch (Exception ex) {
			Console.WriteLine($"Ocorreu um erro fatal: {ex.Message}");
			Console.WriteLine(ex.StackTrace);
		}
	}

	private static String[]? ObterCnpjsFiltro(String[] args) {
		var cnpjIndex = Array.FindIndex(args, a => a.ToLower() == "--cnpjs" || a.ToLower() == "-c");

		if (cnpjIndex >= 0 && cnpjIndex + 1 < args.Length) {
			return args[cnpjIndex + 1].Split(',', StringSplitOptions.RemoveEmptyEntries);
		}

		return null;
	}

	private static Int32 ObterDiasRetroativos(String[] args) {
		var diasIndex = Array.FindIndex(args, a => a.ToLower() == "--dias" || a.ToLower() == "-d");

		if (diasIndex >= 0 && diasIndex + 1 < args.Length) {
			if (Int32.TryParse(args[diasIndex + 1], out var dias)) {
				return dias;
			}
		}

		return 1;
	}

	private static void ExibirAjuda() {
		Console.WriteLine(@"
Uso: dotnet run [comando] [opcoes]

Comandos:
  orgaos       Carrega todos os orgaos e unidades do PNCP (~98k orgaos)
  diaria       Executa importacao diaria de orgaos monitorados (padrao)
  incremental  Executa importacao incremental de orgaos monitorados
  status       Exibe status de importacao dos orgaos monitorados

Opcoes:
  --cnpjs, -c <cnpjs>   Lista de CNPJs separados por virgula (filtra entre monitorados)
  --dias, -d <dias>     Dias retroativos para importacao diaria (padrao: 1)

Nota: As importacoes (diaria, incremental) processam apenas orgaos monitorados.
      Use a API /v1/orgaos-monitorados/{cnpj} para ativar/desativar monitoramento.
      Primeira carga: ultimos 90 dias. Cargas subsequentes: incrementais.

Exemplos:
  dotnet run orgaos
  dotnet run diaria --dias 7
  dotnet run diaria --cnpjs 17695032000151,18296681000142
  dotnet run incremental
  dotnet run status --cnpjs 17695032000151
");
	}
}
