# DocumentService

Document ingestion entry point for the Enterprise Document Intelligence Platform.

## Layers

- Domain — document naming, supported file-type policy, size policy and ACL invariants.
- Application — document creation/upload/retrieval/ACL use cases and ports.
- Infrastructure — EF Core repository, Azure Blob Storage adapter and Service Bus event publisher.
- API — HTTP contracts, authorization, multipart upload validation and dependency injection.

## Upload flow

1. Authenticate the caller and resolve tenant context.
2. Validate filename, content type and size.
3. Validate the file signature for PDF/DOC/DOCX/TXT.
4. Upload to tenant-scoped Azure Blob Storage.
5. Persist the document and version metadata.
6. Publish DocumentUploaded.
7. Publish the audit event.

Blob paths are tenant/document/version scoped to prevent cross-tenant collisions.

## Local configuration

Use either Storage:ConnectionString or Storage:BlobServiceUri with DefaultAzureCredential.

The shared BuildingBlocks configuration supplies the database, authentication and Service Bus infrastructure.

## Verification

The repository CI builds the DocumentService project independently and runs the Unit test suite with coverage across the DocumentService API, Application, Domain and Infrastructure assemblies. The service-specific gate requires at least 90% line coverage.
