# DocumentService

ASP.NET Core API for tenant-scoped document metadata, uploads, version records, ACL management and document lifecycle events.

## Responsibilities
- Validate file name, content type and size policy.
- Upload bytes to Azure Blob Storage through `IDocumentStorage`.
- Persist document/version metadata and current version.
- Publish document-upload, ACL-change and audit events.
- Expose health checks and Swagger endpoints.

## API
- `POST /api/v1/documents` — create a metadata record.
- `GET /api/v1/documents/{id}` — retrieve a tenant-scoped document.
- `POST /api/v1/documents/{id}/acl` — add a principal permission.
- `DELETE /api/v1/documents/{id}/acl/{principalId}?permission=Read` — remove a permission.

File upload is implemented by the dedicated `UploadController`; check its route and multipart request contract before integrating clients.

## Dependencies
`DocumentService.Application`, `DocumentService.Domain`, `DocumentService.Infrastructure`, `BuildingBlocks`, Azure Blob Storage and SQL Server.

## Run
```bash
dotnet run --project src/DocumentService/DocumentService.Api/DocumentService.Api.csproj
```

Configure SQL Server, Blob Storage and Microsoft Entra ID before using authenticated endpoints. Metadata-only creation and binary upload are separate paths; use the upload endpoint when a real file must be stored and processed.
