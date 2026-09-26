var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/api/v1/ingestionservice/status", () => Results.Ok(new { service = "IngestionService", status = "ready" }));
app.Run();
