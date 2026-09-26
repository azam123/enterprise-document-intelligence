using EnterpriseDocumentIntelligence.AgentService;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient<AgentHarness>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient("SearchService", (sp, c) =>
{
    c.BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["Services:SearchServiceUrl"] ?? "http://searchservice:8080/");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddBuildingBlocks(builder.Configuration, "AgentService");
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseBuildingBlocks();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();