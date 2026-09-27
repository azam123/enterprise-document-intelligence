using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
public sealed record McpToolRequest(string Tool,JsonElement Arguments);
public sealed record McpToolResponse(string Tool,string Status,object Result);
[ApiController,Route("api/v1/mcp"),Authorize]
public sealed class McpController(IMcpToolExecutor executor,ICurrentUser user,ILogger<McpController> log):ControllerBase
{
 private static readonly HashSet<string> Allowed=["document.search","document.get","document.audit"];
 [HttpGet("tools")]public IActionResult Tools()=>Ok(Allowed.Select(x=>new{name=x,inputSchema=new{type="object"}}));
 [HttpPost("tools/call")]public async Task<ActionResult<McpToolResponse>>Call(McpToolRequest request,CancellationToken ct)
 {
  if(!Allowed.Contains(request.Tool))return NotFound();
  if(request.Arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null||request.Arguments.GetRawText().Length>32768)return BadRequest("Invalid or oversized arguments.");
  log.LogInformation("MCP tool={Tool} Tenant={TenantId} User={UserId}",request.Tool,user.TenantId,user.UserId);
  var result=await executor.ExecuteAsync(request.Tool,request.Arguments,ct);
  return Ok(new McpToolResponse(request.Tool,"Completed",result));
 }
}