# ADR-002: Authorization before RAG context

Tenant and document ACL filtering must happen before chunks are placed into an LLM context. Post-generation filtering is insufficient for protecting sensitive content.