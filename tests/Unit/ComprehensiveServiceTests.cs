using EnterpriseDocumentIntelligence.IngestionService.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EnterpriseDocumentIntelligence.DocumentService.Api;
using EnterpriseDocumentIntelligence.AuditService.Application;
using EnterpriseDocumentIntelligence.AuditService.Infrastructure;
using EnterpriseDocumentIntelligence.DocumentService.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using System.Security.Claims;
using Xunit;

public sealed class ComprehensiveServiceTests
{
 [Fact] public void Document_Enforces_Invariants(){var tenant=Guid.NewGuid();Assert.Throws<ArgumentException>(()=>new Document(Guid.Empty,"a","text/plain",1));Assert.Throws<ArgumentException>(()=>new Document(tenant,"","text/plain",1));Assert.Throws<ArgumentOutOfRangeException>(()=>new Document(tenant,"a","text/plain",0));var d=new Document(tenant,"a","text/plain",2);var v=new DocumentVersion(d.Id,1,"blob","sha",2);d.Versions.Add(v);d.SetCurrentVersion(v.Id);d.MarkProcessing();d.MarkReady();Assert.Equal("Ready",d.Status);}
 [Fact] public void Domain_Entities_Validate_Arguments(){var d=Guid.NewGuid();var v=Guid.NewGuid();Assert.Throws<ArgumentException>(()=>new DocumentVersion(Guid.Empty,1,"","",1));Assert.Throws<ArgumentOutOfRangeException>(()=>new DocumentVersion(d,0,"","",1));Assert.Throws<ArgumentException>(()=>new Chunk(Guid.Empty,0,"x",1));Assert.Throws<ArgumentException>(()=>new Chunk(v,0,"",1));Assert.Throws<ArgumentOutOfRangeException>(()=>new Chunk(v,-1,"x",1));Assert.Throws<ArgumentOutOfRangeException>(()=>new Chunk(v,0,"x",0));Assert.Throws<ArgumentException>(()=>new DocumentAcl(Guid.Empty,d,"User","Read"));Assert.Throws<ArgumentException>(()=>new AuditEvent(Guid.Empty,null,"a","r",null,"Succeeded",null,null));}
 [Fact]