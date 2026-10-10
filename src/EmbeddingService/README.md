# EmbeddingService

Worker that consumes processed document chunks, generates embeddings and publishes vector payloads for indexing.

## Flow
1. Consume `DocumentProcessed` from the processing topic.
2. Validate and embed each chunk.
3. Publish `EmbeddingsCreated` to the embedding topic.
4. Complete, retry or dead-letter the message based on processing outcome.

The API project includes an Azure OpenAI client and batch processor. The deterministic in-memory embedding generator is a development/test fallback, not a semantic production model. Configure the model deployment, endpoint, authentication and vector dimensions consistently with Azure AI Search.

```bash
dotnet run --project src/EmbeddingService/EmbeddingService.Api/EmbeddingService.Api.csproj
```
