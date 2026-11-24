# Private Enterprise Document Intelligence MVP

A self-hosted document Q&A MVP. The AI path uses local Ollama inference; no OpenAI, Anthropic, Gemini or other public LLM API is called.

## Components
- Next.js 15 web UI.
- .NET 10 gateway (health and platform metadata).
- Python 3.12 / FastAPI document and question API.
- PostgreSQL 16 with pgvector.
- Kafka event publication for indexed-document events.
- Ollama local inference: qwen2.5:3b for chat and nomic-embed-text for 768-dimensional embeddings.
- Pytest unit tests and GitHub Actions coverage artifacts.

## Run locally
Prerequisites: Docker Engine/Desktop with Compose v2 and ideally 8 GB+ RAM. First startup downloads local models and requires internet; inference is local after download. GPU acceleration is optional.

```bash
docker compose -f docker-compose.mvp.yml up --build
```

Open web UI http://localhost:3000, AI API docs http://localhost:8000/docs, AI health http://localhost:8000/health, gateway health http://localhost:8080/health.

## API
- `POST /documents`: multipart upload; requires `X-Tenant-Id`; PDF, DOCX, TXT and Markdown.
- `POST /ask`: JSON `{"question":"What does the contract say?"}`; requires `X-Tenant-Id`.
- `GET /health`: liveness endpoint.

## Tests
```bash
cd mvp/ai-service
python -m venv .venv
# activate virtual environment
pip install -r requirements.txt
pytest -q --cov=app.core --cov-report=term-missing --cov-report=xml:coverage.xml --junitxml=test-results.xml
```

GitHub Actions runs Python unit tests and coverage, .NET 10 build, Next.js production build and Docker Compose validation. Test and coverage XML are uploaded as workflow artifacts.

## MVP limitations — read before customer data
This is a development MVP, not production-ready. Before external customer data, implement OIDC/SSO, trusted tenant identity (never trust a caller-supplied tenant header), malware scanning, upload size limits, rate limiting, durable audit events, document-level ACLs, secret management, backups, retention/deletion, observability, security tests and deployment hardening. The tenant field in the demo UI is not authentication.

Kafka currently receives an event after synchronous indexing; production should use a transactional outbox and independently scaled worker. Customer-specific retrieval and model configuration are implemented as the first customization layer; automatic per-customer fine-tuning is not included.

## Model licensing
Review the precise model licenses and acceptable-use terms before commercial distribution. Models are downloaded into the local Ollama volume. This MVP sends no customer content to public model APIs.
