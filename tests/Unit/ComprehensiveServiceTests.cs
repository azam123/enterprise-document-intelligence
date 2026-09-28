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
 [Fact] public void ProcessingJob_Lifecycle_Works(){var j=new ProcessingJob(Guid.NewGuid(),"Extraction");j.Start();j.Start();j.Fail("x");Assert.Equal(2,j.Attempts);Assert.Equal("Failed",j.Status);Assert.Equal("x",j.Error);j.Complete();Assert.Equal("Completed",j.Status);}
 [Fact] public void SemanticChunker_Respects_Token_Limit_And_Overlap(){var c=new SemanticChunker();var chunks=c.Chunk("One two three. Four five six. Seven eight nine. Ten eleven twelve.",6,2);Assert.NotEmpty(chunks);Assert.All(chunks,x=>Assert.InRange(x.TokenCount,1,6));Assert.Equal(Enumerable.Range(0,chunks.Count),chunks.Select(x=>x.Number));}
 [Fact] public void SemanticChunker_Handles_Empty_And_Oversized_Input(){var c=new SemanticChunker();Assert.Empty(c.Chunk(""));Assert.Throws<ArgumentOutOfRangeException>(()=>c.Chunk("x",0));Assert.Throws<ArgumentOutOfRangeException>(()=>c.Chunk("x",5,5));var chunks=c.Chunk(string.Join(" ",Enumerable.Repeat("word",13)),5,0);Assert.Equal(3,chunks.Count);Assert.All(chunks,x=>Assert.InRange(x.TokenCount,1,5));}
 [Fact] public async Task DocumentExtractor_Reads_Text(){var e=new DocumentExtractor(new HttpClient(),new ConfigurationBuilder().Build());await using var s=new MemoryStream(Encoding.UTF8.GetBytes("hello world"));Assert.Equal("hello world",await e.ExtractAsync(s,"text/plain","a.txt",default));}
 [Fact] public async Task DocumentExtractor_Reads_Docx(){var e=new DocumentExtractor(new HttpClient(),new ConfigurationBuilder().Build());await using var s=new MemoryStream();using(var z=new ZipArchive(s,ZipArchiveMode.Create,true)){var entry=z.CreateEntry("word/document.xml");await using var w=entry.Open();var xml=XDocument.Parse("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>Hello</w:t></w:r></w:p><w:p><w:r><w:t>World</w:t></w:r></w:p></w:body></w:document>");xml.Save(w);}s.Position=0;var result=await e.ExtractAsync(s,"application/vnd.openxmlformats-officedocument.wordprocessingml.document","a.docx",default);Assert.Equal("Hello\nWorld",result);}
 [Fact] public async Task DocumentExtractor_Requires_DocumentIntelligence_For_Pdf(){var e=new DocumentExtractor(new HttpClient(),new ConfigurationBuilder().Build());await using var s=new MemoryStream(new byte[]{1,2});await Assert.ThrowsAsync<InvalidOperationException>(()=>e.ExtractAsync(s,"application/pdf","a.pdf",default));}
 [Fact] public void FileSignatureValidator_Validates_Pdf_And_Rejects_Mismatch(){var bytes=Encoding.UTF8.GetBytes("%PDF-1.7");var form=new FormFile(new MemoryStream(bytes),0,bytes.Length,"file","doc.pdf"){Headers=new HeaderDictionary(),ContentType="application/pdf"};Assert.True(FileSignatureValidator.IsAllowed(form,"doc.pdf"));var bad=new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("nope")),0,4,"file","doc.pdf"){Headers=new HeaderDictionary(),ContentType="application/pdf"};Assert.False(FileSignatureValidator.IsAllowed(bad,"doc.pdf"));}
 [Fact] public void FileSignatureValidator_Validates_All_Supported_Types(){var txt=new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("hello")),0,5,"file","a.txt"){Headers=new HeaderDictionary(),ContentType="text/plain"};Assert.True(FileSignatureValidator.IsAllowed(txt,"a.txt"));var docxBytes=new byte[]{0x50,0x4B,0x03,0x04};var docx=new FormFile(new MemoryStream(docxBytes),0,docxBytes.Length,"file","a.docx"){Headers=new HeaderDictionary(),ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document"};Assert.True(FileSignatureValidator.IsAllowed(docx,"a.docx"));var docBytes=new byte[]{0xD0,0xCF,0x11,0xE0,0xA1,0xB1,0x1A,0xE1};var doc=new FormFile(new MemoryStream(docBytes),0,docBytes.Length,"file","a.doc"){Headers=new HeaderDictionary(),ContentType="application/msword"};Assert.True(FileSignatureValidator.IsAllowed(doc,"a.doc"));var unsupported=new FormFile(new MemoryStream(new byte[]{1,2,3}),0,3,"file","a.exe"){Headers=new HeaderDictionary(),ContentType="application/octet-stream"};Assert.False(FileSignatureValidator.IsAllowed(unsupported,"a.exe"));}
 [Fact] public void SearchFilterBuilder_Contains_Tenant_And_Acl(){var tenant=Guid.NewGuid();var user=Guid.NewGuid();var f=SearchFilterBuilder.Build(tenant,user);Assert.Contains($"TenantId eq '{tenant}'",f);Assert.Contains(user.ToString(),f);Assert.Contains("AllowedPrincipalIds/any()",f);}
 [Fact] public void SearchFilterBuilder_Rejects_Empty_Identities(){Assert.Throws<ArgumentException>(()=>SearchFilterBuilder.Build(Guid.Empty,Guid.NewGuid()));Assert.Throws<ArgumentException>(()=>SearchFilterBuilder.Build(Guid.NewGuid(),Guid.Empty));}
 [Fact] public void Contracts_Preserve_Optional_Acl(){var t=Guid.NewGuid();var d=Guid.NewGuid();var p=Guid.NewGuid();var e=new DocumentProcessed(Guid.NewGuid(),t,d,Guid.NewGuid(),[],DateTimeOffset.UtcNow,"c",[p]);Assert.Contains(p,e.AllowedPrincipalIds!);}
 [Fact] public void AuditEvent_Can_Be_Idempotent_By_EventId(){var id=Guid.NewGuid();var e=new AuditEvent(Guid.NewGuid(),null,"read","Document",Guid.NewGuid(),"Succeeded",null,null,id);Assert.Equal(id,e.Id);}
 [Fact] public void ProcessingJob_Can_Complete(){var j=new ProcessingJob(Guid.NewGuid(),"Index");j.Start();j.Complete();Assert.Equal("Completed",j.Status);}
 [Fact] public void Document_And_Version_Cover_Failure_Transitions(){var d=new Document(Guid.NewGuid(),"a.txt","text/plain",1);Assert.Throws<ArgumentException>(()=>d.SetCurrentVersion(Guid.Empty));d.MarkFailed();Assert.Equal("Failed",d.Status);var v=new DocumentVersion(d.Id,1,"blob","sha",1);Assert.Throws<ArgumentException>(()=>v.SetStatus(" "));v.SetStatus("Ready");Assert.Equal("Ready",v.ProcessingStatus);}
 [Fact] public void Chunk_Can_Set_Vector_Id(){var c=new Chunk(Guid.NewGuid(),0,"text",1);Assert.Throws<ArgumentException>(()=>c.SetVectorId(" "));c.SetVectorId("vector-1");Assert.Equal("vector-1",c.VectorId);}
 [Fact] public void AuditEvent_Rejects_Missing_Required_Fields(){var tenant=Guid.NewGuid();Assert.Throws<ArgumentException>(()=>new AuditEvent(tenant,null,"","Document",null,"Succeeded",null,null));var valid = new AuditEvent(tenant,null,"read","Document",null,"Succeeded",null,null);
Assert.Equal("read", valid.Action);}
 [Fact] public void CurrentUser_Reads_Claims_And_Trace(){var tenant=Guid.NewGuid();var user=Guid.NewGuid();var context=new DefaultHttpContext();context.TraceIdentifier="corr-1";var claims=new[]{new Claim("tid",tenant.ToString()),new Claim("oid",user.ToString()),new Claim(ClaimTypes.Role,"Document.Reader")};context.User=new ClaimsPrincipal(new ClaimsIdentity(claims,"test"));var current=new CurrentUser(new HttpContextAccessor{HttpContext=context});Assert.True(current.IsAuthenticated);Assert.Equal(tenant,current.TenantId);Assert.Equal(user,current.UserId);Assert.True(current.IsInRole("Document.Reader"));Assert.Equal("corr-1",current.CorrelationId);}
 [Fact] public void CurrentUser_Returns_Empty_Ids_When_Claims_Are_Invalid(){var context=new DefaultHttpContext();context.User=new ClaimsPrincipal(new ClaimsIdentity());var current=new CurrentUser(new HttpContextAccessor{HttpContext=context});Assert.False(current.IsAuthenticated);Assert.Equal(Guid.Empty,current.TenantId);Assert.Equal(Guid.Empty,current.UserId);Assert.False(current.IsInRole("anything"));Assert.Equal(context.TraceIdentifier,current.CorrelationId);}
 [Fact] public void DocumentPolicies_Cover_Boundaries(){Assert.False(DocumentTypePolicy.IsAllowed("","a.pdf"));Assert.False(DocumentTypePolicy.IsAllowed("application/pdf",""));Assert.Throws<ArgumentException>(()=>DocumentTypePolicy.EnsureAllowed("text/plain","a.pdf"));Assert.Throws<ArgumentOutOfRangeException>(()=>DocumentSizePolicy.EnsureValid(DocumentSizePolicy.MaximumBytes+1));DocumentSizePolicy.EnsureValid(DocumentSizePolicy.MaximumBytes);Assert.Equal("report.pdf",new DocumentName(" report.pdf ").Value);Assert.Throws<ArgumentException>(()=>new DocumentName(new string('a',256)+".pdf"));Assert.Throws<ArgumentException>(()=>AclPolicy.EnsurePrincipal(Guid.Empty));AclPolicy.EnsurePermission("write");Assert.Throws<ArgumentException>(()=>AclPolicy.EnsurePermission("delete"));}
 [Fact] public void SemanticChunker_Covers_Normalization_And_Overlap(){var c=new SemanticChunker();var chunks=c.Chunk("one   two.\r\n\r\nthree four. five six.",4,1);Assert.True(chunks.Count>=2);Assert.Equal("one two. three four.", chunks[0].Text);
Assert.Equal(4, chunks[0].TokenCount);Assert.Equal(2,SemanticChunker.CountTokens(" one   two "));Assert.Throws<ArgumentOutOfRangeException>(()=>c.Chunk("x",1,-1));}
 [Fact] public async Task DocumentExtractor_Rejects_Unsupported_And_Null(){var e=new DocumentExtractor(new HttpClient(),new ConfigurationBuilder().Build());await Assert.ThrowsAsync<ArgumentNullException>(()=>e.ExtractAsync(null!,"text/plain","a.txt",default));await using var s=new MemoryStream(Encoding.UTF8.GetBytes("x"));await Assert.ThrowsAsync<NotSupportedException>(()=>e.ExtractAsync(s,"application/octet-stream","a.bin",default));}
 [Fact] public async Task DocumentExtractor_Rejects_Invalid_Docx(){var e=new DocumentExtractor(new HttpClient(),new ConfigurationBuilder().Build());await using var s=new MemoryStream();using(var z=new ZipArchive(s,ZipArchiveMode.Create,true)){var entry=z.CreateEntry("wrong.txt");await using var w=entry.Open();await w.WriteAsync("x"u8.ToArray());}s.Position=0;await Assert.ThrowsAsync<InvalidDataException>(()=>e.ExtractAsync(s,"application/vnd.openxmlformats-officedocument.wordprocessingml.document","bad.docx",default));}
 [Fact] public void Messaging_Contracts_Expose_Topics(){Assert.Equal("document-events",Topics.DocumentEvents);Assert.Equal("ingestion-events",Topics.IngestionEvents);Assert.Equal("processing-events",Topics.ProcessingEvents);Assert.Equal("embedding-events",Topics.EmbeddingEvents);Assert.Equal("indexing-events",Topics.IndexingEvents);Assert.Equal("audit-events",Topics.AuditEvents);}
 [Fact] public void SearchFilterBuilder_Escapes_Apostrophes(){var filter=SearchFilterBuilder.Build(Guid.NewGuid(),Guid.NewGuid());Assert.DoesNotContain("''",filter);Assert.Contains("AllowedPrincipalIds",filter);}


 [Fact] public void SearchQueryPolicy_Normalizes_And_Rejects_Invalid_Input(){Assert.Equal("hello",SearchQueryPolicy.NormalizeQuery("  hello "));Assert.Equal(10,SearchQueryPolicy.NormalizeTopK(0));Assert.Equal(50,SearchQueryPolicy.NormalizeTopK(100));Assert.Throws<ArgumentException>(()=>SearchQueryPolicy.NormalizeQuery(" "));Assert.Throws<ArgumentOutOfRangeException>(()=>SearchQueryPolicy.NormalizeTopK(-1));}

 [Fact] public void AgentRequestPolicy_Normalizes_And_Validates(){var p=new AgentRequestPolicy();Assert.Equal("hello",p.ValidatePrompt(" hello "));var n=p.Normalize(0,0);Assert.Equal(4,n.MaxTools);Assert.Equal(2048,n.MaxTokens);Assert.Throws<ArgumentException>(()=>p.ValidatePrompt(" "));Assert.Throws<ArgumentOutOfRangeException>(()=>p.Normalize(-1,10));Assert.Contains("document agent",p.BuildSystemPrompt());}

 [Fact] public void McpToolPolicy_Validates_Tools_And_Arguments(){McpToolPolicy.EnsureAllowed("document.search");Assert.Contains("document.get",McpToolPolicy.Tools);Assert.Equal(8,McpToolPolicy.NormalizeTopK(0));Assert.Equal(20,McpToolPolicy.NormalizeTopK(99));Assert.Throws<KeyNotFoundException>(()=>McpToolPolicy.EnsureAllowed("unknown"));using var doc=JsonDocument.Parse("{\"documentId\":\"00000000-0000-0000-0000-000000000001\"}");Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"),McpToolPolicy.ParseDocumentId(doc.RootElement));Assert.Throws<ArgumentException>(()=>McpToolPolicy.ParseDocumentId(JsonDocument.Parse("{\"documentId\":\"bad\"}").RootElement));}

 [Fact] public async Task EmbeddingBatchProcessor_Processes_And_Validates(){var p=new EmbeddingBatchProcessor();var id=Guid.NewGuid();var calls=0;var result=await p.ProcessAsync([new EmbeddingWorkItem(id,0,"hello",1)],(text,ct)=>{calls++;return Task.FromResult(new float[]{1,2,3});},CancellationToken.None);Assert.Single(result);Assert.Equal(1,calls);Assert.Equal(3,result[0].Vector.Length);await Assert.ThrowsAsync<ArgumentException>(()=>p.ProcessAsync([new EmbeddingWorkItem(Guid.Empty,0,"hello",1)],(_,_)=>Task.FromResult(new float[]{1}),CancellationToken.None));await Assert.ThrowsAsync<InvalidOperationException>(()=>p.ProcessAsync([new EmbeddingWorkItem(Guid.NewGuid(),0,"hello",1)],(_,_)=>Task.FromResult(Array.Empty<float>()),CancellationToken.None));}

 [Fact] public void IndexDocumentValidator_Enforces_Index_Contract(){var v=new IndexDocumentValidator();v.Validate(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),0,"text",[Guid.NewGuid()],[1f,2f]);Assert.Throws<ArgumentException>(()=>v.Validate(Guid.Empty,Guid.NewGuid(),Guid.NewGuid(),0,"text",[],[1f]));Assert.Throws<ArgumentException>(()=>v.Validate(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),0,"text",[Guid.Empty],[1f]));Assert.Throws<ArgumentException>(()=>v.Validate(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),0,"text",[],[float.NaN]));}

 [Fact]
public async Task AuditEventProcessor_Is_Idempotent()
{
    var store = new InMemoryAuditStore();
    var application = new AuditApplication(store);
    var processor = new EnterpriseDocumentIntelligence.AuditService.Application.AuditEventProcessor(application);
    var id = Guid.NewGuid();

    var request = new AuditRequested(
        id,
        Guid.NewGuid(),
        null,
        "read",
        "Document",
        Guid.NewGuid(),
        "Succeeded",
        "corr",
        null);

    Assert.True(await processor.RecordAsync(request));
    Assert.False(await processor.RecordAsync(request));

    var records = await application.QueryAsync(request.TenantId);

    Assert.Single(records);
    Assert.Equal(id, records[0].EventId);
}

[Fact]
public async Task SqlAuditStore_Persists_And_Queries_Tenant_Records()
{
    var options = new DbContextOptionsBuilder<DocumentDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var db = new DocumentDbContext(options);
    var store = new SqlAuditStore(db);
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    var record = new EnterpriseDocumentIntelligence.AuditService.Domain.AuditRecord(
        Guid.NewGuid(),
        tenantId,
        Guid.NewGuid(),
        "read",
        "Document",
        resourceId,
        "Succeeded",
        DateTimeOffset.UtcNow,
        "correlation-1",
        new Dictionary<string, string>
        {
            ["source"] = "unit-test"
        });

    Assert.True(await store.AppendAsync(record));
    Assert.True(await store.ExistsAsync(record.EventId));

    var results = await store.QueryAsync(
        tenantId,
        resourceId);

    var stored = Assert.Single(results);
    Assert.Equal(record.EventId, stored.EventId);
    Assert.Equal("unit-test", stored.Metadata["source"]);
}

[Fact]
public async Task AuditEventProcessor_Rejects_Invalid_Metadata()
{
    var store = new InMemoryAuditStore();
    var application = new AuditApplication(store);
    var processor = new EnterpriseDocumentIntelligence.AuditService.Application.AuditEventProcessor(application);

    var request = new AuditRequested(
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        "read",
        "Document",
        null,
        "Succeeded",
        null,
        "[]");

    await Assert.ThrowsAsync<ArgumentException>(
        () => processor.RecordAsync(request));
}


}
