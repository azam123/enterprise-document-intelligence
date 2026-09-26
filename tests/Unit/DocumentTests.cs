using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using Xunit;

public sealed class DocumentTests
{
    [Fact]
    public void Create_sets_document_metadata()
    {
        var tenantId = Guid.NewGuid();
        var document = new Document(tenantId, "contract.pdf", "application/pdf", 100);

        Assert.Equal(tenantId, document.TenantId);
        Assert.Equal("contract.pdf", document.Name);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(100, document.SizeBytes);
        Assert.Equal("Uploaded", document.Status);
    }

    [Fact]
    public void Document_can_be_marked_failed()
    {
        var document = new Document(Guid.NewGuid(), "contract.pdf", "application/pdf", 100);

        document.MarkProcessing();
        document.MarkFailed();

        Assert.Equal("Failed", document.Status);
    }
}