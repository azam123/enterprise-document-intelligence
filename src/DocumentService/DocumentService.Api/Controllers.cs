using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using EnterpriseDocumentIntelligence.DocumentService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.DocumentService.Api;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(
    DocumentServiceApplication service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Contributor + "," + Roles.Administrator)]
    public async Task<ActionResult<DocumentDto>> Create(
        CreateDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var document = await service.CreateAsync(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = document.Id },
            document);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Reader + "," + Roles.Contributor + "," + Roles.Administrator)]
    public async Task<ActionResult<DocumentDto>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await service.GetAsync(
            id,
            cancellationToken);

        return document is null
            ? NotFound()
            : Ok(document);
    }

    [HttpPost("{id:guid}/acl")]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Contributor)]
    public async Task<IActionResult> AddAcl(
        Guid id,
        AclRequest request,
        CancellationToken cancellationToken)
    {
        await service.AddAclAsync(
            new AddAclCommand(
                id,
                request.PrincipalId,
                request.PrincipalType,
                request.Permission),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}/acl/{principalId:guid}")]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Contributor)]
    public async Task<IActionResult> RemoveAcl(
        Guid id,
        Guid principalId,
        [FromQuery] string permission = "Read",
        CancellationToken cancellationToken = default)
    {
        await service.RemoveAclAsync(
            id,
            principalId,
            permission,
            cancellationToken);

        return NoContent();
    }
}

public sealed record AclRequest(
    Guid PrincipalId,
    string PrincipalType = "User",
    string Permission = "Read");
