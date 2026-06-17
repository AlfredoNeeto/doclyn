using Doclyn.Domain.Enums;

namespace Doclyn.Domain.Entities;

public sealed class DocumentInsight : BaseEntity
{
    public Guid DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;
    public DocumentInsightType Type { get; private set; }
    public DocumentInsightSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public decimal Confidence { get; private set; }
    public DocumentInsightSource Source { get; private set; }
    public string? RelatedFieldName { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentInsight()
    {
    }

    public static DocumentInsight Create(
        Guid documentId,
        DocumentInsightType type,
        DocumentInsightSeverity severity,
        string title,
        string message,
        decimal confidence,
        DocumentInsightSource source,
        string? relatedFieldName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (confidence < 0 || confidence > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");

        return new DocumentInsight
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Type = type,
            Severity = severity,
            Title = title.Trim(),
            Message = message.Trim(),
            Confidence = confidence,
            Source = source,
            RelatedFieldName = relatedFieldName?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
