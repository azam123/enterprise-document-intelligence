# 🧠 Enterprise Document Intelligence Platform

![.NET](https://img.shields.io/badge/.NET-8+-512BD4?logo=dotnet&logoColor=white) ![Azure](https://img.shields.io/badge/Azure-Cloud-0078D4?logo=microsoftazure&logoColor=white) ![RAG](https://img.shields.io/badge/AI-RAG-FF8F00) ![Agentic AI](https://img.shields.io/badge/AI-Agentic-7B1FA2) ![MCP](https://img.shields.io/badge/MCP-Tooling-1565C0) ![CQRS](https://img.shields.io/badge/CQRS-Enabled-00897B) ![Tests](https://img.shields.io/badge/Tests-xUnit-512BD4)

> Production-oriented reference architecture for enterprise document ingestion, search, RAG and agentic workflows on Azure.

## ✨ What it provides

- Secure document upload and versioning
- Async ingestion with Azure Service Bus
- Azure Blob Storage + ADLS Gen2
- OCR/content extraction and structure-aware chunking
- Embeddings and vector indexing
- Hybrid RAG with authorization-aware retrieval
- Prompt engineering and grounded citations
- Agent harness with budgets, policies and tool controls
- MCP gateway for enterprise tools/resources
- Azure AI Foundry integration boundary
- SQL metadata and ACL model
- Databricks for large-scale processing/evaluation
- Clean Architecture, CQRS, DI and SOLID
- Structured logging, error handling and OpenTelemetry
- Unit/integration/security/RAG evaluation test strategy
- Azure deployment and CI/CD guidance

## 📚 Documentation

1. [Production Ready Checklist](docs/01-production-ready-checklist.md)
2. [HLD, Data Model, API, Capacity & Monitoring](docs/02-hld-data-api-capacity-monitoring.md)
3. [Installation, Setup & Azure Deployment](docs/03-installation-setup-azure-deployment.md)
4. [Codebase, Patterns, Error Handling & Tests](docs/04-codebase-and-testing.md)

## 🏛️ Core architecture

```text
Client → APIM/WAF → APIs → Service Bus → Workers
                         ↓             ↓
                     SQL/ACL       Blob/ADLS
                                      ↓
                              Extract → Chunk
                                      ↓
                                  Embedding
                                      ↓
                                Vector Index
                                      ↓
                            Hybrid RAG + Rerank
                                      ↓
                         Agent Harness / MCP
                                      ↓
                             Azure AI Foundry
```

## 🔐 Security model

Identity is based on Microsoft Entra ID/OIDC. Azure workload-to-workload access uses Managed Identity where possible. Authorization is evaluated at tenant, resource and document-ACL level before retrieval context is sent to an AI model.

## 🧪 Quality model

Production readiness is not defined by “the API works”. It requires tested failure modes, observable asynchronous workflows, secure authorization, idempotency, deployment automation, rollback procedures and measurable AI quality.

## 🗂️ Suggested complete source tree

```text
src/
├── BuildingBlocks/
│   ├── BuildingBlocks.Domain/
│   ├── BuildingBlocks.Application/
│   ├── BuildingBlocks.Infrastructure/
│   └── BuildingBlocks.Observability/
├── DocumentService/
├── IngestionService/
├── ProcessingService/
├── EmbeddingService/
├── IndexingService/
├── SearchService/
├── AgentService/
├── McpGateway/
└── AuditService/

tests/
├── Unit/
├── Integration/
├── Contract/
├── Security/
├── Load/
└── AiEvaluation/

infra/
├── bicep/
├── terraform/
└── policies/
```

## ⚠️ Important production note

This repository is a production-grade **reference scaffold and engineering blueprint**. Provider-specific Azure SDK calls, exact model names, vector schema, infrastructure SKU sizing and organization-specific compliance controls should be pinned to the target Azure subscription/environment before production rollout.
