# ProcessingService

Background worker that consumes ingestion-completed events, reads extracted text, creates bounded chunks and publishes processing events for embedding.

## Main components
- `ExtractedTextReader` — reads extracted text from storage.
- `SemanticChunker` — normalizes text, chunks by token/character policy and tracks token counts.
- `Worker` — Service Bus event handling, retries and dead-lettering.

The current processing store is in-memory for local/test use. Configure Blob Storage and Service Bus; validate chunking parameters against downstream embedding limits.

```bash
dotnet run --project src/ProcessingService/ProcessingService.Api/ProcessingService.Api.csproj
```
