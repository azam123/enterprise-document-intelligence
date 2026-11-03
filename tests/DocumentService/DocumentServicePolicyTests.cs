using EnterpriseDocumentIntelligence.DocumentService.Domain;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DocumentServiceTests;

public sealed class DocumentServicePolicyTests
{
    [Fact]
    public void DocumentName_RejectsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new DocumentName("   "));
    }

    [Fact]
    public void DocumentName_RejectsNamesLongerThan255Characters()
    {
        Assert.Throws<ArgumentException>(() => new DocumentName(new string('a', 256) + ".pdf"));
    }

    [Fact]
    public void DocumentName_ToStringReturnsNormalizedValue()
    {
        var name = new DocumentName(" report.pdf ");
        Assert.Equal("report.pdf", name.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(DocumentSizePolicy.MaximumBytes)]
    public void DocumentSizePolicy_AcceptsBoundaryValues(long size)
    {
        DocumentSizePolicy.EnsureValid(size);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(DocumentSizePolicy.MaximumBytes + 1)]
    public void DocumentSizePolicy_RejectsInvalidValues(long size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentSizePolicy.EnsureValid(size));
    }

    [Theory]
    [InlineData("text/plain", "notes.txt")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "report.docx")]
    [InlineData("application/msword", "legacy.doc")]
    [InlineData("APPLICATION/PDF", "REPORT.PDF")]
    public void DocumentTypePolicy_AcceptsSupportedTypes(string contentType, string fileName)
    {
        Assert.True(DocumentTypePolicy.IsAllowed(contentType, fileName));
    }

    [Theory]
    [InlineData("", "notes.txt")]
    [InlineData("text/plain", "notes.pdf")]
    [InlineData("application/pdf", "")]
    [InlineData("application/unknown", "file.bin")]
    public void DocumentTypePolicy_RejectsUnsupportedTypes(string contentType, string fileName)
    {
        Assert.False(DocumentTypePolicy.IsAllowed(contentType, fileName));
        Assert.Throws<ArgumentException>(() => DocumentTypePolicy.EnsureAllowed(contentType, fileName));
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("Write")]
    [InlineData("read")]
    [InlineData("WRITE")]
    public void AclPolicy_AcceptsSupportedPermissions(string permission)
    {
        AclPolicy.EnsurePrincipal(Guid.NewGuid());
        AclPolicy.EnsurePermission(permission);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Delete")]
    public void AclPolicy_RejectsUnsupportedPermissions(string permission)
    {
        Assert.Throws<ArgumentException>(() => AclPolicy.EnsurePermission(permission));
    }

    [Fact]
    public void FileSignatureValidator_AcceptsDocxSignature()
    {
        Assert.True(IsAllowed([0x50, 0x4B, 0x03, 0x04], "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "file.docx"));
    }

    [Fact]
    public void FileSignatureValidator_RejectsDocxWithWrongExtension()
    {
        Assert.False(IsAllowed([0x50, 0x4B, 0x03, 0x04], "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "file.pdf"));
    }

    [Fact]
    public void FileSignatureValidator_RejectsDocxWithShortHeader()
    {
        Assert.False(IsAllowed([0x50], "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "file.docx"));
    }

    [Fact]
    public void FileSignatureValidator_AcceptsLegacyDocSignature()
    {
        Assert.True(IsAllowed([0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], "application/msword", "file.doc"));
    }

    [Fact]
    public void FileSignatureValidator_RejectsLegacyDocWithShortHeader()
    {
        Assert.False(IsAllowed([0xD0, 0xCF], "application/msword", "file.doc"));
    }

    [Fact]
    public void FileSignatureValidator_RejectsPdfWithShortHeader()
    {
        Assert.False(IsAllowed([0x25, 0x50], "application/pdf", "file.pdf"));
    }

    [Fact]
    public void FileSignatureValidator_RejectsUnsupportedContentType()
    {
        Assert.False(IsAllowed([0x01, 0x02, 0x03], "application/octet-stream", "file.bin"));
    }

    [Fact]
    public void FileSignatureValidator_RejectsPdfWithWrongExtension()
    {
        Assert.False(IsAllowed("%PDF-1.7"u8.ToArray(), "application/pdf", "file.txt"));
    }

    private static bool IsAllowed(byte[] bytes, string contentType, string fileName)
    {
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return EnterpriseDocumentIntelligence.DocumentService.Api.FileSignatureValidator.IsAllowed(file, fileName);
    }
}
