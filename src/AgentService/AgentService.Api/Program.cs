using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers();builder.Services.AddEndpointsApiExplorer();builder.Services.AddSwaggerGen();builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();builder.Services.AddHttpClient<AgentHarness>(c=>c.Timeout=TimeSpan.FromSeconds(90));
builder.Services.AddBuildingBlocks(builder.Configuration,"AgentService");
var app=builder.Build();app.UseSwagger();app.UseSwaggerUI();app.UseHttpsRedirection();app.UseBuildingBlocks();app.MapControllers();app.MapHealthChecks("/health/live");app.MapHealthChecks("/health/ready");app.Run();
