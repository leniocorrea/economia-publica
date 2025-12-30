using System;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(configuration => {
	configuration.RegisterServicesFromAssemblyContaining<ListOrgaos.Query>();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var elasticsearchUrl = builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";

builder.Services.AddDbContextFactory<EconomIAQueryDbContext>(options => {
	options.UseNpgsql(connectionString);
	options.UseLazyLoadingProxies();
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

app.MapGet("/", () => "EconomIA API");
app.MapOrgaosEndpoints();
app.MapItensDaCompraEndpoints();

app.Run();
