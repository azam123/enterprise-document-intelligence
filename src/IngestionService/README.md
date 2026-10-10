# IngestionService

Background worker that consumes document-upload events, records ingestion job state, extracts document text and writes the extracted representation to Blob Storage before publishing a completion event.

## Supported extraction paths
- Plain text and Markdown: local stream extraction.
- DOCX: paragraph text extraction from the Open XML package.
- PDF and legacy DOC: Azure AI Document Intelligence prebuilt-read endpoint.

## Configuration
- `Storage:BlobServiceUri` or `Storage:ConnectionString`
- `Ingestion:DocumentIntelligenceEndpoint`
- `Ingestion:DocumentIntelligenceKey`
- `ServiceBus:FullyQualifiedNamespace` or `ServiceBus:ConnectionString`

The job store is currently in-memory and must be replaced with durable persistence for production/restart-safe idempotency.

```bash
dotnet run --project src/IngestionService/IngestionService.Api/IngestionService.Api.csproj
```
