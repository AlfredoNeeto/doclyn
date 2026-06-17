using Doclyn.Domain.Enums;

namespace Doclyn.Domain.Entities;

public sealed class ProcessingLog : BaseEntity
{
    public Guid DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;
    public string Step { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core requer construtor sem parâmetros
    private ProcessingLog()
    {
    }

    public static ProcessingLog Create(
        Guid documentId,
        string step,
        string message,
        DocumentStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(step);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new ProcessingLog
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Step = step.Trim(),
            Message = message.Trim(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
