var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "edi-gateway", runtime = Environment.Version.ToString() }));
app.MapGet("/api/platform", () => Results.Ok(new { name = "Enterprise Document Intelligence", inference = "self-hosted", publicLlmApisEnabled = false }));
app.Run();
public partial class Program { }
