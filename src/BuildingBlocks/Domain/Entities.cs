namespace EnterpriseDocumentIntelligence.BuildingBlocks.Domain;

public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;
}

public sealed class Document : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string Status { get; private set; } = "Uploaded";
    public Guid CurrentVersionId { get; private set; }
    public List<DocumentVersion> Versions { get; private set; } = [];
    public List<DocumentAcl> Acls { get; private set; } = [];
    private Document() { }
    public Document(Guid tenantId, string name, string contentType, long size) { TenantId = tenantId; Name = name; ContentType = contentType; SizeBytes = size; }
    public void SetCurrentVersion(Guid id) { CurrentVersionId = id; Touch(); }
    public void MarkProcessing() { Status = "Processing"; Touch(); }
    public void MarkReady() { Status = "Ready"; Touch(); }
    public void MarkFailed() { Status = "Failed"; Touch(); }
    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public sealed class DocumentVersion : Entity
{
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string BlobUri { get; private set; } = null!;
    public string Sha256 { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string ProcessingStatus { get; private set; } = "Pending";
    public Document Document { get; private set; } = null!;
    private DocumentVersion() { }
    public DocumentVersion(Guid doc, int version, string blob, string sha, long size) { DocumentId = doc; VersionNumber = version; BlobUri = blob; Sha256 = sha; SizeBytes = size; }
    public void SetStatus(string status) { ProcessingStatus = status; UpdatedAt = DateTimeOffset.UtcNow; }
}

public sealed class Chunk : Entity
{
    public Guid DocumentVersionId { get; private set; }
    public int ChunkNumber { get; private set; }
    public string Text { get; private set; } = null!;
    public int TokenCount { get; private set; }
    public string? VectorId { get; private set; }
    private Chunk() { }
    public Chunk(Guid version, int number, string text, int tokens) { DocumentVersionId = version; ChunkNumber = number; Text = text; TokenCount = tokens; }
    public void SetVectorId(string id) { VectorId = id; UpdatedAt = DateTimeOffset.UtcNow; }
}

public sealed class DocumentAcl : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid PrincipalId { get; private set; }
    public string PrincipalType { get; private set; } = null!;
    public string Permission { get; private set; } = null!;
    private DocumentAcl() { }
    public DocumentAcl(Guid d, Guid p, string t, string permission) { DocumentId = d; PrincipalId = p; PrincipalType = t; Permission = permission; }
}

public sealed class ProcessingJob : Entity
{
    public Guid DocumentVersionId { get; private set; }
    public string JobType { get; private set; } = null!;
    public string Status { get; private set; } = "Queued";
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
    private ProcessingJob() { }
    public ProcessingJob(Guid version, string type) { DocumentVersionId = version; JobType = type; }
    public void Start() { Status = "Running"; Attempts++; }
    public void Complete() { Status = "Completed"; }
    public void Fail(string error) { Status = "Failed"; Error = error; }
}

public sealed class AuditEvent : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public Guid? ResourceId { get; private set; }
    public string Outcome { get; private set; } = null!;
    public string? CorrelationId { get; private set; }
    public string? MetadataJson { get; private set; }
    private AuditEvent() { }
    public AuditEvent(Guid tenant, Guid? actor, string action, string resource, Guid? resourceId, string outcome, string? correlation, string? metadata, Guid? id = null)
    {
        if (tenant == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenant));
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Resource type is required.", nameof(resource));
        Id = id ?? Guid.NewGuid();
        TenantId = tenant; ActorId = actor; Action = action; ResourceType = resource; ResourceId = resourceId;
        Outcome = outcome; CorrelationId = correlation; MetadataJson = metadata;
    }
}