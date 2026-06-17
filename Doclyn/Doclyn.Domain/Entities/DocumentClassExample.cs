namespace Doclyn.Domain.Entities;

public sealed class DocumentClassExample : BaseEntity
{
    public Guid DocumentClassId { get; private set; }
    public DocumentClass DocumentClass { get; private set; } = null!;

    public Guid DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;

    public decimal Confidence { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentClassExample()
    {
    }

    public static DocumentClassExample Create(
        Guid documentClassId,
        Guid documentId,
        decimal confidence)
    {
        if (documentClassId == Guid.Empty)
            throw new ArgumentException("DocumentClassId is required.", nameof(documentClassId));

        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        if (confidence < 0 || confidence > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");

        return new DocumentClassExample
        {
            Id = Guid.NewGuid(),
            DocumentClassId = documentClassId,
            DocumentId = documentId,
            Confidence = confidence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
