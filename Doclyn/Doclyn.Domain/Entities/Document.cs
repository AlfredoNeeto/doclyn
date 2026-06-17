using Doclyn.Domain.Constants;
using Doclyn.Domain.Enums;

namespace Doclyn.Domain.Entities;

public sealed class Document : SoftDeletableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string FileName { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = DocumentTypes.Unknown;
    public DocumentStatus DocumentStatus { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // EF Core requer construtor sem parâmetros
    private Document()
    {
    }

    public static Document Create(
        Guid userId,
        string fileName,
        string fileHash,
        string storagePath,
        string documentType = DocumentTypes.Unknown,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);

        return new Document
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            FileName = fileName.Trim(),
            FileHash = fileHash,
            StoragePath = storagePath.Trim(),
            DocumentType = documentType.Trim(),
            DocumentStatus = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(DocumentStatus status)
    {
        DocumentStatus = status;
        UpdatedAt = DateTime.UtcNow;

        if (status == DocumentStatus.Processed)
        {
            ProcessedAt = DateTime.UtcNow;
        }
    }

    public void UpdateDocumentType(string documentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);
        DocumentType = documentType.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
