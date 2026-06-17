# AGENTS.md

## Repo snapshot

.NET 10 Clean Architecture REST API — "Doclyn" (document intelligence: PDF upload, OCR, AI extraction, processing history, dashboard). Authentication with JWT + refresh tokens + password recovery is implemented; other business features are still pending.

## Solution format

Solution file is `Doclyn.slnx` (new XML format, requires .NET 10 SDK). `dotnet` CLI finds it automatically from the repo root.

**`Doclyn.IntegrationTests` is NOT in `Doclyn.slnx`** — must be built/tested by explicit project path. The solution contains 5 projects: Api, Application, Domain, Infrastructure, UnitTests.

## Commands

```sh
# Build (all 5 solution projects)
dotnet build

# Run API — default profile is https
dotnet run --project Doclyn.Api/Doclyn.Api.csproj
# Ports: http://localhost:5172  |  https://localhost:7292

# Health check
GET /health

# OpenAPI (Development only)
GET /openapi/v1.json

# Run unit tests
dotnet test

# Run integration tests (must target project directly — not in solution)
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj

# EF Core migrations
# The startup project needs Microsoft.EntityFrameworkCore.Design.
dotnet ef migrations add <Name> --project Doclyn.Infrastructure --startup-project Doclyn.Api --output-dir Database\Migrations
dotnet ef database update --project Doclyn.Infrastructure --startup-project Doclyn.Api
```

No Makefile, no build scripts, no CI. Docker Compose is available for local development (see [Local development environment](#local-development-environment)).

## Local development environment

Docker Compose provides the required external services for local development. The compose file is versioned at the repository root.

### Services

| Service | Image | Host port | Purpose |
|---|---|---|---|
| PostgreSQL | `postgres:16-alpine` | `5432` | Application database |
| MinIO | `minio/minio:latest` | `9000` (API) / `9001` (console) | S3-compatible object storage |
| Seq | `datalust/seq:latest` | `5341` | Log aggregation |
| Mailpit | `axllent/mailpit:latest` | `1025` (SMTP) / `8025` (UI) | Local SMTP test server |

### Start the environment

```sh
docker compose up -d
```

### Access URLs

| Service | URL | Credentials |
|---|---|---|
| Doclyn API (HTTP) | `http://localhost:5172` | — |
| Doclyn API (HTTPS) | `https://localhost:7292` | — |
| MinIO Console | `http://localhost:9001` | `minioadmin` / `minioadmin` |
| Seq | `http://localhost:5341` | — (authentication disabled for local dev) |
| Mailpit UI | `http://localhost:8025` | — |

### First-time setup

1. Start the containers:
   ```sh
   docker compose up -d
   ```

2. Create the `doclyn-documents` bucket in MinIO:
   - Open `http://localhost:9001`
   - Sign in with `minioadmin` / `minioadmin`
   - Create a bucket named `doclyn-documents`

   Alternatively, you can create it with the MinIO Client:
   ```sh
   docker run --rm --network doclyn-dev minio/mc:latest alias set doclyn http://doclyn-minio:9000 minioadmin minioadmin
   docker run --rm --network doclyn-dev minio/mc:latest mb doclyn/doclyn-documents
   ```

3. Apply EF Core migrations:
   ```sh
   dotnet ef database update --project Doclyn.Infrastructure --startup-project Doclyn.Api
   ```

4. Run the API:
   ```sh
   dotnet run --project Doclyn.Api/Doclyn.Api.csproj
   ```

5. Verify health:
   ```sh
   curl -k https://localhost:7292/health
   ```

### Stop the environment

```sh
# Stop without removing data
docker compose down

# Stop and remove all persistent data
docker compose down -v
```

### Development settings

`Doclyn.Api/appsettings.Development.json` is pre-configured to connect to the Docker Compose services on `localhost`. SMTP points to Mailpit (`localhost:1025`) so password-reset emails can be inspected locally without real credentials.

## Architecture

```
Doclyn.Domain           → entities, value objects, enums — no external dependencies
Doclyn.Application      → use cases, interfaces, DTOs, MediatR handlers, FluentValidation — depends on Domain only
Doclyn.Infrastructure   → AI, Database, Jobs, OCR, Security, Storage impls — depends on Application + Domain
Doclyn.Api              → HTTP host, controllers, middleware — depends on Application + Infrastructure
```

Project references are configured. Do not reference Infrastructure from Domain or Application.

```
Doclyn.Application      → Doclyn.Domain
Doclyn.Infrastructure   → Doclyn.Application + Doclyn.Domain
Doclyn.Api              → Doclyn.Application + Doclyn.Infrastructure
Doclyn.UnitTests        → Doclyn.Application + Doclyn.Domain
Doclyn.IntegrationTests → Doclyn.Api
```

## Project state

| Layer | Content |
|---|---|
| Domain | `BaseEntity`, `AuditableEntity`, `User`, `RefreshToken`, `PasswordResetRequest`, `Document`, `DocumentClass`, `DocumentClassExample`, `DocumentClassIndexer`, `ExtractedData`, `ProcessingLog`, `DocumentStatus`, `DocumentType`, `UserRole`, `IndexerDataType`, `ExtractionSource`; empty `ValueObjects/` |
| Application | MediatR handlers in `Auth/` (Register, Login, Refresh, Logout, Me, ForgotPassword, VerifyResetCode, ResetPassword), `DocumentClasses/` (GetAll, GetById, GetExamples) and `DocumentClassIndexers/` (GetByDocumentClass, Create, Update, Disable); `IApplicationDbContext`, `IUnitOfWork`, `ITokenService`, `IPasswordHasher`, `ICurrentUserService`, `IEmailService`, `IDocumentClassCatalogService`, `IDocumentClassIndexerCatalogService`; `ValidationBehavior`, `ValidationException` |
| Infrastructure | `DoclynDbContext` (with global `UpperSnakeCaseConvention`), `StorageOptions`, `DocumentOptions`, `JwtOptions`, `SmtpOptions`, `JwtTokenService`, `PasswordHasherService`, `CurrentUserService`, `SmtpEmailService`, `MinioFileStorageService`, `FileHashService`, `DocumentClassCatalogService`, `DocumentClassCatalogSeeder`, `DocumentClassIndexerCatalogService`, `DocumentClassIndexerSeeder`, `DocumentClassAiSchemaBuilder`, catalog-driven `RegexDocumentIndexer`, dynamic AI `OpenAiStructuredDataExtractor`; DI wires EF Core + Npgsql + MinIO + Serilog + Security + Email + Storage |
| Api | `AuthController`, `DocumentClassesController`, `DocumentClassIndexersController`, `GlobalExceptionMiddleware`; `Program.cs` with JWT auth, rate limiting, Serilog, DI, health checks, OpenAPI |
| UnitTests | Tests for auth, documents, document classes, document class indexers, processing pipeline |
| IntegrationTests | Tests for auth, documents, document classes, document class indexers |

## Key facts

- All projects target `net10.0` with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`. Treat null warnings as errors in intent.
- No NuGet Central Package Management (`Directory.Packages.props` absent). Each `.csproj` owns its own version attributes.
- No `global.json` — SDK version not pinned. Requires .NET 10 SDK on PATH.
- No `Directory.Build.props` — no shared MSBuild properties.
- `Doclyn.Application` is SDK (not Web) — `IServiceCollection`, `IConfiguration`, EF Core, MediatR, and FluentValidation are explicit NuGet refs.
- MediatR pipeline includes `ValidationBehavior<,>` for FluentValidation. All validators are auto-registered from the Application assembly.
- OpenAPI via `Microsoft.AspNetCore.OpenApi` (not Swashbuckle). `/openapi/v1.json` exposed in Development only.
- JWT authentication is configured in `Program.cs` with `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`.
- Test projects: xUnit 2.9.3. `Xunit` namespace globally imported via `<Using Include="Xunit" />` — no `using Xunit;` needed.
- `.gitignore` excludes `.vs/`, `bin/`, `obj/`, `TestResults/`.

## Auth endpoints

```
POST /api/auth/register            → public
POST /api/auth/login               → public
POST /api/auth/refresh-token       → public
POST /api/auth/forgot-password     → public (rate limited: 10/h IP + 3/h user)
POST /api/auth/verify-reset-code   → public (rate limited: 20/h IP)
POST /api/auth/reset-password      → public
POST /api/auth/logout              → [Authorize]
GET  /api/auth/me                  → [Authorize]
```

## Document class endpoints

```
GET /api/document-classes            → [Authorize]
GET /api/document-classes/{id}       → [Authorize]
GET /api/document-classes/{id}/examples → [Authorize]
GET /api/document-classes/{id}/indexers  → [Authorize]
POST /api/document-classes/{id}/indexers → [Authorize(Roles = Admin)]
PUT /api/document-classes/{id}/indexers/{indexerId} → [Authorize(Roles = Admin)]
DELETE /api/document-classes/{id}/indexers/{indexerId} → [Authorize(Roles = Admin)]
```

Taxonomia oficial: `RELATORIO_TECNICO_PRELIMINAR`, `CONTRATO_ADMINISTRATIVO`, `OFICIO`, `NOTA_FISCAL`, `PETICAO_JUDICIAL`, `DOCUMENTO_DESCONHECIDO`.
`DisplayName` é derivado automaticamente de `Name`.

## Infrastructure packages (current)

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | 10.0.9 | ORM |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 | PostgreSQL provider |
| `Minio` | 7.0.0 | MinIO / S3-compatible storage client |
| `System.IdentityModel.Tokens.Jwt` | 8.19.1 | JWT token generation |
| `Microsoft.Extensions.Identity.Core` | 10.0.9 | `PasswordHasher<T>` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | JWT middleware (Api project) |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | EF Core CLI design-time support (Api project) |
| `MediatR` | 14.1.0 | Mediator pattern (Application project) |
| `FluentValidation` | 12.1.1 | Input validation (Application project) |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Validator DI registration |
| `Serilog.AspNetCore` | 10.0.0 | Serilog integration for ASP.NET Core (Api project) |
| `Serilog.Extensions.Hosting` | 10.0.0 | `UseSerilog()` on IHostBuilder |
| `Serilog.Settings.Configuration` | 10.0.0 | `ReadFrom.Configuration` from appsettings.json |
| `Serilog.Sinks.Console` | 6.1.1 | Console sink |
| `Serilog.Sinks.Seq` | 9.1.0 | Seq sink |

## Domain baseline

```
Doclyn.Domain/Entities/BaseEntity.cs       → abstract, Guid Id
Doclyn.Domain/Entities/AuditableEntity.cs  → : BaseEntity, DateTime CreatedAt, DateTime? UpdatedAt
Doclyn.Domain/Entities/User.cs             → Name, Email, PasswordHash, Role, IsActive
Doclyn.Domain/Entities/RefreshToken.cs     → UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenHash
Doclyn.Domain/Entities/PasswordResetRequest.cs → UserId, CodeHash, ResetTokenHash, ExpiresAt, ResetTokenExpiresAt, Attempts, IsUsed, IsResetTokenUsed
Doclyn.Domain/Entities/Document.cs         → UserId, FileName, FileHash, StoragePath, DocumentType, DocumentStatus, ProcessedAt
Doclyn.Domain/Entities/DocumentClass.cs    → Name, DisplayName, Group, SubGroup, Description, IsSystemDefined, IsActive
Doclyn.Domain/Entities/DocumentClassExample.cs → DocumentClassId, DocumentId, Confidence, CreatedAt
Doclyn.Domain/Entities/DocumentClassIndexer.cs → DocumentClassId, Name, DisplayName, Description, DataType, IsRequired, IsMultiple, ExtractionHint, RegexPattern, IsActive
Doclyn.Domain/Entities/ExtractedData.cs    → DocumentId, Data (jsonb)
Doclyn.Domain/Entities/ProcessingLog.cs    → DocumentId, Step, Message, Status, CreatedAt
Doclyn.Domain/Enums/DocumentStatus.cs      → Pending | Processing | Processed | Failed | Success
Doclyn.Domain/Enums/DocumentType.cs        → Unknown | RG | CPF | CNH | Payslip | ProofOfAddress
Doclyn.Domain/Enums/UserRole.cs            → Admin | Operator
Doclyn.Domain/Enums/IndexerDataType.cs     → Text | Number | Decimal | Date | Boolean | Cpf | Cnpj | Email | Phone | Cep | Currency | Object | Array
Doclyn.Domain/Enums/ExtractionSource.cs    → Regex | AI | Merged | Manual
```

## Application interfaces

```
Doclyn.Application/Common/Interfaces/IApplicationDbContext.cs  → DbSet<User>, DbSet<RefreshToken>, DbSet<PasswordResetRequest>, DbSet<Document>, DbSet<ExtractedData>, DbSet<ProcessingLog>, DbSet<DocumentClass>, DbSet<DocumentClassExample>, DbSet<DocumentClassIndexer>, SaveChangesAsync
Doclyn.Application/Common/Interfaces/IUnitOfWork.cs            → CommitAsync
Doclyn.Application/Common/Interfaces/ITokenService.cs          → GenerateAccessToken, GenerateRefreshToken, HashRefreshToken
Doclyn.Application/Common/Interfaces/IPasswordHasher.cs        → Hash, Verify
Doclyn.Application/Common/Interfaces/ICurrentUserService.cs    → UserId, Email, Role, IsAuthenticated
Doclyn.Application/Common/Interfaces/IEmailService.cs          → SendPasswordResetCodeAsync
Doclyn.Application/Common/Interfaces/IFileStorageService.cs    → UploadAsync
Doclyn.Application/Common/Interfaces/IFileHashService.cs       → ComputeSha256Async
Doclyn.Application/Common/Interfaces/IDocumentClassCatalogService.cs → FindByNameAsync, GetOrCreateAsync, RegisterExampleAsync
Doclyn.Application/Common/Interfaces/IDocumentClassIndexerCatalogService.cs → GetActiveByDocumentClassAsync, CreateAsync, UpdateAsync, DisableAsync
```

## Infrastructure wiring

```csharp
// Doclyn.Infrastructure/DependencyInjection.cs
services.AddInfrastructure(IConfiguration configuration)
  → AddDatabase()       // EF Core + Npgsql; registers IApplicationDbContext + IUnitOfWork via DoclynDbContext
  → AddSecurity()       // JwtOptions, ITokenService, IPasswordHasher, ICurrentUserService
  → AddEmail()          // SmtpOptions, IEmailService
  → AddMinioStorage()   // registers IMinioClient + binds MinioOptions
  → AddSerilogLogging() // ReadFrom.Configuration; call AddSerilog() on IServiceCollection
```

```csharp
// Doclyn.Application/DependencyInjection.cs
services.AddApplication()
  → MediatR with ValidationBehavior<,>
  → FluentValidation validators from the Application assembly
```

Both are registered in `Program.cs`. Add service registrations there as features are built.

## Serilog configuration

Serilog is configured entirely via `appsettings.json` under the `"Serilog"` key — no hardcoded sinks in code.
`Program.cs` creates a bootstrap logger (Console only) before the host starts, then `builder.Host.UseSerilog()` replaces it.
Seq URL defaults to `http://localhost:5341` in both environments.

## Storage configuration

Storage options are bound from `appsettings.json` → `"Storage"` section via `StorageOptions` (record in `Application/Common/Options/`).
Default dev values point to the local MinIO instance at `localhost:9000` with `minioadmin` credentials and bucket `doclyn-documents`.

```json
"Storage": {
  "Provider": "Minio",
  "BucketName": "doclyn-documents",
  "Endpoint": "localhost:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "UseSsl": false
}
```

Document upload settings are bound from `appsettings.json` → `"Documents"` section via `DocumentOptions`.

```json
"Documents": {
  "MaxUploadSizeInMb": 10
}
```

## JWT configuration

JWT options are bound from `appsettings.json` → `"Jwt"` section via `JwtOptions` (record in `Infrastructure/Security/`).

```json
"Jwt": {
  "Issuer": "Doclyn",
  "Audience": "Doclyn",
  "Secret": "USE_USER_SECRETS_OR_ENVIRONMENT_VARIABLE",
  "AccessTokenExpirationMinutes": 15,
  "RefreshTokenExpirationDays": 7
}
```

**Never commit real secrets.** In production use User Secrets, environment variables, or a vault. `appsettings.Development.json` contains a placeholder secret for local dev only.

## SMTP configuration

SMTP options are bound from `appsettings.json` → `"Smtp"` section via `SmtpOptions` (record in `Infrastructure/Email/`).
Production values point to Hostinger (`smtp.hostinger.com:587`) with `From: contato@doclyn.com.br`.
Development values point to Mailpit (`localhost:1025`) with `From: dev@doclyn.local`.

**Never commit SMTP credentials.** Use User Secrets or environment variables for `Smtp:Username` and `Smtp:Password`.

## Rate limiting

ASP.NET Core built-in rate limiting is configured in `Program.cs`:

| Endpoint | Policy | Limit |
|---|---|---|
| `POST /api/auth/forgot-password` | `ForgotPasswordPerIp` | 10 requests/hour per IP |
| `POST /api/auth/verify-reset-code` | `VerifyResetCodePerIp` | 20 requests/hour per IP |

Per-user forgot-password limit (3/hour) is enforced inside the `ForgotPasswordHandler`.

## EF Core notes

- `DoclynDbContext` is in `Doclyn.Infrastructure/Database/`.
- Entity configurations go in `Infrastructure/Database/Configurations/` as `IEntityTypeConfiguration<T>` — auto-applied via `ApplyConfigurationsFromAssembly`.
- Global `UpperSnakeCaseConvention` is registered in `ConfigureConventions`. It converts table names, columns, keys, foreign keys and indexes from PascalCase to UPPER_SNAKE_CASE.
- Migrations assembly is set to `Doclyn.Infrastructure`; output directory is `Database/Migrations`.
- Migration command (run from repo root): `dotnet ef migrations add <Name> --project Doclyn.Infrastructure --startup-project Doclyn.Api --output-dir Database\Migrations`
- Existing migrations:
  - `InitialAuth` (created for `User` and `RefreshToken` entities).
  - `AddDocumentEntities` (adds `PasswordResetRequest`, `Document`, `ExtractedData`, and `ProcessingLog` entities).
  - `AddDocumentStatusSuccess` (adds `Success` value to the `DocumentStatus` enum used by processing logs).
  - `AddDocumentClasses` (adds `DocumentClass` and `DocumentClassExample` entities).
  - `AddDocumentClassIndexers` (adds `DocumentClassIndexer` and catalog-driven extraction metadata).

## Next steps (not yet implemented)

- Background job infrastructure
- Reprocessing, download, preview
- Seeding of first admin user

## EF Core migration note

Run the migration command to persist the new domain entities before deploying or running `dotnet ef database update`:

```sh
dotnet ef migrations add AddDocumentClassIndexers --project Doclyn.Infrastructure --startup-project Doclyn.Api --output-dir Database\Migrations
```

## Coding rules

1. Keep Domain pure — no EF Core, ASP.NET Core, OCR, AI, or storage SDK references.
2. Put interfaces in Application; implementations in Infrastructure.
3. Keep controllers thin — delegate to Application use cases (MediatR).
4. Prefer explicit DTOs over exposing entities directly.
5. Do not hardcode secrets, API keys, connection strings, or JWT signing keys.
6. Do not call real AI/OCR providers from tests.
7. Add packages only when a feature concretely requires them.
8. Handlers throw `UnauthorizedAccessException` for auth failures and `InvalidOperationException` for business conflicts; `ValidationBehavior` throws `ValidationException` for input errors. The global exception middleware maps these to proper HTTP status codes.
