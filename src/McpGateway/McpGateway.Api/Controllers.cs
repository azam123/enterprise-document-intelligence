using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
public sealed record McpToolRequest(string Tool,JsonElement Arguments);
public sealed record McpToolResponse(string Tool,string Status,object Result);
[ApiController,Route("api/v1/mcp"),Authorize]
public sealed class McpController(IMcpToolExecutor executor,ICurrentUser user,ILogger<McpController> log):ControllerBase
{

 [HttpGet("tools")]public IActionResult Tools()=>Ok(McpToolPolicy.Tools.Select(x=>new{name=x,inputSchema=new{type="object"}}));
 [HttpPost("tools/call")]public async Task<ActionResult<McpToolResponse>>Call(McpToolRequest request,CancellationToken ct)
 {
  try{McpToolPolicy.EnsureAllowed(request.Tool);McpToolPolicy.ValidateArguments(request.Arguments);}catch((ArgumentException,KeyNotFoundException) ex){return BadRequest(ex.Message);}
  log.LogInformation("MCP tool={Tool} Tenant={TenantId} User={UserId}",request.Tool,user.TenantId,user.UserId);
  var result=await executor.ExecuteAsync(request.Tool,request.Arguments,ct);
  return Ok(new McpToolResponse(request.Tool,"Completed",result));
 }
}