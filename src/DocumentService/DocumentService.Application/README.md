# DocumentService.Application

Application orchestration for document metadata, binary upload, tenant-scoped reads and ACL changes.

- `CreateDocumentCommand` — metadata-only creation.
- `UploadDocumentCommand` — upload bytes and create a storage-backed version.
- `DocumentServiceApplication` — coordinates validation, repository, storage and event publisher.
- `IDocumentRepository`, `IDocumentStorage`, `IDocumentEventPublisher` — infrastructure ports.

Keep application logic independent of Azure SDKs. Validate tenant context and metadata before persistence or event publication.
