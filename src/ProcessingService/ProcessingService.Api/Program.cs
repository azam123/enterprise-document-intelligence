using EnterpriseDocumentIntelligence.ProcessingService.Infrastructure;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

var builder=Host.CreateApplicationBuilder(args);
builder.Services.AddBuildingBlocks(builder.Configuration,"ProcessingService");builder.Services.AddProcessingServiceApplication();
builder.Services.AddSingleton(sp=>{var c=sp.GetRequiredService<IConfiguration>();var uri=c["Storage:BlobServiceUri"]??throw new InvalidOperationException("Storage:BlobServiceUri missing");var key=c["Storage:ConnectionString"];return string.IsNullOrWhiteSpace(key)?new BlobServiceClient(new Uri(uri),new DefaultAzureCredential()):new BlobServiceClient(key);});
builder.Services.AddSingleton<ExtractedTextReader>();
builder.Services.AddSingleton<SemanticChunker>();
builder.Services.AddSingleton<ServiceBusClientFactory>();
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();