using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using EconomIA.Adapters.Persistence;
using EconomIA.Adapters.Persistence.Repositories.Atas;
using EconomIA.Adapters.Persistence.Repositories.Contratos;
using EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;
using EconomIA.Adapters.Persistence.Repositories.Orgaos;
using EconomIA.Adapters.Persistence.Repositories.OrgaosMonitorados;
using EconomIA.Adapters.Persistence.Repositories.ExecucoesCarga;
using EconomIA.Adapters.Persistence.Repositories.ConfiguracoesCarga;
using EconomIA.Adapters.Persistence.Repositories.Usuarios;
using EconomIA.Adapters.Persistence.Repositories.Estatisticas;
using EconomIA.Application.Queries.ListOrgaos;
using EconomIA.Configuration;
using EconomIA.Domain.Repositories;
using EconomIA.Endpoints.Atas;
using EconomIA.Endpoints.Auth;
using EconomIA.Endpoints.ItensDaCompra;
using EconomIA.Endpoints.Orgaos;
using EconomIA.Endpoints.OrgaosMonitorados;
using EconomIA.Endpoints.Execucoes;
using EconomIA.Endpoints.Configuracao;
using EconomIA.Endpoints.Usuarios;
using EconomIA.Endpoints.Notificacoes;
using EconomIA.Endpoints.Dashboard;
using EconomIA.Hubs;
using EconomIA.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options => {
	options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
});

builder.Services.AddCors(options => {
	options.AddDefaultPolicy(policy => {
		policy.WithOrigins(
				"http://localhost:5173",
				"http://localhost:3000",
				"http://127.0.0.1:5173",
				"http://136.113.233.79",
				"https://136.113.233.79",
				"https://desenvolvimentoeconomiapublica.prefeiturasistemas.com.br"
			)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
	});
});

builder.Services.AddSignalR();

builder.Services.AddMediatR(configuration => {
	configuration.RegisterServicesFromAssemblyContaining<ListOrgaos.Query>();
});

builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection(JwtConfiguration.SectionName));
builder.Services.AddScoped<IServicoDeAutenticacao, ServicoDeAutenticacao>();
builder.Services.AddScoped<IServicoDeNotificacoes, ServicoDeNotificacoes>();

var jwtConfig = builder.Configuration.GetSection(JwtConfiguration.SectionName).Get<JwtConfiguration>() ?? new JwtConfiguration();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options => {
		options.TokenValidationParameters = new TokenValidationParameters {
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtConfig.Issuer,
			ValidAudience = jwtConfig.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
		};

		options.Events = new JwtBearerEvents {
			OnMessageReceived = context => {
				var accessToken = context.Request.Query["access_token"];

				var path = context.HttpContext.Request.Path;

				if (!String.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
					context.Token = accessToken;
				}

				return System.Threading.Tasks.Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var elasticsearchUrl = builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";

builder.Services.AddDbContextFactory<EconomIAQueryDbContext>(options => {
	options.UseNpgsql(connectionString);
});

builder.Services.AddDbContext<EconomIACommandDbContext>(options => {
	options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton(_ => {
	var settings = new ElasticsearchClientSettings(new Uri(elasticsearchUrl));
	return new ElasticsearchClient(settings);
});

builder.Services.AddScoped<IOrgaosReader, OrgaosQueryRepository>();
builder.Services.AddScoped<IItensDaCompraReader, ItensDaCompraQueryRepository>();
builder.Services.AddScoped<IItensDaCompraSearcher, ElasticsearchItemSearcher>();
builder.Services.AddScoped<IAtasReader, AtasQueryRepository>();
builder.Services.AddScoped<IContratosReader, ContratosQueryRepository>();
builder.Services.AddScoped<IOrgaosMonitoradosReader, OrgaosMonitoradosQueryRepository>();
builder.Services.AddScoped<IOrgaosMonitorados, OrgaosMonitoradosCommandRepository>();
builder.Services.AddScoped<IExecucoesCargaReader, ExecucoesCargaQueryRepository>();
builder.Services.AddScoped<IExecucoesCarga, ExecucoesCargaCommandRepository>();
builder.Services.AddScoped<IConfiguracoesCarga, ConfiguracoesCargaCommandRepository>();
builder.Services.AddScoped<IConfiguracoesCargaReader, ConfiguracoesCargaQueryRepository>();
builder.Services.AddScoped<IUsuariosReader, UsuariosQueryRepository>();
builder.Services.AddScoped<IUsuarios, UsuariosCommandRepository>();
builder.Services.AddScoped<IEstatisticas, EstatisticasQueryRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
	var dbContext = scope.ServiceProvider.GetRequiredService<EconomIACommandDbContext>();

	try {
		logger.LogInformation("Verificando migrations pendentes...");
		var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

		if (pendingMigrations.Count > 0) {
			logger.LogInformation("Aplicando {Count} migration(s): {Migrations}", pendingMigrations.Count, String.Join(", ", pendingMigrations));
			var timeoutDeMigracaoEmSegundos = 600;
			dbContext.Database.SetCommandTimeout(timeoutDeMigracaoEmSegundos);
			dbContext.Database.Migrate();
			logger.LogInformation("Migrations aplicadas com sucesso");
		} else {
			logger.LogInformation("Nenhuma migration pendente");
		}
	} catch (Exception ex) {
		logger.LogError(ex, "Erro ao aplicar migrations");
		throw;
	}
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "EconomIA API v1.0.11");
app.MapHub<NotificacoesHub>("/hubs/notificacoes");
app.MapAuthEndpoints();
app.MapUsuariosEndpoints();
app.MapOrgaosEndpoints();
app.MapItensDaCompraEndpoints();
app.MapAtasEndpoints();
app.MapOrgaosMonitoradosEndpoints();
app.MapExecucoesEndpoints();
app.MapConfiguracaoEndpoints();
app.MapNotificacoesEndpoints();
app.MapDashboardEndpoints();

app.Run();
