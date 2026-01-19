using System.Data;
using EconomIA.CargaDeDados.Configuration;
using EconomIA.CargaDeDados.HealthChecks;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Observability;
using EconomIA.CargaDeDados.Repositories;
using EconomIA.CargaDeDados.Services;
using EconomIA.CargaDeDados.Workers;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;

namespace EconomIA.CargaDeDados;

public class Program {
	public static async Task Main(String[] args) {
		var modo = args.Length > 0 ? args[0].ToLower() : "worker";

		if (modo == "worker") {
			await ExecutarComoWorkerAsync(args);
		} else {
			await ExecutarComoCLIAsync(args);
		}
	}

	private static async Task ExecutarComoWorkerAsync(String[] args) {
		var builder = WebApplication.CreateBuilder(args);

		builder.Host.ConfigurarLogging(builder.Configuration);

		builder.Services.Configure<WorkerConfiguration>(builder.Configuration.GetSection(WorkerConfiguration.SectionName));
		builder.Services.Configure<SentryConfiguration>(builder.Configuration.GetSection(SentryConfiguration.SectionName));

		ConfigurarServicosComuns(builder.Services, builder.Configuration);

		builder.Services.AddHostedService<CargaDiariaWorker>();
		builder.Services.AddHostedService<SincronizacaoOrgaosWorker>();
		builder.Services.AddHostedService<ExecucaoManualWorker>();

		var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")!;
		builder.Services.AddHealthChecks()
			.AddCheck<WorkerHealthCheck>("worker", tags: new[] { "live", "ready" })
			.AddNpgSql(connectionString, name: "postgresql", tags: new[] { "ready" });

		var app = builder.Build();

		app.MapHealthCheckEndpoints();

		await app.RunAsync();
	}

	private static async Task ExecutarComoCLIAsync(String[] args) {
		var construtor = Host.CreateApplicationBuilder(args);

		construtor.Logging.ClearProviders();
		construtor.Logging.AddConsole();

		ConfigurarServicosComuns(construtor.Services, construtor.Configuration);

		using var host = construtor.Build();

		using var escopo = host.Services.CreateScope();
		var servicos = escopo.ServiceProvider;
		var logger = servicos.GetRequiredService<ILogger<Program>>();
		var execucoesCarga = servicos.GetRequiredService<ExecucoesCarga>();

		logger.LogInformation("=== EconomIA - Sistema de Carga de Dados PNCP ===");

		try {
			var comando = args[0].ToLower();
			var cnpjsFiltro = ObterCnpjsFiltro(args);
			var diasRetroativos = ObterDiasRetroativos(args);

			switch (comando) {
				case "orgaos":
					var metricsOrgaos = new MetricasExecucao();
					var execucaoOrgaos = await execucoesCarga.IniciarExecucaoAsync(ModoExecucao.Orgaos, TipoGatilho.Cli);

					try {
						var servicoCargaOrgaos = servicos.GetRequiredService<ServicoCargaOrgaos>();

						if (cnpjsFiltro is not null && cnpjsFiltro.Length > 0) {
							foreach (var cnpj in cnpjsFiltro) {
								await servicoCargaOrgaos.ImportarOrgaoPorCnpjAsync(cnpj);
							}
						} else {
							await servicoCargaOrgaos.CarregarTodosOrgaosEUnidadesAsync();
						}

						await execucoesCarga.FinalizarComSucessoAsync(execucaoOrgaos.Identificador, metricsOrgaos);
					} catch (Exception ex) {
						await execucoesCarga.FinalizarComErroAsync(execucaoOrgaos.Identificador, ex.Message, ex.StackTrace, metricsOrgaos);
						throw;
					}
					break;

				case "diaria":
					var execucaoAtivaDiaria = await execucoesCarga.ObterExecucaoEmAndamentoAsync();

					if (execucaoAtivaDiaria is not null) {
						logger.LogError(
							"Execucao em andamento detectada (ID: {ExecucaoId}, Modo: {Modo}, Inicio: {Inicio}). Aguarde a finalizacao antes de iniciar nova carga.",
							execucaoAtivaDiaria.Identificador,
							execucaoAtivaDiaria.ModoExecucao,
							execucaoAtivaDiaria.InicioEm);
						return;
					}

					var metricsDiaria = new MetricasExecucao();
					var execucaoDiaria = await execucoesCarga.IniciarExecucaoAsync(ModoExecucao.Diaria, TipoGatilho.Cli);

					try {
						var orquestrador = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
						await orquestrador.ExecutarImportacaoDiariaAsync(metricsDiaria, cnpjsFiltro, diasRetroativos);
						await execucoesCarga.FinalizarComSucessoAsync(execucaoDiaria.Identificador, metricsDiaria);
					} catch (Exception ex) {
						await execucoesCarga.FinalizarComErroAsync(execucaoDiaria.Identificador, ex.Message, ex.StackTrace, metricsDiaria);
						throw;
					}
					break;

				case "incremental":
					var execucaoAtivaIncremental = await execucoesCarga.ObterExecucaoEmAndamentoAsync();

					if (execucaoAtivaIncremental is not null) {
						logger.LogError(
							"Execucao em andamento detectada (ID: {ExecucaoId}, Modo: {Modo}, Inicio: {Inicio}). Aguarde a finalizacao antes de iniciar nova carga.",
							execucaoAtivaIncremental.Identificador,
							execucaoAtivaIncremental.ModoExecucao,
							execucaoAtivaIncremental.InicioEm);
						return;
					}

					var metricsIncremental = new MetricasExecucao();
					var execucaoIncremental = await execucoesCarga.IniciarExecucaoAsync(ModoExecucao.Incremental, TipoGatilho.Cli);

					try {
						var orquestradorIncremental = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
						await orquestradorIncremental.ExecutarImportacaoIncrementalAsync(metricsIncremental, cnpjsFiltro);
						await execucoesCarga.FinalizarComSucessoAsync(execucaoIncremental.Identificador, metricsIncremental);
					} catch (Exception ex) {
						await execucoesCarga.FinalizarComErroAsync(execucaoIncremental.Identificador, ex.Message, ex.StackTrace, metricsIncremental);
						throw;
					}
					break;

				case "status":
					var orquestradorStatus = servicos.GetRequiredService<ServicoOrquestradorImportacao>();
					await orquestradorStatus.ExibirStatusImportacaoAsync(cnpjsFiltro);
					break;

				case "brasil":
					var execucaoAtivaBrasil = await execucoesCarga.ObterExecucaoEmAndamentoAsync();

					if (execucaoAtivaBrasil is not null) {
						logger.LogError(
							"Execucao em andamento detectada (ID: {ExecucaoId}, Modo: {Modo}, Inicio: {Inicio}). Aguarde a finalizacao antes de iniciar nova carga.",
							execucaoAtivaBrasil.Identificador,
							execucaoAtivaBrasil.ModoExecucao,
							execucaoAtivaBrasil.InicioEm);
						return;
					}

					var metricsBrasil = new MetricasExecucao();
					var execucaoBrasil = await execucoesCarga.IniciarExecucaoAsync(ModoExecucao.Brasil, TipoGatilho.Cli);

					try {
						var servicoBrasil = servicos.GetRequiredService<ServicoCargaBrasil>();
						var dataFinalBrasil = DateTime.Now;
						var dataInicialBrasil = dataFinalBrasil.AddDays(-diasRetroativos);
						var apenasModalidadesComDados = !args.Any(a => a.ToLower() == "--todas-modalidades");

						logger.LogInformation(
							"Carga Brasil: {DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy}, Modalidades: {Modalidades}",
							dataInicialBrasil,
							dataFinalBrasil,
							apenasModalidadesComDados ? "apenas com dados (6,8,9,12)" : "todas (1-14)");

						var resultadoBrasil = await servicoBrasil.ProcessarCargaCompletaAsync(
							dataInicialBrasil,
							dataFinalBrasil,
							apenasModalidadesComDados);

						metricsBrasil.TotalComprasProcessadas = resultadoBrasil.ComprasProcessadas;
						metricsBrasil.TotalItensIndexados = resultadoBrasil.ItensIndexados;
						metricsBrasil.TotalContratosProcessados = resultadoBrasil.ContratosProcessados;
						metricsBrasil.TotalAtasProcessadas = resultadoBrasil.AtasProcessadas;

						await execucoesCarga.FinalizarComSucessoAsync(execucaoBrasil.Identificador, metricsBrasil);

						logger.LogInformation(
							"Carga Brasil finalizada. Compras: {Compras}, Itens: {Itens}, Contratos: {Contratos}, Atas: {Atas}, Duracao: {Duracao}ms",
							resultadoBrasil.ComprasProcessadas,
							resultadoBrasil.ItensIndexados,
							resultadoBrasil.ContratosProcessados,
							resultadoBrasil.AtasProcessadas,
							resultadoBrasil.DuracaoMs);
					} catch (Exception ex) {
						await execucoesCarga.FinalizarComErroAsync(execucaoBrasil.Identificador, ex.Message, ex.StackTrace, metricsBrasil);
						throw;
					}
					break;

				case "help":
				case "--help":
				case "-h":
					ExibirAjuda();
					break;

				default:
					logger.LogWarning("Comando desconhecido: {Comando}", comando);
					ExibirAjuda();
					break;
			}

			logger.LogInformation("Aplicacao finalizada com sucesso!");
		} catch (Exception ex) {
			logger.LogError(ex, "Ocorreu um erro fatal");
		}
	}

	private static void ConfigurarServicosComuns(IServiceCollection services, IConfiguration configuration) {
		var connectionString = configuration.GetConnectionString("PostgreSQL");
		services.AddTransient<IDbConnection>(sp => new NpgsqlConnection(connectionString));

		var elasticUri = configuration["ElasticSearch:Uri"] ?? "http://economia-elasticsearch:9200";
		var elasticSettings = new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(elasticUri))
			.DefaultIndex("itens-da-compra");
		services.AddSingleton(new Elastic.Clients.Elasticsearch.ElasticsearchClient(elasticSettings));

		services.AddHttpClient<ServicoCarga>(client => {
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

		services.AddTransient<Orgaos>();
		services.AddTransient<OrgaosMonitorados>();
		services.AddTransient<Unidades>();
		services.AddTransient<Compras>();
		services.AddTransient<ItensDaCompra>();
		services.AddTransient<ResultadosItens>();
		services.AddTransient<Contratos>();
		services.AddTransient<Atas>();
		services.AddTransient<ControlesImportacao>();
		services.AddTransient<ExecucoesCarga>();

		services.AddHttpClient<ServicoCargaOrgaos>()
			.AddTransientHttpErrorPolicy(builder => builder
				.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				.WaitAndRetryAsync(new[] {
					TimeSpan.FromMilliseconds(500),
					TimeSpan.FromSeconds(1),
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10)
				}));

		services.AddHttpClient<ServicoCargaContratosAtas>(client => {
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

		services.AddTransient<ServicoOrquestradorImportacao>();

		services.AddHttpClient<ServicoCargaBrasil>(client => {
				client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
				client.DefaultRequestHeaders.Add("Accept", "application/json");
			})
			.AddTransientHttpErrorPolicy(builder => builder
				.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				.WaitAndRetryAsync(new[] {
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10)
				}));

		var apiBaseUrl = configuration["Api:BaseUrl"];

		if (!String.IsNullOrEmpty(apiBaseUrl)) {
			services.AddHttpClient<IClienteDeNotificacoes, ClienteDeNotificacoes>(client => {
				client.BaseAddress = new Uri(apiBaseUrl);
			});
		} else {
			services.AddSingleton<IClienteDeNotificacoes, ClienteDeNotificacoesDesabilitado>();
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
  worker       Inicia o worker de carga automatica (padrao)
  orgaos       Carrega todos os orgaos e unidades do PNCP (~98k orgaos)
  diaria       Executa importacao diaria de orgaos monitorados
  incremental  Executa importacao incremental de orgaos monitorados
  brasil       Executa importacao de todo o Brasil (sem filtro de CNPJ)
  status       Exibe status de importacao dos orgaos monitorados

Opcoes:
  --cnpjs, -c <cnpjs>         Lista de CNPJs separados por virgula (filtra entre monitorados)
  --dias, -d <dias>           Dias retroativos para importacao (padrao: 1)
  --todas-modalidades         Processa todas as 14 modalidades (padrao: apenas 6,8,9,12)

Modo Brasil:
  - Busca dados de todo o Brasil sem filtrar por CNPJ
  - Rate limit de 70 requisicoes/segundo para evitar bloqueio
  - Por padrao processa apenas modalidades com dados (6, 8, 9, 12)
  - ~97% mais rapido que processar por orgao individual

Modo Worker:
  - Executa carga incremental automaticamente no horario configurado
  - Sincroniza orgaos/unidades semanalmente
  - Expoe endpoints de health check em /health

Nota: As importacoes (diaria, incremental) processam apenas orgaos monitorados.
      Use a API /v1/orgaos-monitorados/{cnpj} para ativar/desativar monitoramento.
      Primeira carga: ultimos 90 dias. Cargas subsequentes: incrementais.

Exemplos:
  dotnet run worker
  dotnet run orgaos
  dotnet run diaria --dias 7
  dotnet run diaria --cnpjs 17695032000151,18296681000142
  dotnet run brasil --dias 7
  dotnet run brasil --dias 30 --todas-modalidades
  dotnet run incremental
  dotnet run status --cnpjs 17695032000151
");
	}
}
