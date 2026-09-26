# 🚀 Production-Ready Checklist
![.NET](https://img.shields.io/badge/.NET-8+-512BD4?logo=dotnet&logoColor=white) ![Azure](https://img.shields.io/badge/Azure-Production-0078D4?logo=microsoftazure&logoColor=white) ![Security](https://img.shields.io/badge/Security-OAuth2%2FOIDC-2E7D32) ![Tests](https://img.shields.io/badge/Tests-xUnit-512BD4)

## Architecture & Engineering
- [ ] Clean Architecture: Domain → Application → Infrastructure → API/Worker
- [ ] SOLID and dependency injection
- [ ] CQRS for command/query separation
- [ ] Idempotency and optimistic concurrency
- [ ] SQL + Outbox + Service Bus transactional workflow
- [ ] API versioning, health/readiness endpoints and correlation IDs

## Document Lifecycle
- [ ] Private Blob upload using short-lived SAS
- [ ] MIME/extension/size/malware validation
- [ ] SQL metadata and ACLs
- [ ] Service Bus ingestion events
- [ ] OCR/layout extraction
- [ ] Structure-aware chunking and lineage
- [ ] Embedding generation and vector indexing
- [ ] Retention, deletion and reprocessing

## RAG & Agentic AI
- [ ] Hybrid vector + keyword retrieval
- [ ] Tenant/ACL filtering before model context
- [ ] Reranking and token budgets
- [ ] Versioned prompts and grounded citations
- [ ] Agent tool allow-list, timeout and token budget
- [ ] Human approval for consequential tools
- [ ] MCP schema/auth/rate-limit enforcement
- [ ] Prompt-injection defenses and AI evaluation set

## Security
- [ ] Microsoft Entra ID/OIDC
- [ ] JWT issuer/audience/signature/expiry/scope validation
- [ ] Tenant and object-level authorization
- [ ] Managed Identity and Key Vault
- [ ] Private endpoints and encryption
- [ ] PII/secrets redaction
- [ ] Audit trail and least privilege

## Observability & Reliability
- [ ] OpenTelemetry traces and structured JSON logs
- [ ] Azure Monitor/Application Insights
- [ ] SLOs, error budgets and actionable alerts
- [ ] Retry with jitter, circuit breaker, bulkhead
- [ ] DLQ and poison-document quarantine

## Testing & Operations
- [ ] Unit, integration, contract and security tests
- [ ] RAG retrieval/groundedness evaluation
- [ ] Load and failure testing
- [ ] SAST/DAST/dependency/image scanning
- [ ] IaC, CI/CD, SBOM and rollback
- [ ] Disaster recovery and production runbook

> Production ready means code, security, tests, observability, deployment, failure handling and operations are version controlled and reviewed.