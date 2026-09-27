using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using EnterpriseDocumentIntelligence.DocumentService.Application;
using EnterpriseDocumentIntelligence.DocumentService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.DocumentService.Api;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentUploadController(
    DocumentServiceApplication service) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(DocumentSizePolicy.MaximumBytes)]
    [Authorize(Roles = Roles.Contributor + "," + Roles.Administrator)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("File is required.");

        var fileName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Invalid file name.");

        if (!FileSignatureValidator.IsAllowed(file, fileName))
            return BadRequest(
                "File content does not match the declared document type.");

        await using var stream = file.OpenReadStream();

        var result = await service.UploadAsync(
            new UploadDocumentCommand(
                fileName,
                file.ContentType,
                stream,
                file.Length),
            cancellationToken);

        return Accepted(result);
    }
}
