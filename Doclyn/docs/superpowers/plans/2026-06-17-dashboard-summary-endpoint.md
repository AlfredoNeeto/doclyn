# Dashboard Summary Endpoint Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `GET /api/dashboard/summary` to return scoped dashboard KPIs, quality indicators, recent documents, class usage, and attention-required items for the current user.

**Architecture:** Implement a new application query under `Doclyn.Application/Dashboard/GetSummary` with DTOs dedicated to the dashboard contract and a single handler that applies authorization scope once, then runs narrow aggregate queries over `Documents`, `ExtractedData`, `DocumentInsights`, and `DocumentClasses`. Keep JSONB parsing for quality metrics inside the handler or a small internal helper because the task explicitly allows initial aggregation in application code and the current extracted fields live only in `ExtractedData.Data`.

**Tech Stack:** ASP.NET Core, MediatR, EF Core 10, Npgsql JSONB, xUnit, NSubstitute.

## Global Constraints

- Keep Clean Architecture: controller thin, MediatR query in Application, EF querying through `IApplicationDbContext`.
- Endpoint path must be `GET /api/dashboard/summary` and must require JWT.
- `Operator` sees only data from accessible documents (`DOCUMENTS.USER_ID = currentUserId`).
- `Admin` sees global data.
- Reuse the existing soft-delete behavior on `Document`; dashboard queries must use the default filtered `Documents` set and must not opt out with `IgnoreQueryFilters()`.
- Do not expose `StoragePath`, raw JSONB payloads, MinIO internals, tokens, or other sensitive internal fields.
- For MVP, do not add caching.
- Prefer aggregated queries (`CountAsync`, `GroupBy`, `Select`, `Take`, `OrderByDescending`) and avoid loading full document graphs.
- `averageConfidence`, `fieldsValidated`, `fieldsNeedsReview`, and `fieldsRejected` must be computed from `ExtractedData.Data` in application code because the extracted fields are currently persisted only in JSONB.
- Leave `GET /api/dashboard/metrics` out of scope; do not implement it.
- Logging requirement (`DashboardSummaryRequested`, `DashboardSummaryGenerated`, `DashboardSummaryFailed`) should be satisfied with structured `ILogger<GetDashboardSummaryHandler>` logs, not `ProcessingLog`, because this is a read-only dashboard concern and `ProcessingLog` is document-specific.
- The existing `DocumentClasses/GetTop` flow counts `DocumentClassExamples`; for dashboard “most used classes” the count must be based on scoped `Documents` joined with their extracted classification payload or other authoritative current document classification source, not examples.
- Assumption for this plan: `classes.total` means total active configured document classes in `DOCUMENT_CLASSES`, while `classes.mostUsed` is scoped by visible documents. If product wants `classes.total` scoped to user-owned usage instead, adjust only Task 3 aggregate logic.

---

## File Map

### New files

- `Doclyn.Application/Dashboard/GetSummary/GetDashboardSummaryQuery.cs`
  - MediatR query contract.
- `Doclyn.Application/Dashboard/GetSummary/GetDashboardSummaryHandler.cs`
  - Main orchestration handler with scope filter, aggregate queries, JSON parsing helpers, and structured logs.
- `Doclyn.Application/Dashboard/GetSummary/DashboardSummaryResponse.cs`
  - Root response DTO.
- `Doclyn.Application/Dashboard/GetSummary/DocumentsSummaryResponse.cs`
  - Document counts block.
- `Doclyn.Application/Dashboard/GetSummary/QualitySummaryResponse.cs`
  - Quality block.
- `Doclyn.Application/Dashboard/GetSummary/InsightsSummaryResponse.cs`
  - Insights counts block.
- `Doclyn.Application/Dashboard/GetSummary/ClassesSummaryResponse.cs`
  - Class totals + top usage block.
- `Doclyn.Application/Dashboard/GetSummary/DashboardClassUsageResponse.cs`
  - Most-used class item DTO.
- `Doclyn.Application/Dashboard/GetSummary/RecentDocumentResponse.cs`
  - Recent document row DTO.
- `Doclyn.Application/Dashboard/GetSummary/AttentionRequiredResponse.cs`
  - Attention item DTO.
- `Doclyn.Api/Controllers/DashboardController.cs`
  - Thin authenticated controller.
- `Doclyn.UnitTests/Dashboard/GetSummary/GetDashboardSummaryHandlerTests.cs`
  - Unit tests for scoping, aggregation, limiting, and prioritization.
- `Doclyn.IntegrationTests/Dashboard/DashboardControllerTests.cs`
  - End-to-end HTTP tests for JWT requirement and response shape.

### Modified files

- `Doclyn.Application/Common/Interfaces/IApplicationDbContext.cs`
  - No changes expected if current sets are sufficient; verify before editing.
- `Doclyn.Api/Program.cs`
  - No changes expected beyond automatic controller discovery; verify before editing.
- `Doclyn.Application/DependencyInjection.cs`
  - Verify MediatR/validator assembly scanning already covers the new query namespace. No edit expected if scanning whole assembly.

---

### Task 1: Add the dashboard query surface and controller

**Files:**
- Create: `Doclyn.Application/Dashboard/GetSummary/GetDashboardSummaryQuery.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/DashboardSummaryResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/DocumentsSummaryResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/QualitySummaryResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/InsightsSummaryResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/ClassesSummaryResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/DashboardClassUsageResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/RecentDocumentResponse.cs`
- Create: `Doclyn.Application/Dashboard/GetSummary/AttentionRequiredResponse.cs`
- Create: `Doclyn.Api/Controllers/DashboardController.cs`
- Test: `Doclyn.IntegrationTests/Dashboard/DashboardControllerTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record GetDashboardSummaryQuery() : IRequest<DashboardSummaryResponse>;`
  - `public sealed record DashboardSummaryResponse(...);`
  - `public sealed record DocumentsSummaryResponse(int Total, int Pending, int Processing, int Processed, int Failed);`
  - `public sealed record QualitySummaryResponse(decimal AverageConfidence, int FieldsValidated, int FieldsNeedsReview, int FieldsRejected);`
  - `public sealed record InsightsSummaryResponse(int Total, int Critical, int Warning, int Info, int Success);`
  - `public sealed record ClassesSummaryResponse(int Total, IReadOnlyList<DashboardClassUsageResponse> MostUsed);`
  - `public sealed record DashboardClassUsageResponse(Guid Id, string Name, string DisplayName, int DocumentsCount);`
  - `public sealed record RecentDocumentResponse(Guid Id, string FileName, string DocumentStatus, string? DocumentClass, decimal? AverageConfidence, int InsightsCount, int NeedsReviewCount, DateTime CreatedAt);`
  - `public sealed record AttentionRequiredResponse(Guid DocumentId, string FileName, string Reason, string Severity, DateTime CreatedAt);`

- [ ] **Step 1: Write the failing integration test for endpoint existence and auth**

Add tests in `Doclyn.IntegrationTests/Dashboard/DashboardControllerTests.cs`:

```csharp
[Fact]
public async Task GetSummary_Without_Jwt_Returns_401()
{
    var response = await _client.GetAsync("/api/dashboard/summary");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task GetSummary_With_Jwt_Returns_All_Top_Level_Blocks()
{
    var user = TestAuthHelper.CreateOperator("dashboard@doclyn.local");
    await SeedUserAsync(user);
    Authenticate(user);

    var response = await _client.GetAsync("/api/dashboard/summary");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var payload = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
    Assert.NotNull(payload);
    Assert.NotNull(payload.Documents);
    Assert.NotNull(payload.Quality);
    Assert.NotNull(payload.Insights);
    Assert.NotNull(payload.Classes);
    Assert.NotNull(payload.RecentDocuments);
    Assert.NotNull(payload.AttentionRequired);
}
```

- [ ] **Step 2: Run only the new integration test and verify RED**

Run:

```bash
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj --filter "FullyQualifiedName~DashboardControllerTests"
```

Expected: fail with `404` or missing `DashboardSummaryResponse` types.

- [ ] **Step 3: Add the DTOs and controller shell**

Create the DTOs and controller with this minimal shape:

```csharp
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(response);
    }
}
```

- [ ] **Step 4: Add a temporary handler stub returning empty-but-valid blocks**

Create `GetDashboardSummaryHandler.cs` temporarily returning zeros and empty lists so the endpoint can compile and respond:

```csharp
public async Task<DashboardSummaryResponse> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
{
    await Task.CompletedTask;

    return new DashboardSummaryResponse(
        new DocumentsSummaryResponse(0, 0, 0, 0, 0),
        new QualitySummaryResponse(0m, 0, 0, 0),
        new InsightsSummaryResponse(0, 0, 0, 0, 0),
        new ClassesSummaryResponse(0, []),
        [],
        []);
}
```

- [ ] **Step 5: Run the dashboard integration tests and verify GREEN for endpoint shape**

Run:

```bash
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj --filter "FullyQualifiedName~DashboardControllerTests"
```

Expected: auth and response-shape tests pass, data-specific tests still missing.

---

### Task 2: Implement scoped document and insight aggregates

**Files:**
- Modify: `Doclyn.Application/Dashboard/GetSummary/GetDashboardSummaryHandler.cs`
- Test: `Doclyn.UnitTests/Dashboard/GetSummary/GetDashboardSummaryHandlerTests.cs`

**Interfaces:**
- Consumes:
  - `IApplicationDbContext.Documents`
  - `IApplicationDbContext.DocumentInsights`
  - `ICurrentUserService`
- Produces:
  - A handler that applies one document scope for all downstream aggregates.

- [ ] **Step 1: Write failing unit tests for operator/admin scope and status counters**

Add tests like:

```csharp
[Fact]
public async Task Operator_Sees_Only_Own_Document_Counts() { ... }

[Fact]
public async Task Admin_Sees_Global_Document_Counts() { ... }
```

Seed documents with mixed owners and statuses:
- `Pending`
- `Processing`
- `Processed`
- `Failed`

Assert expected `DocumentsSummaryResponse` values.

- [ ] **Step 2: Run the unit tests and verify RED**

Run:

```bash
dotnet test Doclyn.UnitTests/Doclyn.UnitTests.csproj --filter "FullyQualifiedName~GetDashboardSummaryHandlerTests"
```

Expected: fail because counts are all zero.

- [ ] **Step 3: Implement scoped document query and status counters**

Inside the handler, create a reusable scoped document query:

```csharp
var scopedDocuments = _context.Documents.AsNoTracking();

if (_currentUserService.Role != UserRole.Admin.ToString())
{
    scopedDocuments = scopedDocuments.Where(d => d.UserId == _currentUserService.UserId!.Value);
}
```

Then compute:

```csharp
var total = await scopedDocuments.CountAsync(cancellationToken);
var pending = await scopedDocuments.CountAsync(d => d.DocumentStatus == DocumentStatus.Pending, cancellationToken);
var processing = await scopedDocuments.CountAsync(d => d.DocumentStatus == DocumentStatus.Processing, cancellationToken);
var processed = await scopedDocuments.CountAsync(d => d.DocumentStatus == DocumentStatus.Processed, cancellationToken);
var failed = await scopedDocuments.CountAsync(d => d.DocumentStatus == DocumentStatus.Failed, cancellationToken);
```

Implement insight counts using a join or scoped document IDs, not global `DocumentInsights`.

- [ ] **Step 4: Add structured handler logs**

Inject `ILogger<GetDashboardSummaryHandler>` and log:

```csharp
_logger.LogInformation("DashboardSummaryRequested for {Role} {UserId}", _currentUserService.Role, _currentUserService.UserId);
```

and on success/failure:

```csharp
_logger.LogInformation("DashboardSummaryGenerated for {Role} {UserId}", ...);
_logger.LogError(ex, "DashboardSummaryFailed for {Role} {UserId}", ...);
```

- [ ] **Step 5: Re-run the handler tests and verify GREEN**

Run:

```bash
dotnet test Doclyn.UnitTests/Doclyn.UnitTests.csproj --filter "FullyQualifiedName~GetDashboardSummaryHandlerTests"
```

Expected: scope and status count tests pass.

---

### Task 3: Implement quality metrics and recent documents from JSONB-backed extracted data

**Files:**
- Modify: `Doclyn.Application/Dashboard/GetSummary/GetDashboardSummaryHandler.cs`
- Test: `Doclyn.UnitTests/Dashboard/GetSummary/GetDashboardSummaryHandlerTests.cs`

**Interfaces:**
- Consumes:
  - `IApplicationDbContext.ExtractedData`
  - `IApplicationDbContext.DocumentInsights`
  - `ValidationStatus` string values in JSON (`Validated`, `NeedsReview`, `Rejected`)
- Produces:
  - `QualitySummaryResponse`
  - `RecentDocumentResponse`

- [ ] **Step 1: Write failing tests for quality aggregation and recent document limit**

Cover:
- `averageConfidence` averages all field confidences found under `fields.*.confidence`
- validated/review/rejected counts come from `fields.*.validationStatus`
- recent documents are limited to `5`

Seed `ExtractedData` JSON like:

```json
{
  "classification": { "documentType": "RELATORIO_TECNICO_PRELIMINAR" },
  "fields": {
    "numeroProcesso": { "confidence": 1.0, "validationStatus": "Validated" },
    "orgao": { "confidence": 0.80, "validationStatus": "NeedsReview" },
    "assunto": { "confidence": 0.65, "validationStatus": "Rejected" }
  }
}
```

- [ ] **Step 2: Run the tests and verify RED**

Run the same dashboard unit test filter. Expected: quality metrics remain zero or recent list unbounded.

- [ ] **Step 3: Implement JSON parsing helpers with narrow projection**

Do not load full document graphs. Query only the fields needed:

```csharp
var scopedExtractedData = await _context.ExtractedData
    .AsNoTracking()
    .Where(ed => scopedDocumentIds.Contains(ed.DocumentId))
    .Select(ed => new { ed.DocumentId, ed.Data })
    .ToListAsync(cancellationToken);
```

Then parse `fields` in memory to compute:
- total confidence sum / count
- `Validated`
- `NeedsReview`
- `Rejected`

For recent documents, project:
- `Id`
- `FileName`
- `DocumentStatus`
- `CreatedAt`

Then enrich in memory with:
- `documentClass` from extracted `classification.documentType` or `documentClass` label if present
- `averageConfidence` per document from parsed fields
- `insightsCount` via grouped counts
- `needsReviewCount` via parsed field statuses

- [ ] **Step 4: Limit recent documents to five newest items**

Use:

```csharp
.OrderByDescending(d => d.CreatedAt)
.Take(5)
```

- [ ] **Step 5: Re-run unit tests and verify GREEN**

Run:

```bash
dotnet test Doclyn.UnitTests/Doclyn.UnitTests.csproj --filter "FullyQualifiedName~GetDashboardSummaryHandlerTests"
```

Expected: quality metrics and recent limit tests pass.

---

### Task 4: Implement classes summary and attention-required prioritization

**Files:**
- Modify: `Doclyn.Application/Dashboard/GetSummary/GetDashboardSummaryHandler.cs`
- Test: `Doclyn.UnitTests/Dashboard/GetSummary/GetDashboardSummaryHandlerTests.cs`

**Interfaces:**
- Consumes:
  - `IApplicationDbContext.DocumentClasses`
  - scoped documents
  - scoped extracted data
  - scoped document insights
- Produces:
  - `ClassesSummaryResponse`
  - `AttentionRequiredResponse`

- [ ] **Step 1: Write failing tests for most-used classes and attention prioritization**

Test cases:
- `mostUsed` orders by descending visible document count
- `classes.total` equals active class count (per current plan assumption)
- `attentionRequired` limited to five
- priority order:
  1. failed documents
  2. critical insights
  3. needs-review fields

- [ ] **Step 2: Run the tests and verify RED**

Expected: no class usage data and no prioritized attention items.

- [ ] **Step 3: Implement “most used classes” based on scoped document classifications**

Do not reuse `GetTopDocumentClassesHandler` directly because it counts `DocumentClassExamples`, not current document usage. Instead:
- parse each scoped `ExtractedData.Data` for `classification.documentClassId` and/or `classification.documentType`
- group scoped documents by resolved class
- join to active `DocumentClasses`

Projected item shape:

```csharp
new DashboardClassUsageResponse(dc.Id, dc.Name, dc.DisplayName, documentsCount)
```

- [ ] **Step 4: Implement attention-required item generation**

Generate candidate items from scoped documents using clear reasons:
- failed document -> `"Document processing failed"`, severity `"Critical"`
- critical insight -> use insight title, severity `"Critical"`
- warning insight when no critical exists -> use insight title, severity `"Warning"`
- fields needing review -> `"Fields require review"`, severity `"Warning"`

Deduplicate by `DocumentId`, keeping the highest-priority reason, then order by:
- severity rank desc
- createdAt desc

Limit to 5.

- [ ] **Step 5: Re-run unit tests and verify GREEN**

Run:

```bash
dotnet test Doclyn.UnitTests/Doclyn.UnitTests.csproj --filter "FullyQualifiedName~GetDashboardSummaryHandlerTests"
```

Expected: most-used classes and attention prioritization tests pass.

---

### Task 5: Add end-to-end integration coverage and verify final behavior

**Files:**
- Create: `Doclyn.IntegrationTests/Dashboard/DashboardControllerTests.cs`

**Interfaces:**
- Consumes:
  - JWT auth via `TestAuthHelper`
  - `CustomWebApplicationFactory`
  - `DoclynDbContext`
- Produces:
  - confidence that endpoint shape and scope work against the HTTP pipeline.

- [ ] **Step 1: Write failing integration tests for operator/admin scope**

Add tests:

```csharp
[Fact]
public async Task Operator_Does_Not_See_Other_User_Documents_In_Summary() { ... }

[Fact]
public async Task Admin_Sees_Global_Summary() { ... }
```

Seed two users, multiple documents, extracted data, and insights.

- [ ] **Step 2: Run the integration tests and verify RED**

Run:

```bash
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj --filter "FullyQualifiedName~DashboardControllerTests"
```

Expected: fail on incorrect counts before handler logic is complete.

- [ ] **Step 3: Implement any remaining projection fixes discovered by integration tests**

Common likely fixes:
- recent document class label mapping
- severity bucket counts including `Success`
- operator scope leaking global insights through joins

- [ ] **Step 4: Run targeted integration tests and verify GREEN**

Run:

```bash
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj --filter "FullyQualifiedName~DashboardControllerTests"
```

Expected: all dashboard integration tests pass.

- [ ] **Step 5: Run broader verification for neighboring areas**

Run:

```bash
dotnet test Doclyn.UnitTests/Doclyn.UnitTests.csproj --filter "FullyQualifiedName~Doclyn.UnitTests.Documents|FullyQualifiedName~GetDashboardSummaryHandlerTests"
dotnet test Doclyn.IntegrationTests/Doclyn.IntegrationTests.csproj --filter "FullyQualifiedName~Doclyn.IntegrationTests.Documents|FullyQualifiedName~DashboardControllerTests"
```

Expected: dashboard changes do not break document-related behavior.

---

## Self-Review

### Spec coverage

- `GET /api/dashboard/summary` exists: covered by Task 1.
- JWT required: covered by Task 1 and Task 5 tests.
- Operator/admin scope: covered by Tasks 2 and 5.
- Document counters: Task 2.
- Quality indicators from JSONB: Task 3.
- Insights summary including `success`: Tasks 2 and 5.
- Most-used classes: Task 4.
- Recent documents limited to 5: Task 3.
- Attention-required limited and prioritized: Task 4.
- No sensitive data: DTO surface in Task 1 and review in Tasks 3/5.
- Optional metrics endpoint not implemented: explicitly constrained in header.

### Placeholder scan

- No `TBD`/`TODO` markers left.
- All new files are explicitly named.
- Query sources are identified concretely.

### Type consistency

- Query returns `DashboardSummaryResponse`.
- Recent and attention items use separate response DTOs.
- Severity strings come from enum `.ToString()` and response contract stays string-based.

## Notes for the implementer

- Prefer one handler file with a few private helper methods over introducing a new service prematurely.
- Keep the JSON parsing helper local to the handler unless tests show it needs separate unit coverage.
- Do not leak `Document.StoragePath`, `ExtractedData.Data`, or raw insight messages beyond the summarized fields defined in the DTOs.
- Reuse the current `CurrentUserService` role pattern already used throughout `Application/Documents/*`.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-17-dashboard-summary-endpoint.md`.

Two execution options:

1. Subagent-Driven (recommended) - I dispatch a fresh subagent per task, review between tasks, fast iteration
2. Inline Execution - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
