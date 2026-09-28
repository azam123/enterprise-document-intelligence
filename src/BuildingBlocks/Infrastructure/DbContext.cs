using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Entity Framework Core context used by services that persist document-platform data.
/// </summary>
/// <param name="options">The configured context options.</param>
public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// Gets the documents table.
    /// </summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    /// Gets the document versions table.
    /// </summary>
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    /// <summary>
    /// Gets the chunks table.
    /// </summary>
    public DbSet<Chunk> Chunks => Set<Chunk>();

    /// <summary>
    /// Gets the document ACL table.
    /// </summary>
    public DbSet<DocumentAcl> DocumentAcls => Set<DocumentAcl>();

    /// <summary>
    /// Gets the processing jobs table.
    /// </summary>
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    /// <summary>
    /// Gets the audit events table.
    /// </summary>
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    /// <summary>
    /// Configures shared entity mappings, constraints and indexes.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("edi");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(260)
                .IsRequired();

            entity.Property(x => x.ContentType)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.Name });

            entity.HasMany(x => x.Versions)
                .WithOne(x => x.Document)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Acls)
                .WithOne()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("DocumentVersions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.BlobUri)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(x => x.Sha256)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.ProcessingStatus)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(x => new { x.DocumentId, x.VersionNumber })
                .IsUnique();
        });

        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.ToTable("Chunks");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Text)
                .IsRequired();

            entity.Property(x => x.VectorId)
                .HasMaxLength(255);

            entity.HasIndex(x => new { x.DocumentVersionId, x.ChunkNumber })
                .IsUnique();
        });

        modelBuilder.Entity<DocumentAcl>(entity =>
        {
            entity.ToTable("DocumentAcls");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PrincipalType)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Permission)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(x => new { x.DocumentId, x.PrincipalId, x.Permission })
                .IsUnique();
        });

        modelBuilder.Entity<ProcessingJob>(entity =>
        {
            entity.ToTable("ProcessingJobs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.JobType)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Error)
                .HasMaxLength(4000);

            entity.HasIndex(x => new { x.DocumentVersionId, x.JobType, x.Status });
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AuditEvents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.ResourceType)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.Outcome)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.CorrelationId)
                .HasMaxLength(128);

            entity.Property(x => x.MetadataJson);

            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasIndex(x => new { x.TenantId, x.ResourceType, x.ResourceId });
        });
    }
}