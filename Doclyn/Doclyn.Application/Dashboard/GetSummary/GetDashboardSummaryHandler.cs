using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Doclyn.Domain.Enums;
using System.Text.Json;

namespace Doclyn.Application.Dashboard.GetSummary;

public sealed class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetDashboardSummaryHandler> _logger;

    public GetDashboardSummaryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetDashboardSummaryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DashboardSummaryResponse> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DashboardSummaryRequested UserId:{UserId} Role:{Role}",
            _currentUserService.UserId,
            _currentUserService.Role);

        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var isAdmin = string.Equals(_currentUserService.Role, UserRole.Admin.ToString(), StringComparison.Ordinal);
            var userId = _currentUserService.UserId.Value;

            var scopedDocuments = _context.Documents
                .AsNoTracking()
                .Where(d => isAdmin || d.UserId == userId);

            var documents = await scopedDocuments
                .GroupBy(_ => 1)
                .Select(group => new DocumentsSummaryResponse(
                    group.Count(),
                    group.Count(d => d.DocumentStatus == DocumentStatus.Pending),
                    group.Count(d => d.DocumentStatus == DocumentStatus.Processing),
                    group.Count(d => d.DocumentStatus == DocumentStatus.Processed),
                    group.Count(d => d.DocumentStatus == DocumentStatus.Failed)))
                .FirstOrDefaultAsync(cancellationToken)
                ?? new DocumentsSummaryResponse(0, 0, 0, 0, 0);

            var scopedInsights = from insight in _context.DocumentInsights.AsNoTracking()
                                 join document in scopedDocuments.Select(d => d.Id)
                                     on insight.DocumentId equals document
                                 select insight;

            var insights = await scopedInsights
                .GroupBy(_ => 1)
                .Select(group => new InsightsSummaryResponse(
                    group.Count(),
                    group.Count(di => di.Severity == DocumentInsightSeverity.Critical),
                    group.Count(di => di.Severity == DocumentInsightSeverity.Warning),
                    group.Count(di => di.Severity == DocumentInsightSeverity.Info),
                    group.Count(di => di.Severity == DocumentInsightSeverity.Success)))
                .FirstOrDefaultAsync(cancellationToken)
                ?? new InsightsSummaryResponse(0, 0, 0, 0, 0);

            var scopedDocumentIds = scopedDocuments.Select(d => d.Id);

            var scopedExtractedData = await _context.ExtractedData
                .AsNoTracking()
                .Where(e => scopedDocumentIds.Contains(e.DocumentId))
                .ToListAsync(cancellationToken);

            var quality = BuildQualitySummary(scopedExtractedData);

            var recentDocumentRows = await scopedDocuments
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.DocumentStatus,
                    d.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var recentDocumentIds = recentDocumentRows.Select(d => d.Id).ToArray();

            var recentExtractedData = scopedExtractedData
                .Where(e => recentDocumentIds.Contains(e.DocumentId))
                .ToDictionary(e => e.DocumentId);

            var recentInsightCounts = await _context.DocumentInsights
                .AsNoTracking()
                .Where(di => recentDocumentIds.Contains(di.DocumentId))
                .GroupBy(di => di.DocumentId)
                .Select(group => new { DocumentId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.DocumentId, item => item.Count, cancellationToken);

            var recentDocuments = recentDocumentRows
                .Select(document => BuildRecentDocumentResponse(
                    document.Id,
                    document.FileName,
                    document.DocumentStatus,
                    document.CreatedAt,
                    recentExtractedData.GetValueOrDefault(document.Id),
                    recentInsightCounts.GetValueOrDefault(document.Id)))
                .ToList();

            var classesSummary = await BuildClassesSummary(scopedExtractedData, cancellationToken);

            var attentionRequired = BuildAttentionRequired(scopedExtractedData, recentDocuments, cancellationToken);

            var response = new DashboardSummaryResponse(
                documents,
                quality,
                insights,
                classesSummary,
                recentDocuments,
                attentionRequired);

            _logger.LogInformation(
                "DashboardSummaryGenerated UserId:{UserId} Role:{Role} DocumentTotal:{DocumentTotal} InsightTotal:{InsightTotal}",
                userId,
                _currentUserService.Role,
                response.Documents.Total,
                response.Insights.Total);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DashboardSummaryFailed UserId:{UserId} Role:{Role}",
                _currentUserService.UserId,
                _currentUserService.Role);

            throw;
        }
    }

    private static QualitySummaryResponse BuildQualitySummary(IReadOnlyCollection<Domain.Entities.ExtractedData> extractedDataItems)
    {
        var totalConfidence = 0m;
        var confidenceCount = 0;
        var validatedCount = 0;
        var needsReviewCount = 0;
        var rejectedCount = 0;

        foreach (var extractedData in extractedDataItems)
        {
            var metrics = ExtractFieldMetrics(extractedData.Data);

            totalConfidence += metrics.TotalConfidence;
            confidenceCount += metrics.ConfidenceCount;
            validatedCount += metrics.ValidatedCount;
            needsReviewCount += metrics.NeedsReviewCount;
            rejectedCount += metrics.RejectedCount;
        }

        var averageConfidence = confidenceCount == 0 ? 0m : totalConfidence / confidenceCount;

        return new QualitySummaryResponse(
            averageConfidence,
            validatedCount,
            needsReviewCount,
            rejectedCount);
    }

    private static RecentDocumentResponse BuildRecentDocumentResponse(
        Guid id,
        string fileName,
        DocumentStatus documentStatus,
        DateTime createdAt,
        Domain.Entities.ExtractedData? extractedData,
        int insightsCount)
    {
        var metrics = extractedData is null
            ? FieldMetrics.Empty
            : ExtractFieldMetrics(extractedData.Data);

        return new RecentDocumentResponse(
            id,
            fileName,
            documentStatus.ToString(),
            extractedData is null ? null : ExtractDocumentClass(extractedData.Data),
            metrics.ConfidenceCount == 0 ? null : metrics.TotalConfidence / metrics.ConfidenceCount,
            insightsCount,
            metrics.NeedsReviewCount,
            createdAt);
    }

    private static FieldMetrics ExtractFieldMetrics(JsonDocument data)
    {
        if (!data.RootElement.TryGetProperty("fields", out var fieldsElement) ||
            fieldsElement.ValueKind != JsonValueKind.Object)
        {
            return FieldMetrics.Empty;
        }

        var totalConfidence = 0m;
        var confidenceCount = 0;
        var validatedCount = 0;
        var needsReviewCount = 0;
        var rejectedCount = 0;

        foreach (var field in fieldsElement.EnumerateObject())
        {
            if (field.Value.TryGetProperty("confidence", out var confidenceElement) &&
                confidenceElement.TryGetDecimal(out var confidence))
            {
                totalConfidence += confidence;
                confidenceCount++;
            }

            if (field.Value.TryGetProperty("validationStatus", out var statusElement))
            {
                switch (statusElement.GetString())
                {
                    case "Validated":
                        validatedCount++;
                        break;
                    case "NeedsReview":
                        needsReviewCount++;
                        break;
                    case "Rejected":
                        rejectedCount++;
                        break;
                }
            }
        }

        return new FieldMetrics(totalConfidence, confidenceCount, validatedCount, needsReviewCount, rejectedCount);
    }

    private static string? ExtractDocumentClass(JsonDocument data)
    {
        if (!data.RootElement.TryGetProperty("classification", out var classificationElement) ||
            classificationElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (classificationElement.TryGetProperty("documentType", out var documentTypeElement))
        {
            var documentType = documentTypeElement.GetString();
            if (!string.IsNullOrWhiteSpace(documentType))
            {
                return documentType;
            }
        }

        if (classificationElement.TryGetProperty("label", out var labelElement))
        {
            var label = labelElement.GetString();
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }
        }

        return null;
    }

    private async Task<ClassesSummaryResponse> BuildClassesSummary(
        IEnumerable<Domain.Entities.ExtractedData> extractedDataItems,
        CancellationToken cancellationToken)
    {
        var activeClassCount = await _context.DocumentClasses
            .AsNoTracking()
            .CountAsync(dc => dc.IsActive, cancellationToken);

        var classCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var extracted in extractedDataItems)
        {
            var className = ExtractDocumentClass(extracted.Data);
            if (className is null) continue;

            classCounts.TryGetValue(className, out var count);
            classCounts[className] = count + 1;
        }

        var topClasses = new List<DashboardClassUsageResponse>();
        foreach (var (className, count) in classCounts.OrderByDescending(kv => kv.Value).Take(5))
        {
            var docClass = await _context.DocumentClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(dc => dc.Name == className.ToUpperInvariant(), cancellationToken);

            topClasses.Add(new DashboardClassUsageResponse(
                docClass?.Id ?? Guid.Empty,
                className,
                docClass?.DisplayName ?? className.Replace('_', ' ').ToLowerInvariant(),
                count));
        }

        return new ClassesSummaryResponse(activeClassCount, topClasses);
    }

    private static IReadOnlyList<AttentionRequiredResponse> BuildAttentionRequired(
        IEnumerable<Domain.Entities.ExtractedData> extractedDataItems,
        IReadOnlyCollection<RecentDocumentResponse> recentDocuments,
        CancellationToken cancellationToken)
    {
        var attentionItems = new List<AttentionRequiredResponse>();

        foreach (var doc in recentDocuments)
        {
            if (doc.DocumentStatus == "Failed")
            {
                attentionItems.Add(new AttentionRequiredResponse(
                    doc.Id, doc.FileName, "Document processing failed", "Critical", doc.CreatedAt));
            }
        }

        foreach (var doc in recentDocuments)
        {
            if (doc.NeedsReviewCount > 0)
            {
                attentionItems.Add(new AttentionRequiredResponse(
                    doc.Id, doc.FileName, "Fields require review", "Warning", doc.CreatedAt));
            }
        }

        return attentionItems
            .GroupBy(a => a.DocumentId)
            .Select(g => g.OrderBy(a => a.Severity == "Critical" ? 0 : 1).First())
            .OrderBy(a => a.Severity == "Critical" ? 0 : 1)
            .ThenByDescending(a => a.CreatedAt)
            .Take(5)
            .ToList();
    }

    private readonly record struct FieldMetrics(
        decimal TotalConfidence,
        int ConfidenceCount,
        int ValidatedCount,
        int NeedsReviewCount,
        int RejectedCount)
    {
        public static FieldMetrics Empty => new(0m, 0, 0, 0, 0);
    }
}
