# 🏗️ HLD, Data Model, API, Capacity & Monitoring
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20CQRS-6A1B9A) ![RAG](https://img.shields.io/badge/AI-RAG%20%2B%20Agents-FF8F00) ![MCP](https://img.shields.io/badge/Tools-MCP-1565C0)

## High-Level Architecture
```text
Clients → APIM/WAF → Document/Search/Agent APIs
                     ↓
              Entra ID + Authorization
                     ↓
      SQL + Blob + Service Bus + ADLS
                     ↓
 Ingestion → Extraction → Chunking → Embedding
                     ↓
               Vector Index
                     ↓
          Hybrid RAG + Reranking
                     ↓
             Agent Harness
                     ↓
                MCP Gateway
                     ↓
             Azure AI Foundry
```

## Service Boundaries
| Service | Responsibility |
|---|---|
| DocumentService | metadata, versions, ACLs, upload orchestration |
| IngestionService | async validation and content extraction |
| ProcessingService | normalization, classification and chunking |
| EmbeddingService | batch embedding generation |
| IndexingService | vector and metadata indexing |
| SearchService | authorization-aware hybrid retrieval |
| AgentService | planning, RAG orchestration and tool use |
| McpGateway | authenticated allow-listed tools/resources |
| AuditService | immutable security and audit events |

## Data Model
```text
Tenant 1---* User
Tenant 1---* Document
Document 1---* DocumentVersion
DocumentVersion 1---* Chunk
Chunk 1---1 VectorRecord
Document 1---* DocumentAcl
DocumentVersion 1---* ProcessingJob
Tenant 1---* AuditEvent
```

Core SQL tables: Tenant, User, Document, DocumentVersion, DocumentAcl, Chunk, ProcessingJob, OutboxMessage, AuditEvent.

Recommended indexes: `(TenantId, Id)`, `(TenantId, CreatedAt)`, `(DocumentId, VersionNumber)`, `(DocumentId, PrincipalId)`, and job state/lease indexes.

## API Design
```text
POST   /api/v1/documents
GET    /api/v1/documents/{id}
POST   /api/v1/documents/{id}/versions
DELETE /api/v1/documents/{id}
POST   /api/v1/search
POST   /api/v1/agents/runs
GET    /api/v1/agents/runs/{id}
GET    /health/live
GET    /health/ready
```

Every command accepts an idempotency key. APIs return Problem Details for errors and propagate correlation IDs.

## RAG Pipeline
1. Authenticate user.
2. Resolve tenant and authorization context.
3. Apply ACL/security metadata filters.
4. Embed query.
5. Run vector + keyword retrieval.
6. Rerank authorized candidates.
7. Build bounded context with source citations.
8. Apply prompt policy.
9. Invoke model.
10. Validate grounding/citations and return response.

## Agent Harness
AgentPolicy controls allowed tools, maximum tool calls, token budget, timeout, data classifications and approval requirements. The harness records every tool invocation and refuses tools outside policy.

## MCP
MCP Gateway exposes narrowly scoped tools/resources. Validate authentication, tenant, authorization, JSON schema, rate limits, input size, downstream timeout and audit events before execution.

## Capacity Estimation
Example starting point: 10,000 documents/day, 50 MB average/document, 500k chunks/day, 100 search RPS peak.

Scale independently: API replicas from CPU/RPS; workers from Service Bus queue age; embedding workers from model throughput; search from latency/RPS; agent workers from concurrent runs and token rate limits.

Use Little's Law for queue sizing: `L = λW`. Track queue age and drain rate rather than only CPU.

## Monitoring
Platform metrics: request rate, p50/p95/p99 latency, 4xx/5xx, queue depth, oldest message age, DLQ count, DB DTU/CPU, storage errors.

AI metrics: retrieval hit rate, Recall@K, MRR/NDCG, groundedness, citation coverage, refusal accuracy, prompt-injection detection, tokens/request and cost/request.

Tracing must cross HTTP, Service Bus, SQL, vector search, model calls and MCP tool calls.