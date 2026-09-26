var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/api/v1/agentservice/status", () => Results.Ok(new { service = "AgentService", status = "ready" }));
app.Run();
