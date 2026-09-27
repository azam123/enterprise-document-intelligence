using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public sealed record CreateDocumentRequest(string Name,string ContentType,long SizeBytes);
public sealed record DocumentResponse(Guid Id,Guid TenantId,string Name,string ContentType,long SizeBytes,string Status,Guid CurrentVersionId);
public sealed record AclRequest(Guid PrincipalId,string PrincipalType="User",string Permission="Read");
public sealed class DocumentApp(DocumentDbContext db,ICurrentUser user,IMessagePublisher bus,ILogger<DocumentApp> log)
{
 public async Task<DocumentResponse>CreateAsync(CreateDocumentRequest r,CancellationToken ct)
 {
  if(user.TenantId==Guid.Empty)throw new UnauthorizedAccessException();
  if(string.IsNullOrWhiteSpace(r.Name))throw new ArgumentException("Name is required");
  if(r.SizeBytes<=0||r.SizeBytes>524288000)throw new ArgumentException("Invalid document size");
  var d=new Document(user.TenantId,r.Name,r.ContentType,r.SizeBytes);var v=new DocumentVersion(d.Id,1,"","",r.SizeBytes);d.Versions.Add(v);d.SetCurrentVersion(v.Id);db.Documents.Add(d);await db.SaveChangesAsync(ct);
  var cid=user.CorrelationId??Guid.NewGuid().ToString("N");
  await bus.PublishAsync(Topics.AuditEvents,new AuditRequested(Guid.NewGuid(),d.TenantId,user.UserId,"document.create","Document",d.Id,"Succeeded",cid,null),cid,ct);
  log.LogInformation("Created document {DocumentId} Tenant={TenantId}",d.Id,d.TenantId);return new(d.Id,d.TenantId,d.Name,d.ContentType,d.SizeBytes,d.Status,d.CurrentVersionId);
 }
 public async Task<DocumentResponse?>GetAsync(Guid id,CancellationToken ct){var d=await db.Documents.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.TenantId==user.TenantId,ct);return d is null?null:new(d.Id,d.TenantId,d.Name,d.ContentType,d.SizeBytes,d.Status,d.CurrentVersionId);}
 public async Task AddAclAsync(Guid documentId,AclRequest request,CancellationToken ct)
 {
  var d=await db.Documents.Include(x=>x.Acls).SingleOrDefaultAsync(x=>x.Id==documentId&&x.TenantId==user.TenantId,ct)??throw new KeyNotFoundException("Document not found");
  if(request.PrincipalId==Guid.Empty)throw new ArgumentException("PrincipalId is required");
  if(!d.Acls.Any(x=>x.PrincipalId==request.PrincipalId&&x.Permission==request.Permission))d.Acls.Add(new DocumentAcl(documentId,request.PrincipalId,request.PrincipalType,request.Permission));
  await db.SaveChangesAsync(ct);var principals=d.Acls.Where(x=>x.Permission=="Read").Select(x=>x.PrincipalId).Distinct().ToArray();var cid=user.CorrelationId??Guid.NewGuid().ToString("N");
  await bus.PublishAsync(Topics.DocumentEvents,new DocumentAclChanged(Guid.NewGuid(),user.TenantId,documentId,principals,DateTimeOffset.UtcNow,cid),cid,ct);
  await bus.PublishAsync(Topics.AuditEvents,new AuditRequested(Guid.NewGuid(),user.TenantId,user.UserId,"document.acl.add","Document",documentId,"Succeeded",cid,null),cid,ct);
 }
 public async Task RemoveAclAsync(Guid documentId,Guid principalId,string permission,CancellationToken ct)
 {
  var d=await db.Documents.Include(x=>x.Acls).SingleOrDefaultAsync(x=>x.Id==documentId&&x.TenantId==user.TenantId,ct)??throw new KeyNotFoundException("Document not found");
  var acl=d.Acls.FirstOrDefault(x=>x.PrincipalId==principalId&&x.Permission==permission);if(acl is null)return;db.DocumentAcls.Remove(acl);await db.SaveChangesAsync(ct);
  var principals=d.Acls.Where(x=>x.Permission=="Read"&&x.Id!=acl.Id).Select(x=>x.PrincipalId).Distinct().ToArray();var cid=user.CorrelationId??Guid.NewGuid().ToString("N");
  await bus.PublishAsync(Topics.DocumentEvents,new DocumentAclChanged(Guid.NewGuid(),user.TenantId,documentId,principals,DateTimeOffset.UtcNow,cid),cid,ct);
 }
}
[ApiController,Route("api/v1/documents"),Authorize]
public sealed class DocumentsController(DocumentApp app):ControllerBase
{
 [HttpPost][Authorize(Roles=Roles.Contributor+","+Roles.Administrator)]public async Task<ActionResult<DocumentResponse>>Create(CreateDocumentRequest r,CancellationToken ct)=>Ok(await app.CreateAsync(r,ct));
 [HttpGet("{id:guid}")][Authorize(Roles=Roles.Reader+","+Roles.Contributor+","+Roles.Administrator)]public async Task<ActionResult<DocumentResponse>>Get(Guid id,CancellationToken ct){var x=await app.GetAsync(id,ct);return x is null?NotFound():Ok(x);}
 [HttpPost("{id:guid}/acl")][Authorize(Roles=Roles.Administrator+","+Roles.Contributor)]public async Task<IActionResult>AddAcl(Guid id,AclRequest request,CancellationToken ct){await app.AddAclAsync(id,request,ct);return NoContent();}
 [HttpDelete("{id:guid}/acl/{principalId:guid}")][Authorize(Roles=Roles.Administrator+","+Roles.Contributor)]public async Task<IActionResult>RemoveAcl(Guid id,Guid principalId,[FromQuery]string permission="Read",CancellationToken ct=default){await app.RemoveAclAsync(id,principalId,permission,ct);return NoContent();}
}