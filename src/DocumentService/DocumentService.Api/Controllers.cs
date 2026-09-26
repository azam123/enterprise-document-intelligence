using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using EnterpriseDocumentIntelligence.DocumentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.DocumentService.Api;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(DocumentApplication app) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Reader + "," + Roles.Contributor + "," + Roles.Administrator)]
    public async Task<ActionResult<DocumentResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await app.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Reader + "," + Roles.Contributor + "," + Roles.Administrator)]
    public async Task<ActionResult<IReadOnlyList<DocumentListItem>>> List(CancellationToken ct) =>
        Ok(await app.ListAsync(ct));
}