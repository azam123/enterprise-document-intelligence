# Test Strategy, CI Quality Gates and Test Report

## Test pyramid

| Layer | Scope | Expected dependencies |
|---|---|---|
| Unit | Domain rules, prompt screening, agent policy, validators, chunking and mapping | No network or Azure resources |
| Integration | API/application boundaries, persistence, messaging, storage and search adapters | Test containers or isolated emulators/fakes |
| Contract | Service Bus event schemas, MCP tool schemas and API contracts | Versioned contract fixtures |
| Security | Tenant isolation, ACL enforcement, prompt injection, tool authorization and secret redaction | Isolated security fixtures |
| AI evaluation | Retrieval quality, groundedness, citations, abstention and adversarial prompts | Versioned evaluation dataset; controlled model endpoint |
| Performance | Ingestion throughput, retrieval latency, agent concurrency and rate limits | Dedicated load-test environment |

## Current automated tests

- `tests/Unit`: xUnit unit tests, including prompt-injection screening and agent execution policy boundary cases.
- `tests/Integration`: deterministic integration-style tests that compose prompt screening with agent tool policy. These do not contact Azure or a live model.
- `tests/DocumentService`: existing DocumentService application tests.
- `.github/workflows/build.yml`: existing service build and per-service coverage workflow.
- `.github/workflows/pr-quality.yml`: pull-request quality workflow for restore/build/test, coverage artifact and static analysis.

## Required pull-request checks

1. Restore all supported .NET projects.
2. Build all service entry projects in Release.
3. Run unit, integration and DocumentService tests; any failing test blocks the PR.
4. Publish Cobertura coverage and TRX test results as workflow artifacts.
5. Run CodeQL analysis and dependency review where GitHub repository settings and plan support them.
6. Keep test output and coverage report available for reviewer inspection.

Coverage is reported per test run. A coverage percentage is not proof of meaningful assertions; reviewers should verify negative cases, authorization boundaries and failure paths. The existing build workflow reports per-service coverage and warns below its configured 90% target. Do not claim a service meets a target unless its measured report shows it.

## Test report template

| Suite | Result | Tests passed/failed | Line coverage | Notes |
|---|---|---:|---:|---|
| Unit | Pending CI | CI generated | CI generated | Includes prompt and harness policy |
| Integration | Pending CI | CI generated | CI generated | Deterministic boundary composition |
| DocumentService | Pending CI | CI generated | CI generated | Existing application suite |
| Security / AI evaluation | Follow-up | Dataset-driven | N/A | Requires broader service adapters and evaluation corpus |
| Performance | Follow-up | N/A | N/A | Run in a representative environment |

## Honest completion criteria

A green CI run verifies only the jobs and tests that actually executed. Production-grade completion additionally requires real adapter integration tests, cross-tenant isolation tests, adversarial RAG/agent evaluations, load tests, dependency/security scans, and deployment-specific operational validation. Never publish a fabricated test count or coverage percentage.
