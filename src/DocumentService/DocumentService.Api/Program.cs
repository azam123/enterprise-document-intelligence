using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.DocumentService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddBuildingBlocks(builder.Configuration, "DocumentService");
builder.Services.AddScoped<DocumentApplication>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connection = configuration["Storage:ConnectionString"];
    var uri = configuration["Storage:BlobServiceUri"] ?? throw new InvalidOperationException("Storage:BlobServiceUri missing.");
    return string.IsNullOrWhiteSpace(connection)
        ? new BlobServiceClient(new Uri(uri), new DefaultAzureCredential())
        : new BlobServiceClient(connection);
});
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseBuildingBlocks();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();