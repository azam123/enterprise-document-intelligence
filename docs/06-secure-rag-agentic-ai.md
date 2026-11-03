# Secure RAG, Prompt Engineering and Agentic AI Harness

## Scope and implementation boundary

This document describes the security and reliability controls for the Enterprise Document Intelligence platform. The deterministic guard and agent execution policy in `src/BuildingBlocks/AI/Security` are reusable first-pass controls. They do **not** prove that an LLM is immune to prompt injection or hallucination. Model-side evaluations, identity enforcement, data-plane ACLs and operational controls remain mandatory.

## RAG pipeline

1. **Ingest** — validate file type, size, malware scan result, tenant ownership and document ACLs before persistence.
2. **Extract** — retain source document ID, version, page/section and extraction provenance. Treat extracted text as untrusted data.
3. **Chunk** — use structure-aware boundaries; preserve document/page metadata and avoid mixing tenants or security labels.
4. **Embed and index** — version embedding model and chunking configuration; apply tenant and ACL metadata to every indexed chunk.
5. **Retrieve** — enforce tenant/principal/document ACL filters in the retrieval query, before content is returned to the application. Never rely on the prompt to enforce authorization.
6. **Rerank and assemble context** — deduplicate, apply relevance thresholds, cap context tokens, and preserve citation metadata.
7. **Generate** — use versioned system/developer prompts; label retrieved material as untrusted evidence, not instructions.
8. **Validate response** — require citations for factual document claims; verify citation IDs belong to retrieved chunks; return an explicit insufficient-evidence response when support is weak.

## Prompt engineering and prompt security

- Keep trusted system/developer instructions separate from user questions and retrieved document content.
- Delimit retrieved text and state explicitly that instructions inside it must not be followed.
- Use structured output schemas and validate outputs server-side.
- Do not place secrets, credentials, hidden prompts or authorization decisions in model context.
- Apply input/output size limits, encoding normalization and control-character handling.
- Screen user and retrieved text with `PromptSecurityGuard`; record the decision and a safe reason code, not raw sensitive prompt content.
- Treat heuristic matches as a signal. Use adversarial evaluation sets and human review for uncertain/high-impact cases.
- Never allow model output alone to authorize access, execute SQL, invoke privileged tools or change tenant/document permissions.

## Hallucination controls

- Ground answers only in retrieved, authorized evidence.
- Require source citations and validate every cited chunk against the retrieval result.
- Configure a minimum relevance/groundedness threshold and abstain when evidence is missing or conflicting.
- Separate quoted facts, inferred conclusions and recommendations.
- Evaluate retrieval Recall@K / nDCG, answer faithfulness, citation precision and abstention quality against a versioned golden dataset.
- Track model, prompt, embedding, index and evaluation-set versions for reproducibility.

## Agentic AI harness

Every agent run should execute inside a server-side harness with:

- Tool allow-list and per-tool JSON schema validation.
- Tenant/principal authorization at tool execution time (not only at planning time).
- Maximum tool-call count, token budget, wall-clock timeout and request size.
- Per-tool rate limits, concurrency limits and cancellation propagation.
- Idempotency keys for mutating operations.
- Human approval for external communication, destructive actions, financial actions or permission changes.
- Sandboxed execution for code or file-processing tools.
- Complete audit events for run ID, tool name, policy decision, duration and outcome; redact secrets and sensitive document content.

`AgentExecutionPolicy` currently enforces an allow-list, tool-call ceiling and token budget. Its timeout property is a configuration value: callers must enforce it with cancellation tokens/timeouts around the actual model and tool calls. It does not itself perform authorization or execute tools.

## Monitoring and observability

Instrument each request and agent run with OpenTelemetry traces and correlated structured logs. Recommended dimensions:

| Area | Signals |
|---|---|
| RAG | retrieval latency, candidate count, reranker latency, Recall@K, citation precision |
| Model | provider latency, token input/output, throttles, timeout rate, cost |
| Agent | run duration, tool calls/run, denied calls, approval wait, loop termination |
| Security | injection detections, ACL denials, schema failures, rate-limit events |
| Reliability | error rate, retry count, DLQ depth, dependency availability |
| Performance | p50/p95/p99 latency, throughput, queue age, concurrency and saturation |

Do not use raw prompts, document text, access tokens or PII as metric labels. Use bounded-cardinality dimensions and redact sensitive log fields.

## Performance and resilience

- Set end-to-end deadlines and smaller per-dependency timeouts.
- Use bounded retries with exponential backoff and jitter for transient failures only.
- Add circuit breakers, bulkheads and queue back-pressure.
- Cache only with tenant/ACL-aware cache keys and explicit invalidation.
- Load-test ingestion, retrieval and concurrent agent runs separately; include large documents, long contexts and provider throttling.
- Define service-specific SLOs and alert on user-impacting symptoms, not only CPU/memory.

## Security verification checklist

- Cross-tenant retrieval and document IDOR tests.
- ACL changes and document deletion propagation tests.
- Direct and indirect prompt-injection test corpus.
- Tool allow-list, schema, replay, timeout and rate-limit tests.
- Secret/PII leakage tests for logs and generated answers.
- Dependency, secret, static-analysis and container scans in CI.
- Manual threat model review before production rollout.
