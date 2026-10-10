# SearchService

Authenticated ASP.NET Core API for vector/hybrid retrieval from Azure AI Search with tenant and allowed-principal filters, bounded query input and citation-bearing results.

## Endpoint
- `POST /api/v1/search` — body: `{ "query": "…", "topK": 10 }`

## Security
The request requires an authenticated tenant/user context. Search filters must be applied server-side; never accept tenant or principal identity from the request body. Review index ACL synchronization and query filters together when changing the schema.

## Configuration
- `AzureSearch:Endpoint`
- `AzureSearch:IndexName`
- `AzureOpenAI` endpoint/deployment settings for query embeddings
- Microsoft Entra ID settings

```bash
dotnet run --project src/SearchService/SearchService.Api/SearchService.Api.csproj
```
