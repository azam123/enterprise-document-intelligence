# 💻 Codebase, Design Patterns, Error Handling & Tests

## Clean Architecture
Every service follows API/Worker → Application → Domain, with Infrastructure implementing application ports.

## CQRS
Commands mutate state through handlers; queries are optimized for read workloads. MediatR or equivalent mediator wiring is isolated in Application.

## Dependency Injection
Register repositories, unit of work, storage, messaging, AI and vector adapters through interfaces. Do not use a service locator.

## Error Handling
Use RFC 9457 Problem Details. Map domain errors to 400/404/409/422 and transient dependency exhaustion to 503. Never expose stack traces or secrets.

## Logging
Use structured logs with correlation, causation, tenant-safe identifiers and trace IDs. Never log document contents, access tokens or sensitive prompts.

## Service Bus
Consumers must be idempotent, bounded, retry transient failures with jitter, renew long locks, and dead-letter poison messages.

## Outbox
Persist business state and OutboxMessage in the same SQL transaction. A publisher drains the outbox to Service Bus and records publication state.

## Security
Validate Entra ID JWT issuer, audience, signature, expiry and scopes. Apply tenant and document ACL authorization before every data operation and before RAG context creation.

## RAG Interfaces
IRetriever, IEmbeddingModel and IChatModel abstractions make AI providers replaceable and testable.

## Agent Harness
AgentPolicy controls allowed tools, maximum tool calls, token budget, timeout, data classifications and approval level.

## MCP
The gateway validates identity, tenant, authorization, JSON schema, input size and rate limits before executing tools.

## Testing
Unit tests cover domain rules; integration tests cover SQL/Blob/Service Bus/vector adapters; security tests cover isolation and ACLs; AI evaluation covers Recall@K, groundedness, citations and prompt injection.

## Design Patterns
| Pattern | Purpose |
|---|---|
| Clean Architecture | business isolation |
| CQRS | command/query separation |
| Repository | persistence abstraction |
| Unit of Work | transaction boundary |
| Outbox | reliable messaging |
| Strategy | interchangeable chunking/retrieval/model strategies |
| Adapter | cloud/provider integrations |
| Factory | extractor/model selection |
| Circuit Breaker | unstable dependencies |
| Bulkhead | workload isolation |
| Saga | long-running workflows |

## SOLID
Single Responsibility separates ingestion, chunking, embedding and indexing. Open/Closed uses adapters for providers. Liskov preserves interface contracts. Interface Segregation favors small ports. Dependency Inversion keeps Application independent from Azure SDKs.