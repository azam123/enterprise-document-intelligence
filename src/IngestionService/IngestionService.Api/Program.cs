builder.Services.AddIngestionServiceApplication();
using EnterpriseDocumentIntelligence.IngestionService.Infrastructure;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBuildingBlocks(builder.Configuration, "IngestionService");
builder.Services.AddHttpClient<DocumentExtractor>(c => c.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddSingleton<IDocumentExtractor>(sp => sp.GetRequiredService<DocumentExtractor>());
builder.Services.AddSingleton(sp =>
{
    var c = sp.GetRequiredService<IConfiguration>();
    var uri = c["Storage:BlobServiceUri"] ?? throw new InvalidOperationException("Storage:BlobServiceUri missing");
    var key = c["Storage:ConnectionString"];
    return string.IsNullOrWhiteSpace(key) ? new BlobServiceClient(new Uri(uri), new DefaultAzureCredential()) : new BlobServiceClient(key);
});
builder.Services.AddSingleton<BlobIngestionService>();
builder.Services.AddSingleton<ServiceBusClientFactory>();
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();
