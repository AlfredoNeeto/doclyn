using System.Text.Json;

namespace Doclyn.Domain.Entities;

public sealed class ExtractedData : AuditableEntity
{
    public Guid DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;
    public JsonDocument Data { get; private set; } = JsonDocument.Parse("{}");

    // EF Core requer construtor sem parâmetros
    private ExtractedData()
    {
    }

    public static ExtractedData Create(Guid documentId, JsonDocument data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new ExtractedData
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Data = data,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateData(JsonDocument data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
        UpdatedAt = DateTime.UtcNow;
    }
}
