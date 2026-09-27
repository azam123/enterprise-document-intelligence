using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
public sealed record AgentRequest(string Prompt,int MaxTools=4,int MaxTokens=2048);
[ApiController,Route("api/v1/agent"),Authorize]
public sealed class AgentController(AgentHarness harness):ControllerBase
{
 [HttpPost("run")]public async Task<ActionResult<AgentResult>>Run(AgentRequest request,CancellationToken ct){try{return Ok(await harness.RunAsync(request.Prompt,request.MaxTools,request.MaxTokens,ct));}catch(ArgumentException ex){return BadRequest(ex.Message);}}
}
