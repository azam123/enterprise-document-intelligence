using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.SearchService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient<AzureOpenAiEmbeddingClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddBuildingBlocks(builder.Configuration, "SearchService");
builder.Services.AddSearchApplication();
builder.Services.AddSingleton(sp =>
{
    var c = sp.GetRequiredService<IConfiguration>();
    var endpoint = c["AzureSearch:Endpoint"] ?? throw new InvalidOperationException("AzureSearch:Endpoint missing");
    var index = c["AzureSearch:IndexName"] ?? "document-chunks";
    var key = c["AzureSearch:ApiKey"];
    return string.IsNullOrWhiteSpace(key)
        ? new SearchClient(new Uri(endpoint), index, new DefaultAzureCredential())
        : new SearchClient(new Uri(endpoint), index, new AzureKeyCredential(key));
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
