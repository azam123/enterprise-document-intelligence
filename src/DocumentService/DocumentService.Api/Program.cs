var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/api/v1/documentservice/status", () => Results.Ok(new { service = "DocumentService", status = "ready" }));
app.Run();
