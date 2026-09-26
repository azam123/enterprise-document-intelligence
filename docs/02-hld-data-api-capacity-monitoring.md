# 🏗️ HLD, Data Model, API, Capacity & Monitoring
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20CQRS-6A1B9A) ![RAG](https://img.shields.io/badge/AI-RAG%20%2B%20Agents-FF8F00) ![MCP](https://img.shields.io/badge/Tools-MCP-1565C0)

## High-Level Architecture

> **Visual legend:** 🟦 Edge & Identity · 🟩 Application Services · 🟨 Data & Messaging · 🟪 AI/RAG · 🟥 Security & Observability  
> The diagram uses high-contrast fills and dark borders so the architecture remains readable in both light and dark GitHub themes.

```mermaid
flowchart TB
    U["👥 Enterprise Users<br/>Admins • External Systems"]:::client
    W["🛡️ Azure WAF<br/>DDoS Protection"]:::security
    A["🚪 Azure API Management<br/>Rate Limits • Versioning • Policies"]:::edge
    I["🔐 Microsoft Entra ID<br/>OIDC/OAuth2 • RBAC • Tenant Context"]:::identity

    U --> W --> A
    A -. "Authenticate / Authorize" .-> I

    subgraph APP["🟩 APPLICATION & MICROSERVICES"]
        direction LR
        D["📄 Document Service<br/>Metadata • Versions • ACLs"]:::service
        ING["📥 Ingestion Service<br/>Validation • Extraction"]:::service
        P["⚙️ Processing Service<br/>Normalize • Classify • Chunk"]:::service
        E["🧬 Embedding Service<br/>Batch Embeddings"]:::ai
        IX["🔎 Indexing Service<br/>Vector + Metadata"]:::ai
        S["🔍 Search Service<br/>Hybrid Search • Rerank"]:::ai
        AG["🤖 Agent Service<br/>Planning • RAG • Tool Use"]:::agent
        MCP["🔌 MCP Gateway<br/>Allow-listed Tools • Resources"]:::agent
    end

    A --> D
    A --> S
    A --> AG
    I -. "Identity + Claims" .-> D
    I -. "Identity + Claims" .-> S
    I -. "Identity + Claims" .-> AG
    D --> ING --> P --> E --> IX
    S --> AG
    AG --> MCP

    subgraph DATA["🟨 DATA, STORAGE & EVENTING"]
        direction LR
        SQL[("🗄️ Azure SQL<br/>Tenants • Documents • ACLs • Jobs • Audit")]:::data
        BLOB[("📦 Azure Blob Storage<br/>Originals • Versions • Extracted Content")]:::storage
        SB[("📨 Azure Service Bus<br/>Commands • Events • Retry • DLQ")]:::messaging
        ADLS[("🌊 Azure Data Lake Storage Gen2<br/>Raw • Curated • Analytics")]:::storage
        VDB[("🧠 Vector Store / Search Index<br/>Embeddings • Metadata • ACL Filters")]:::vector
        DBX["⚡ Azure Databricks<br/>ETL • Enrichment • Batch Processing"]:::data
    end

    D <--> SQL
    D --> BLOB
    D --> SB
    ING --> SB
    P --> ADLS
    P --> DBX
    E --> VDB
    IX --> VDB
    S <--> VDB
    SB --> ING
    SB --> P
    DBX --> ADLS

    subgraph AI["🟪 AI, RAG & AGENTIC INTELLIGENCE"]
        direction TB
        RAG["📚 RAG Pipeline<br/>Query Rewrite → Retrieval → Rerank → Context"]:::rag
        PE["📝 Prompt Engineering<br/>System Policy • Grounding • Citations"]:::prompt
        HARNESS["🎛️ Agent Harness<br/>Policy • Token Budget • Tool Limits • Approvals"]:::agent
        FOUNDRY["✨ Azure AI Foundry<br/>Models • Evaluations • Tracing"]:::model
    end

    S --> RAG --> PE
    AG --> HARNESS --> RAG
    PE --> FOUNDRY
    MCP --> FOUNDRY

    subgraph OPS["🟥 SECURITY, GOVERNANCE & OBSERVABILITY"]
        direction LR
        KV["🔑 Azure Key Vault<br/>Secrets • Certificates"]:::security
        MON["📊 Azure Monitor / App Insights<br/>Metrics • Logs • Distributed Tracing"]:::observability
        AUD["🧾 Audit Service<br/>Immutable Security Events"]:::security
        DEF["🛡️ Defender for Cloud<br/>Threat Detection • Posture"]:::security
    end

    I --> KV
    D --> AUD
    AG --> AUD
    MCP --> AUD
    A --> MON
    D --> MON
    ING --> MON
    P --> MON
    E --> MON
    S --> MON
    AG --> MON
    MCP --> MON
    MON --> DEF

    BLOB -. "Document Content" .-> ING
    VDB -. "Authorized Chunks" .-> RAG
    SQL -. "ACL / Tenant Policy" .-> S
    AUD -. "Compliance Evidence" .-> MON

    classDef client fill:#E0F2FE,stroke:#075985,stroke-width:3px,color:#082F49;
    classDef edge fill:#DBEAFE,stroke:#1D4ED8,stroke-width:3px,color:#172554;
    classDef identity fill:#F3E8FF,stroke:#7E22CE,stroke-width:3px,color:#3B0764;
    classDef security fill:#FEE2E2,stroke:#B91C1C,stroke-width:3px,color:#450A0A;
    classDef service fill:#DCFCE7,stroke:#15803D,stroke-width:3px,color:#052E16;
    classDef data fill:#FEF3C7,stroke:#B45309,stroke-width:3px,color:#451A03;
    classDef storage fill:#FEF9C3,stroke:#A16207,stroke-width:3px,color:#422006;
    classDef messaging fill:#FFEDD5,stroke:#C2410C,stroke-width:3px,color:#431407;
    classDef ai fill:#EDE9FE,stroke:#6D28D9,stroke-width:3px,color:#2E1065;
    classDef vector fill:#E0E7FF,stroke:#4338CA,stroke-width:3px,color:#1E1B4B;
    classDef agent fill:#FCE7F3,stroke:#BE185D,stroke-width:3px,color:#500724;
    classDef rag fill:#EDE9FE,stroke:#6D28D9,stroke-width:3px,color:#2E1065;
    classDef prompt fill:#FAE8FF,stroke:#A21CAF,stroke-width:3px,color:#4A044E;
    classDef model fill:#DDD6FE,stroke:#5B21B6,stroke-width:3px,color:#2E1065;
    classDef observability fill:#CCFBF1,stroke:#0F766E,stroke-width:3px,color:#042F2E;

    style APP fill:#F0FDF4,stroke:#166534,stroke-width:4px
    style DATA fill:#FFFBEB,stroke:#92400E,stroke-width:4px
    style AI fill:#F5F3FF,stroke:#5B21B6,stroke-width:4px
    style OPS fill:#FEF2F2,stroke:#991B1B,stroke-width:4px
```

### End-to-End Request & Document Flow

**Upload:** Client → WAF/APIM → Entra ID → Document Service → Blob/SQL → Service Bus → Ingestion → Processing → Embedding → Vector Index.

**Search / RAG:** Client → APIM → Entra ID → Search Service → ACL-filtered hybrid retrieval → Reranking → RAG context → Prompt Policy → Azure AI Foundry → cited response.

**Agentic flow:** Client → Agent Service → Agent Harness → RAG and/or MCP Gateway → allow-listed tools/resources → Azure AI Foundry → grounded response → Audit/Telemetry.

**Security boundary:** Authorization and tenant/ACL filtering happen **before retrieved content is placed into the model context**.

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