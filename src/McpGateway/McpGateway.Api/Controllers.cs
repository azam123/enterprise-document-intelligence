using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.McpGateway.Api;

public sealed record McpToolRequest(string Tool, JsonElement Arguments);
public sealed record McpToolResponse(string Tool, string Status, object Result);

[ApiController]
[Route("api/v1/mcp")]
[Authorize]
public sealed class McpController(IMcpToolExecutor executor, ICurrentUser user) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, object> ToolDefinitions = new Dictionary<string, object>
    {
        ["document.search"] = new { name = "document.search", description = "Search authorized enterprise documents.", inputSchema = new { type = "object", properties = new { query = new { type = "string" }, topK = new { type = "integer" } }, required = new[] { "query" } } },
        ["document.get"] = new { name = "document.get", description = "Get one authorized document.", inputSchema = new { type = "object", properties = new { documentId = new { type = "string", format = "uuid" } }, required = new[] { "documentId" } } },
        ["document.audit"] = new { name = "document.audit", description = "Read tenant audit events.", inputSchema = new { type = "object", properties = new { from = new { type = "string", format = "date-time" } } } }
    };

    [HttpGet("tools")]
    public IActionResult Tools() => Ok(ToolDefinitions.Values);

    [HttpPost("tools/call")]
    public async Task<ActionResult<McpToolResponse>> Call(McpToolRequest request, CancellationToken ct)
    {
        if (!ToolDefinitions.ContainsKey(request.Tool)) return NotFound();
        if (request.Arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null || request.Arguments.GetRawText().Length > 32768)
            return BadRequest("Invalid or oversized arguments.");

        var result = await executor.ExecuteAsync(request.Tool, request.Arguments, ct);
        return Ok(new McpToolResponse(request.Tool, "Executed", result));
    }
}