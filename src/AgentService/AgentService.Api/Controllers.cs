using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.AgentService.Api;

public sealed record AgentRunRequest(string Prompt, int MaxToolCalls = 8, int MaxTokens = 1200);

[ApiController]
[Route("api/v1/agents")]
[Authorize(Roles = Roles.Reader + "," + Roles.Contributor + "," + Roles.Administrator)]
public sealed class AgentController(AgentHarness harness) : ControllerBase
{
    [HttpPost("runs")]
    public async Task<ActionResult<AgentResult>> Run(AgentRunRequest request, CancellationToken ct) =>
        Ok(await harness.RunAsync(request.Prompt, request.MaxToolCalls, request.MaxTokens, ct));
}