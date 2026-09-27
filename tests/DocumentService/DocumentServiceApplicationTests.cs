using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using EnterpriseDocumentIntelligence.DocumentService.Application;
using EnterpriseDocumentIntelligence.DocumentService.Domain;
using EnterpriseDocumentIntelligence.DocumentService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DocumentServiceTests;

public sealed class DocumentServiceApplicationTests
{
    [Fact]
    public async Task CreateAsync_creates_document_and_audit()
    {
        var fixture = CreateFixture();
        var result = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("contract.pdf", "application/pdf", 100),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Uploaded", result.Status);
        Assert.NotEqual(Guid.Empty, result.CurrentVersionId);
        Assert.Equal(1, fixture.Events.Audits);
    }

    [Fact]
    public async Task CreateAsync_rejects_unauthenticated_user()
    {
        var fixture = CreateFixture(authenticated: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fixture.Service.CreateAsync(
                new CreateDocumentCommand("contract.pdf", "application/pdf", 100),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_extension()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.CreateAsync(
                new CreateDocumentCommand("contract.txt", "application/pdf", 100),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_rejects_zero_size()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            fixture.Service.CreateAsync(
                new CreateDocumentCommand("contract.pdf", "application/pdf", 0),
                CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_stores_document_and_publishes_events()
    {
        var fixture = CreateFixture();
        await using var content = new MemoryStream("hello"u8.ToArray());

        var result = await fixture.Service.UploadAsync(
            new UploadDocumentCommand(
                "notes.txt",
                "text/plain",
                content,
                content.Length),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.NotEqual(Guid.Empty, result.VersionId);
        Assert.Equal("Processing", result.Status);
        Assert.Equal(1, fixture.Storage.Uploads);
        Assert.Equal(1, fixture.Events.Uploads);
        Assert.Equal(1, fixture.Events.Audits);
    }

    [Fact]
    public async Task UploadAsync_rejects_oversized_file()
    {
        var fixture = CreateFixture();
        var size = DocumentSizePolicy.MaximumBytes + 1;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            fixture.Service.UploadAsync(
                new UploadDocumentCommand(
                    "notes.txt",
                    "text/plain",
                    Stream.Null,
                    size),
                CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_returns_only_current_tenant_document()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);

        var found = await fixture.Service.GetAsync(
            created.Id,
            CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_missing_document()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.GetAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAclAsync_adds_read_principal_and_publishes_change()
    {
        var fixture = CreateFixture();
        var document = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);

        var principal = Guid.NewGuid();

        await fixture.Service.AddAclAsync(
            new AddAclCommand(
                document.Id,
                principal,
                "User",
                "Read"),
            CancellationToken.None);

        var entity = await fixture.Repository.GetWithAclAsync(
            fixture.User.TenantId,
            document.Id,
            CancellationToken.None);

        Assert.NotNull(entity);
        Assert.Single(entity!.Acls);
        Assert.Equal(principal, entity.Acls[0].PrincipalId);
        Assert.Equal(1, fixture.Events.AclChanges);
    }

    [Fact]
    public async Task AddAclAsync_is_idempotent_for_same_permission()
    {
        var fixture = CreateFixture();
        var document = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);
        var principal = Guid.NewGuid();

        var command = new AddAclCommand(
            document.Id,
            principal,
            "User",
            "Read");

        await fixture.Service.AddAclAsync(command, CancellationToken.None);
        await fixture.Service.AddAclAsync(command, CancellationToken.None);

        var entity = await fixture.Repository.GetWithAclAsync(
            fixture.User.TenantId,
            document.Id,
            CancellationToken.None);

        Assert.Single(entity!.Acls);
    }

    [Fact]
    public async Task AddAclAsync_rejects_empty_principal()
    {
        var fixture = CreateFixture();
        var document = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.AddAclAsync(
                new AddAclCommand(
                    document.Id,
                    Guid.Empty,
                    "User",
                    "Read"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AddAclAsync_rejects_unknown_permission()
    {
        var fixture = CreateFixture();
        var document = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.AddAclAsync(
                new AddAclCommand(
                    document.Id,
                    Guid.NewGuid(),
                    "User",
                    "Delete"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AddAclAsync_rejects_missing_document()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.Service.AddAclAsync(
                new AddAclCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "User",
                    "Read"),
                CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAclAsync_removes_existing_acl()
    {
        var fixture = CreateFixture();
        var document = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);
        var principal = Guid.NewGuid();

        await fixture.Service.AddAclAsync(
            new AddAclCommand(document.Id, principal, "User", "Read"),
            CancellationToken.None);

        await fixture.Service.RemoveAclAsync(
            document.Id,
            principal,
            "Read",
            CancellationToken.None);

        var entity = await fixture.Repository.GetWithAclAsync(
            fixture.User.TenantId,
            document.Id,
            CancellationToken.None);

        Assert.Empty(entity!.Acls);
    }

    [Fact]
    public async Task RemoveAclAsync_is_noop_when_acl_does_not_exist()
    {
        var fixture = CreateFixture();
        var document = await fixture.Service.CreateAsync(
            new CreateDocumentCommand("a.pdf", "application/pdf", 10),
            CancellationToken.None);

        await fixture.Service.RemoveAclAsync(
            document.Id,
            Guid.NewGuid(),
            "Read",
            CancellationToken.None);

        Assert.Equal(0, fixture.Events.AclChanges);
    }

    [Fact]
    public void File_signature_accepts_plain_text()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "notes.txt")
        {
            ContentType = "text/plain"
        };

        Assert.True(
            EnterpriseDocumentIntelligence.DocumentService.Api.FileSignatureValidator.IsAllowed(
                file,
                "notes.txt"));
    }

    [Fact]
    public void File_signature_accepts_pdf()
    {
        using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "report.pdf")
        {
            ContentType = "application/pdf"
        };

        Assert.True(
            EnterpriseDocumentIntelligence.DocumentService.Api.FileSignatureValidator.IsAllowed(
                file,
                "report.pdf"));
    }

    [Fact]
    public void File_signature_rejects_invalid_pdf()
    {
        using var stream = new MemoryStream("not-a-pdf"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "report.pdf")
        {
            ContentType = "application/pdf"
        };

        Assert.False(
            EnterpriseDocumentIntelligence.DocumentService.Api.FileSignatureValidator.IsAllowed(
                file,
                "report.pdf"));
    }

    [Fact]
    public void File_signature_rejects_missing_file_name()
    {
        using var stream = new MemoryStream();
        var file = new FormFile(stream, 0, 0, "file", "notes.txt")
        {
            ContentType = "text/plain"
        };

        Assert.False(
            EnterpriseDocumentIntelligence.DocumentService.Api.FileSignatureValidator.IsAllowed(
                file,
                string.Empty));
    }

    [Fact]
    public void Domain_name_normalizes_whitespace()
    {
        var name = new DocumentName("  report.pdf  ");

        Assert.Equal("report.pdf", name.Value);
    }

    [Fact]
    public void Domain_name_rejects_path_traversal()
    {
        Assert.Throws<ArgumentException>(() =>
            new DocumentName("../report.pdf"));
    }

    [Fact]
    public void Domain_type_policy_rejects_mismatched_extension()
    {
        Assert.False(
            DocumentTypePolicy.IsAllowed(
                "application/pdf",
                "report.txt"));
    }

    [Fact]
    public void Domain_type_policy_accepts_supported_type()
    {
        Assert.True(
            DocumentTypePolicy.IsAllowed(
                "application/pdf",
                "report.pdf"));
    }

    private static Fixture CreateFixture(bool authenticated = true)
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new DocumentDbContext(options);
        var repository = new DocumentRepository(db);
        var user = new FakeCurrentUser(authenticated);
        var storage = new FakeStorage();
        var events = new FakeEvents();

        return new Fixture(
            new DocumentServiceApplication(
                repository,
                storage,
                events,
                user),
            repository,
            user,
            storage,
            events);
    }

    private sealed record Fixture(
        DocumentServiceApplication Service,
        DocumentRepository Repository,
        FakeCurrentUser User,
        FakeStorage Storage,
        FakeEvents Events);

    private sealed class FakeCurrentUser(bool authenticated) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid TenantId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => authenticated;
        public string? CorrelationId => "test-correlation";
        public bool IsInRole(string role) => true;
    }

    private sealed class FakeStorage : IDocumentStorage
    {
        public int Uploads { get; private set; }

        public async Task<StoredDocument> UploadAsync(
            Guid tenantId,
            Guid documentId,
            int versionNumber,
            string fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            Uploads++;
            using var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);

            return new StoredDocument(
                $"https://storage/{tenantId}/{documentId}/{versionNumber}/{fileName}",
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(memory.ToArray())),
                memory.Length);
        }
    }

    private sealed class FakeEvents : IDocumentEventPublisher
    {
        public int Uploads { get; private set; }
        public int AclChanges { get; private set; }
        public int Audits { get; private set; }

        public Task PublishUploadedAsync(
            Document document,
            DocumentVersion version,
            string correlationId,
            CancellationToken cancellationToken)
        {
            Uploads++;
            return Task.CompletedTask;
        }

        public Task PublishAclChangedAsync(
            Document document,
            IReadOnlyList<Guid> allowedPrincipalIds,
            string correlationId,
            CancellationToken cancellationToken)
        {
            AclChanges++;
            return Task.CompletedTask;
        }

        public Task PublishAuditAsync(
            Guid tenantId,
            Guid? actorId,
            string action,
            Guid? resourceId,
            string outcome,
            string correlationId,
            CancellationToken cancellationToken)
        {
            Audits++;
            return Task.CompletedTask;
        }
    }
}
