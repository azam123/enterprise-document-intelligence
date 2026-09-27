using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.DocumentService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddBuildingBlocks(
    builder.Configuration,
    "DocumentService");

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["Storage:ConnectionString"];

    if (!string.IsNullOrWhiteSpace(connectionString))
        return new BlobServiceClient(connectionString);

    var serviceUri = configuration["Storage:BlobServiceUri"]
        ?? throw new InvalidOperationException(
            "Storage:BlobServiceUri is required when a connection string is not configured.");

    return new BlobServiceClient(
        new Uri(serviceUri),
        new DefaultAzureCredential());
});

builder.Services.AddDocumentService();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseBuildingBlocks();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
