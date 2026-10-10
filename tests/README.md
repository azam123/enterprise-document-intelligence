# Tests

## Projects
- `Unit/Unit.csproj` — cross-service policy, domain, messaging contract and component tests.
- `DocumentService/DocumentService.Tests.csproj` — focused DocumentService application and domain tests.

## Run locally
```bash
dotnet test tests/Unit/Unit.csproj -c Release
dotnet test tests/DocumentService/DocumentService.Tests.csproj -c Release
```

The CI workflow currently runs a service-filtered coverage pass for each service. Coverage results should be treated as measured only when the Cobertura report has non-zero coverable lines; shared test totals are not separate per-service test counts.
