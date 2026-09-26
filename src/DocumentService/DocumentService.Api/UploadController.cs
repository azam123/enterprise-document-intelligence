using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.DocumentService.Api;

[ApiController]
[Route("api/v1/documents")]
[Authorize(Roles = Roles.Contributor + "," + Roles.Administrator)]
public sealed class UploadController(DocumentApplication app) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(500L * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var result = await app.UploadAsync(file, ct);
        return AcceptedAtAction(nameof(DocumentsController.Get), "Documents", new { id = result.Id }, result);
    }
}