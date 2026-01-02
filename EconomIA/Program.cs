using System;
using System.Linq;
using System.Text.Json;
using EconomIA.Adapters.Persistence;
using EconomIA.Adapters.Persistence.Repositories.Atas;
using EconomIA.Adapters.Persistence.Repositories.Contratos;
using EconomIA.Adapters.Persistence.Repositories.ItensDaCompra;
using EconomIA.Adapters.Persistence.Repositories.Orgaos;
using EconomIA.Application.Queries.ListOrgaos;
using EconomIA.Domain.Repositories;
using EconomIA.Endpoints.ItensDaCompra;
using EconomIA.Endpoints.Orgaos;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options => {
	options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
});

builder.Services.AddMediatR(configuration => {
	configuration.RegisterServicesFromAssemblyContaining<ListOrgaos.Query>();
});

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

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
	var dbContext = scope.ServiceProvider.GetRequiredService<EconomIACommandDbContext>();

	try {
		logger.LogInformation("Verificando migrations pendentes...");
		var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

		if (pendingMigrations.Count > 0) {
			logger.LogInformation("Aplicando {Count} migration(s): {Migrations}", pendingMigrations.Count, String.Join(", ", pendingMigrations));
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

app.MapGet("/", () => "EconomIA API v1.0.3");
app.MapOrgaosEndpoints();
app.MapItensDaCompraEndpoints();

app.Run();
