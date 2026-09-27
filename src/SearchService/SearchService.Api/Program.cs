using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.SearchService.Application;
using EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;
using EnterpriseDocumentIntelligence.SearchService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddBuildingBlocks(
    builder.Configuration,
    "SearchService");

builder.Services.AddHttpClient<IQueryEmbeddingService, AzureOpenAiQueryEmbeddingService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddSingleton<SearchClient>(
    serviceProvider =>
    {
        var configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        var endpoint = configuration["AzureSearch:Endpoint"]
            ?? throw new InvalidOperationException(
                "AzureSearch:Endpoint is missing.");

        var indexName = configuration["AzureSearch:IndexName"]
            ?? "document-chunks";

        var apiKey = configuration["AzureSearch:ApiKey"];

        return string.IsNullOrWhiteSpace(apiKey)
            ? new SearchClient(
                new Uri(endpoint),
                indexName,
                new DefaultAzureCredential())
            : new SearchClient(
                new Uri(endpoint),
                indexName,
                new AzureKeyCredential(apiKey));
    });

builder.Services.AddScoped<ISearchRepository, AzureSearchRepository>();
builder.Services.AddScoped<SearchService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseBuildingBlocks();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();