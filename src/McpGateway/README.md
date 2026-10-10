# McpGateway

Authenticated ASP.NET Core gateway for a constrained set of enterprise document tools. The API validates tool names and argument shapes before dispatching calls.

## Endpoints
- `GET /api/v1/mcp/tools` — list allowed tools.
- `POST /api/v1/mcp/tools/call` — invoke an allowed tool.

The registered tool executor/handler is an integration boundary. Before production, connect each tool to the real document/search/audit services, enforce tenant/ACL checks at execution time, add timeouts and quotas, and audit every invocation.

```bash
dotnet run --project src/McpGateway/McpGateway.Api/McpGateway.Api.csproj
```
