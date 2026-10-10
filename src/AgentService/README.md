# AgentService

ASP.NET Core endpoint for policy-validated agent requests. The API bounds prompt size, tool count and token budget before handing work to the agent harness.

## Main components
- `AgentRequestPolicy` — request normalization and limits.
- `AgentHarness` — orchestration boundary and model/tool integration.
- `AgentService.Application` — conversation lifecycle.
- `AgentService.Infrastructure` — conversation store registration.

## Endpoint
- `POST /api/v1/agent/run`

Configure model endpoint/credentials and allowed tools before enabling real model execution. The in-memory conversation store is intended for local development, not multi-instance production use.

```bash
dotnet run --project src/AgentService/AgentService.Api/AgentService.Api.csproj
```
