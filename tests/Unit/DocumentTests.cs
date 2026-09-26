using DocumentService.Domain;
using Xunit;

public sealed class DocumentTests
{
    [Fact]
    public void Create_rejects_empty_name()
    {
        Assert.Throws<DomainException>(() => Document.Create(Guid.NewGuid(), Guid.NewGuid(), "", "application/pdf", 100));
    }
}
