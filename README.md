# EAP API

.NET 8 Web API baseline for a medium-to-large internal service. Persistence uses Dapper with named SQL Server, Oracle and MySQL connections; EF Core is intentionally not used.

团队成员开始开发前，请先阅读 [架构与团队开发规范](DEVELOPMENT_GUIDE.md)。该文档说明每一层的职责、依赖与调用顺序、新功能开发步骤以及提交前检查清单。

## Architecture

- `EAP.Api`: controllers, authentication/authorization, middleware, rate limiting, health endpoints and composition root.
- `EAP.Application`: business use cases and ports (`IHttpApiClient`, `ITokenService`, `IMessageLocalizer`).
- `EAP.Infrastructure`: Dapper repositories, multi-provider database connections, database health checks and downstream HTTP client.
- `EAP.Models`: public request/response contracts and business response codes.
- `EAP.UnitTests`: application unit tests.
- `EAP.IntegrationTests`: full HTTP pipeline tests with isolated test configuration and no real database calls.
- `Database`: idempotent SQL deployment scripts.

Dependency direction:

```text
Api -> Application
Api -> Infrastructure -> Application
Contracts <- Api/Application
```

## Local configuration

Do not commit production secrets. Configure sensitive values with environment variables, a secret store, or .NET user secrets:

```powershell
dotnet user-secrets --project EAP.Api set "Jwt:SigningKey" "a-development-key-at-least-32-characters"
dotnet user-secrets --project EAP.Api set "Jwt:Clients:0:ClientSecret" "the-client-specific-secret"
dotnet user-secrets --project EAP.Api set "Databases:Connections:Dashboard:Enabled" "true"
```

JWT authentication is always enabled. JWT settings and client credentials are grouped under the `Jwt` section. The current local configuration contains two independent clients:

```text
ClientId: EAP_SYSTEM,       ClientName: EAP
ClientId: DASHBOARD_SYSTEM, ClientName: DASHBOARD
```

Each client must use its own secret. Before production deployment, move `Jwt:SigningKey` and every `Jwt:Clients:n:ClientSecret` value to environment variables, a secret store, or user secrets; do not use repository configuration as the production secret source.

External HTTP endpoints are configured under `HttpApi:Endpoints`. `IHttpApiClient` sends requests only when a business service calls it; there is no polling worker. Each endpoint defaults to `POST`, and an individual request can override the method to `GET`.

## Database

Named connections are self-contained under `Databases:Connections`; each node contains its provider, encrypted connection string, enabled state, command timeout and health-check state. Supported providers are `SqlServer`, `Oracle` and `MySql`. `RepositoryBase` provides parameterized Dapper queries, stored procedures, managed connections and local transactions. SQL Server bulk imports and staging-table merges use the provider-specific `ISqlServerBulkWriter`. Add repeatable provider-specific SQL deployment scripts under `Database` as business tables are introduced. Redis is not required.

All configured database connection strings use the `enc:v1:` AES-GCM format. The 32-byte AES-256 key is stored as Base64 in `.secrets/eap-database.key`. The complete `.secrets` directory is excluded by `.gitignore`; the key must be copied separately during deployment and must not be committed with the source code. `Encryption:KeyFilePath` is resolved relative to the API content root. `DbConnectionFactory` decrypts a connection string only in memory immediately before creating the provider connection.

If the encrypted value or key is modified, startup validation or authenticated AES-GCM decryption fails instead of attempting a database connection. Back up the key securely: losing it makes every encrypted connection string unrecoverable.

## Endpoints

- `POST /api/v1/auth/token`: obtain a local JWT access token (anonymous by necessity).
- `GET /health/live`: process liveness.
- `GET /health/ready`: SQL Server readiness.
- `GET /api/v1/process-assemblies`: authenticated paged multi-condition query.
- `GET /api/v1/process-assemblies/{id}`: authenticated detail query including related `SN_DEFECT_DET_API` rows in `defects`.
- `POST /api/v1/process-assemblies`: authenticated create operation.
- `PUT /api/v1/process-assemblies/{id}`: authenticated full update.
- `DELETE /api/v1/process-assemblies/{id}`: authenticated delete operation.
- `GET /swagger/v1/swagger.json`: development-only OpenAPI document.
- `GET /swagger`: development-only Swagger UI.

All business controllers require JWT by the global fallback policy. The token endpoint and minimal health endpoints are anonymous so clients can obtain credentials and infrastructure can probe the process. In Swagger, obtain a token and enter it in `Authorize` as `Bearer {token}`.

`Database/001_AddProcessAssemblyApiPrimaryKey.sql` adds the `ID BIGINT IDENTITY` primary key required for safe single-record CRUD on `dbo.PROCESS_ASSEMBLY_API`. Run database scripts before deploying the matching API version.

`Database/002_AddSnDefectRelationIndex.sql` adds the nonclustered index used by the process-to-defect relation query. The relation uses project, product, process and lot fields; the detail repository retrieves the parent and its one-to-many defect rows in one parameterized multi-result SQL command.

## Localization

Response messages support Chinese and English. Send the standard `Accept-Language` header:

```text
Accept-Language: en-US
Accept-Language: zh-CN
```

Chinese is the default. Resource text is stored in `EAP.Models/Resources` while the HTTP culture adapter remains in the API layer.

## Logging

NLog is configured only in the API composition root. Each request writes one readable timing chain instead of separate start/completed messages for every layer. Unhandled exceptions include the full stack trace, failed layer/method and business error code.

```text
2026-10-11 00:10:21.123 | INFO | [REQUEST] GET /api/v1/process-assemblies/20 | Status=200 | Total=48.0ms | Controller[ProcessAssembliesController].GetById=42.1ms > Application[ProcessAssemblyService].GetByIdAsync=39.8ms > Database[Dashboard].GetDetailsByIdAsync=31.5ms | Trace=...
2026-10-11 00:10:22.123 | ERROR | [UNHANDLED_ERROR] GET /api/v1/process-assemblies/20 | Status=500 | Total=48.0ms | Controller[...] > Application[...] > Database[...] | FailedAt=Application[ProcessAssemblyService].GetByIdAsync | Code=5002 | Error=... | Trace=...
```

- All logs: `EAP.Api/bin/{Configuration}/net8.0/logs`
- Error logs: `EAP.Api/bin/{Configuration}/net8.0/logs/error`
- Text encoding: UTF-8.
- Rotation: daily and whenever a file reaches 100 MB.
- Retention: 30 archives for all logs and 90 archives for error logs.
- Successful Swagger and health-check requests are omitted; failures are still logged.

## Build and test

```powershell
dotnet restore EAP.WebApi.sln
dotnet build EAP.WebApi.sln --no-restore
dotnet test EAP.WebApi.sln --no-build
```
