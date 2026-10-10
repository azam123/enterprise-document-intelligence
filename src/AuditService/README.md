# AuditService

Background worker and application components for consuming audit events, recording idempotent audit records and querying them by tenant, resource and time.

## Components
- `AuditWorker` / Service Bus processor — consumes audit events.
- `AuditEventProcessor` — validates and coordinates idempotent writes.
- `SqlAuditStore` — SQL Server persistence.
- `InMemoryAuditStore` — local/test adapter.

Configure `ServiceBus` and `ConnectionStrings:SqlServer`. The SQL schema/migrations, retention and access policy must be provisioned for the target environment.

```bash
dotnet run --project src/AuditService/AuditService.Api/AuditService.Api.csproj
```
