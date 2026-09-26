# ☁️ Installation, Setup & Azure Deployment
![Azure](https://img.shields.io/badge/Azure-Deployment-0078D4?logo=microsoftazure&logoColor=white) ![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)

## Prerequisites
- .NET 8 SDK
- Docker
- Azure CLI
- Azure subscription
- Microsoft Entra tenant
- Azure SQL, Storage/ADLS Gen2, Service Bus, Key Vault
- Azure AI Foundry/model deployments
- Vector search capability

## Local Setup
```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet run --project src/DocumentService/DocumentService.Api
```

Use user secrets or environment variables locally. Never commit credentials.

## Azure Resources
Provision resource group, VNet/private endpoints, Azure SQL, Storage/ADLS, Service Bus, Key Vault, Application Insights/Azure Monitor, managed identities, container hosting and Azure AI Foundry resources.

## Authentication
Register APIs in Entra ID. Validate issuer, audience, signature, expiry and scopes. Use Managed Identity for Azure-to-Azure calls.

## AI Foundry
Keep model/deployment names in configuration. Put model access behind `IChatModel` and `IEmbeddingModel` so deployments can change without changing domain/application code.

## Deployment Flow
```text
PR → Build → Unit Tests → Integration Tests → SAST/Dependency Scan
   → Container Build → Image Scan/SBOM → Dev → Evaluation → Staging
   → Approval → Production Canary → SLO Verification
```

## Container
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY publish/ .
ENTRYPOINT ["dotnet", "DocumentService.Api.dll"]
```

## Production Configuration
- Externalize configuration.
- Use Key Vault references/secrets.
- Set explicit timeouts and cancellation tokens.
- Retry only transient failures.
- Use backward-compatible DB migrations.
- Deploy infrastructure before applications.
- Gate migrations and support rollback.

## Azure Data Lake + Databricks
Use raw/curated/evaluation zones. Land immutable source artifacts and processing outputs in ADLS. Use Databricks for large-scale ETL, document quality analysis, embedding evaluation and offline RAG datasets.

## Operational Readiness
Configure dashboards, alerts, DLQ runbooks, backup/restore, DR procedures, security incident response and cost budgets before production.