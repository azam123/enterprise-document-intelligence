using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
[ApiController,Route("api/v1/documents"),Authorize]
public sealed class DocumentUploadController(BlobServiceClient blobs,DocumentDbContext db,ICurrentUser user,IMessagePublisher bus,ILogger<DocumentUploadController> log):ControllerBase
{
 [HttpPost("upload")][RequestSizeLimit(524288000)][Authorize(Roles=Roles.Contributor+","+Roles.Administrator)]
 public async Task<IActionResult>Upload(IFormFile file,CancellationToken ct)
 {
  if(file is null||file.Length==0)return BadRequest("File is required");if(file.Length>524288000)return BadRequest("Maximum file size is 500 MB");
  var allowed=new[]{"application/pdf","text/plain","application/vnd.openxmlformats-officedocument.wordprocessingml.document","application/msword"};if(!allowed.Contains(file.ContentType,StringComparer.OrdinalIgnoreCase))return BadRequest("Unsupported document type");
  var safeFileName=Path.GetFileName(file.FileName);if(string.IsNullOrWhiteSpace(safeFileName))return BadRequest("Invalid file name");if(!FileSignatureValidator.IsAllowed(file,safeFileName))return BadRequest("File content does not match the declared document type");var d=new Document(user.TenantId,safeFileName,file.ContentType,file.Length);var container=blobs.GetBlobContainerClient("documents");await container.CreateIfNotExistsAsync(cancellationToken:ct);
  var versionNumber=1;var blob=container.GetBlobClient($"{user.TenantId}/{d.Id}/{versionNumber}/{Uri.EscapeDataString(safeFileName)}");
  await using var hashStream=file.OpenReadStream();var hash=await SHA256.HashDataAsync(hashStream,ct);
  await using var uploadStream=file.OpenReadStream();await blob.UploadAsync(uploadStream,overwrite:false,ct);
  var v=new DocumentVersion(d.Id,versionNumber,blob.Uri.ToString(),Convert.ToHexString(hash),file.Length);d.Versions.Add(v);d.SetCurrentVersion(v.Id);d.MarkProcessing();db.Documents.Add(d);await db.SaveChangesAsync(ct);
  var correlation=user.CorrelationId??Guid.NewGuid().ToString("N");
  await bus.PublishAsync(Topics.DocumentEvents,new DocumentUploaded(Guid.NewGuid(),d.TenantId,d.Id,v.Id,blob.Uri.ToString(),d.ContentType,d.SizeBytes,DateTimeOffset.UtcNow,correlation,[]),correlation,ct);
  await bus.PublishAsync(Topics.AuditEvents,new AuditRequested(Guid.NewGuid(),d.TenantId,user.UserId,"document.upload","Document",d.Id,"Succeeded",correlation,null),correlation,ct);
  log.LogInformation("Uploaded document {DocumentId} Blob={BlobUri}",d.Id,blob.Uri);return Accepted(new{documentId=d.Id,versionId=v.Id,status=d.Status});
 }
}