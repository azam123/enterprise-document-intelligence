# Enterprise Document Intelligence Platform

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-Cloud-0078D4?logo=microsoftazure&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-Microservices-00897B)
![AI](https://img.shields.io/badge/AI-RAG%20%7C%20Agents-7B1FA2)
![License](https://img.shields.io/badge/License-GPL--3.0-blue)

A .NET 8 reference implementation for secure document ingestion, extraction, chunking, embeddings, Azure AI Search indexing, retrieval, agent workflows, MCP tool access, and audit events.

> **Status:** This repository is an evolving reference implementation. Several adapters and stores are development-oriented or in-memory; do not treat the repository as production-deployed until the deployment, persistence, identity, security, and operational checklists have been completed for your Azure environment.

## Services

| Service | Responsibility | Main technology |
|---|---|---|
| DocumentService | Document metadata, upload, versions, tenant-scoped access and ACL events | ASP.NET Core, SQL/EF Core, Blob Storage |
| IngestionService | Consume document events, extract text, store extracted output | Worker Service, Service Bus, Document Intelligence |
| ProcessingService | Normalize extracted text and create chunks | Worker Service, chunking |
| EmbeddingService | Generate vectors for processed chunks | Azure OpenAI integration, Service Bus |
| IndexingService | Provision Azure AI Search index and index chunks/ACLs | Azure AI Search |
| SearchService | Tenant- and principal-filtered hybrid/vector retrieval with citations | ASP.NET Core, Azure AI Search |
| AgentService | Apply agent request policy and execute bounded agent workflows | ASP.NET Core, configured model/tool boundary |
| McpGateway | Expose a policy-controlled MCP-style tool endpoint | ASP.NET Core |
| AuditService | Consume and persist/query audit events | Worker Service, SQL Server |
| BuildingBlocks | Shared domain entities, messaging contracts, security context, persistence and telemetry | .NET, EF Core, Azure Service Bus, OpenTelemetry |

## Architecture

```text
Client
  │
  ▼
DocumentService ──► Blob Storage
  │                     │
  └── document event ───┘
            │
            ▼
      IngestionService ──► extracted text
            │
            ▼
      ProcessingService ──► chunks
            │
            ▼
      EmbeddingService ──► vectors
            │
            ▼
      IndexingService ──► Azure AI Search
                               │
                               ▼
                        SearchService
                               │
                      AgentService / MCP
                               │
                               ▼
                         AuditService
```

Events are defined in `src/BuildingBlocks/Messaging/Contracts.cs`. Topic/subscription names and message contracts should remain backward compatible as services evolve.

## Repository layout

- `src/BuildingBlocks` — shared entities, database context, Service Bus publisher, security and telemetry.
- `src/*Service` — independently buildable API or worker projects.
- `tests/Unit` — cross-service unit tests and policy/contract tests.
- `tests/DocumentService` — focused DocumentService application tests.
- `docs/` — production-readiness checklist, HLD, API/data model, capacity/monitoring and setup guides.
- `.github/workflows/build.yml` — restore, Release build, test/coverage and publish-artifact workflow.

## Prerequisites

- .NET SDK 8.x
- Azure resources only for end-to-end execution: Azure Service Bus, Blob Storage, Azure AI Document Intelligence, Azure OpenAI and Azure AI Search
- SQL Server for the durable document/audit persistence paths
- An identity with least-privilege permissions or local development connection strings

## Build and test

From the repository root:

```bash
dotnet restore src/BuildingBlocks/BuildingBlocks.csproj
dotnet build src/BuildingBlocks/BuildingBlocks.csproj -c Release

# Build all service entry projects
for project in \
  src/BuildingBlocks/BuildingBlocks.csproj \
  src/DocumentService/DocumentService.Api/DocumentService.Api.csproj \
  src/IngestionService/IngestionService.Api/IngestionService.Api.csproj \
  src/ProcessingService/ProcessingService.Api/ProcessingService.Api.csproj \
  src/EmbeddingService/EmbeddingService.Api/EmbeddingService.Api.csproj \
  src/IndexingService/IndexingService.Api/IndexingService.Api.csproj \
  src/SearchService/SearchService.Api/SearchService.Api.csproj \
  src/AgentService/AgentService.Api/AgentService.Api.csproj \
  src/McpGateway/McpGateway.Api/McpGateway.Api.csproj \
  src/AuditService/AuditService.Api/AuditService.Api.csproj; do
  dotnet build "$project" -c Release
done

dotnet test tests/Unit/Unit.csproj -c Release
dotnet test tests/DocumentService/DocumentService.Tests.csproj -c Release
```

## Configuration

Each service has an `appsettings.json` for local defaults. Configure environment-specific values with environment variables or a secret store; never commit credentials.

Common settings include:

- `ConnectionStrings__SqlServer`
- `ServiceBus__FullyQualifiedNamespace` or `ServiceBus__ConnectionString`
- `Storage__BlobServiceUri` or `Storage__ConnectionString`
- `Ingestion__DocumentIntelligenceEndpoint` and `Ingestion__DocumentIntelligenceKey`
- `AzureOpenAI__Endpoint`, deployment/model settings and API credentials
- `AzureSearch__Endpoint`, `AzureSearch__IndexName` and development-only API key

Use Managed Identity / DefaultAzureCredential in Azure where possible. Placeholder tenant/client IDs in checked-in configuration are not deployable identity settings.

## Security and production readiness

- Authenticate API callers with Microsoft Entra ID and authorize roles/tenant context.
- Verify tenant and document ACL filters are applied before AI context is constructed.
- Keep secrets out of source control and logs.
- Configure dead-letter handling, retry limits, idempotency and poison-message alerting.
- Replace in-memory stores with durable implementations before multi-instance deployment.
- Add integration, contract, security, load and RAG evaluation tests.
- Validate migrations, private networking, managed identities, retention, backup/restore and disaster recovery.
- Review `docs/01-production-ready-checklist.md` before deployment.

## Documentation

- [Production readiness checklist](docs/01-production-ready-checklist.md)
- [HLD, data model, APIs, capacity and monitoring](docs/02-hld-data-api-capacity-monitoring.md)
- [Installation and Azure deployment](docs/03-installation-setup-azure-deployment.md)
- [Codebase and testing](docs/04-codebase-and-testing.md)
- [Implemented service code notes](docs/05-implemented-service-code.md)

## License

GPL-3.0. See [LICENSE](LICENSE).
