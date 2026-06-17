using System.Text.Json;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;
using Doclyn.Application.Documents.Insights;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.GenerateInsights;

public sealed class GenerateDocumentInsightsHandler : IRequestHandler<GenerateDocumentInsightsCommand, GenerateDocumentInsightsResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentInsightService _documentInsightService;

    public GenerateDocumentInsightsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentInsightService documentInsightService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentInsightService = documentInsightService;
    }

    public async Task<GenerateDocumentInsightsResponse> Handle(
        GenerateDocumentInsightsCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
            throw new NotFoundException("Document not found.");

        if (_currentUserService.Role != UserRole.Admin.ToString()
            && document.UserId != _currentUserService.UserId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        if (document.DocumentStatus != DocumentStatus.Processed
            && document.DocumentStatus != DocumentStatus.Failed)
            throw new InvalidOperationException("Document must be processed before generating insights.");

        var extractedDataEntity = await _context.ExtractedData
            .FirstOrDefaultAsync(e => e.DocumentId == request.DocumentId, cancellationToken);

        if (extractedDataEntity is null)
            throw new InvalidOperationException("No extracted data found. Process the document first.");

        var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase);
        if (extractedDataEntity.Data.RootElement.TryGetProperty("fields", out var fieldsElement))
        {
            foreach (var field in fieldsElement.EnumerateObject())
            {
                var value = field.Value.TryGetProperty("value", out var v) ? ParseJsonValue(v) : null;
                var confidence = field.Value.TryGetProperty("confidence", out var c) ? c.GetDecimal() : 0m;
                var source = field.Value.TryGetProperty("source", out var s) ? s.GetString() ?? "AI" : "AI";
                var status = field.Value.TryGetProperty("validationStatus", out var st)
                    && Enum.TryParse<ValidationStatus>(st.GetString(), out var parsedStatus)
                    ? parsedStatus
                    : ValidationStatus.NeedsReview;

                var extractionSource = source switch
                {
                    "Regex" => Domain.Enums.ExtractionSource.Regex,
                    "AI" => Domain.Enums.ExtractionSource.AI,
                    "Manual" => Domain.Enums.ExtractionSource.Manual,
                    _ => Domain.Enums.ExtractionSource.Merged,
                };

                fields[field.Name] = new ExtractedFieldResult(value, confidence, extractionSource, status);
            }
        }

        var docClassId = extractedDataEntity.Data.RootElement.TryGetProperty("classification", out var classification)
            && classification.TryGetProperty("documentClassId", out var classId)
            && classId.ValueKind == JsonValueKind.String
            ? Guid.Parse(classId.GetString()!)
            : (Guid?)null;

        var docType = classification.TryGetProperty("documentType", out var dt)
            ? dt.GetString() ?? "UNKNOWN"
            : "UNKNOWN";

        var extractedData = new ExtractedDocumentData(
            request.DocumentId,
            docClassId,
            docType,
            fields,
            null);

        var insights = await _documentInsightService.GenerateAsync(
            request.DocumentId,
            extractedData,
            cancellationToken);

        var existingInsights = _context.DocumentInsights
            .Where(di => di.DocumentId == request.DocumentId);
        foreach (var existing in existingInsights)
            _context.DocumentInsights.Remove(existing);

        foreach (var insight in insights)
        {
            _context.DocumentInsights.Add(DocumentInsight.Create(
                request.DocumentId,
                insight.Type,
                insight.Severity,
                insight.Title,
                insight.Message,
                insight.Confidence,
                insight.Source,
                insight.RelatedFieldName));
        }

        _context.ProcessingLogs.Add(ProcessingLog.Create(
            request.DocumentId,
            "InsightGenerationCompleted",
            $"Manually regenerated {insights.Count} insights.",
            DocumentStatus.Success));

        await _context.SaveChangesAsync(cancellationToken);

        return new GenerateDocumentInsightsResponse(request.DocumentId, insights.Count);
    }

    private static object? ParseJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
