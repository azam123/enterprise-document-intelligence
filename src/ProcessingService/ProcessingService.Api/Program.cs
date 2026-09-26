using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.ProcessingService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBuildingBlocks(builder.Configuration, "ProcessingService");
builder.Services.AddSingleton(sp =>
{
    var c = sp.GetRequiredService<IConfiguration>();
    var connection = c["Storage:ConnectionString"];
    var uri = c["Storage:BlobServiceUri"] ?? throw new InvalidOperationException("Storage:BlobServiceUri missing.");
    return string.IsNullOrWhiteSpace(connection) ? new BlobServiceClient(new Uri(uri), new DefaultAzureCredential()) : new BlobServiceClient(connection);
});
builder.Services.AddSingleton<SemanticChunker>();
builder.Services.AddSingleton<ServiceBusClientFactory>();
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();