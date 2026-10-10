# IndexingService

Background worker that provisions the Azure AI Search index, consumes embedding events and upserts chunks with tenant, document, citation, vector and allowed-principal fields. It also consumes ACL-change events to update indexed access lists.

## Important configuration
- `AzureSearch:Endpoint`
- `AzureSearch:IndexName` (defaults to `document-chunks`)
- `AzureSearch:ApiKey` for local development only; prefer Managed Identity in Azure
- `ServiceBus` namespace or connection string

The index schema's vector dimensions and field names must match the configured embedding deployment.

```bash
dotnet run --project src/IndexingService/IndexingService.Api/IndexingService.Api.csproj
```
