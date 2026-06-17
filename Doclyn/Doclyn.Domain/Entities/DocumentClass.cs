namespace Doclyn.Domain.Entities;

public sealed class DocumentClass : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty;
    public string SubGroup { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemDefined { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<DocumentClassIndexer> Indexers { get; private set; } = [];

    private DocumentClass()
    {
    }

    public static DocumentClass Create(
        string name,
        string group,
        string subGroup,
        string description = "",
        bool isSystemDefined = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = NormalizeName(name);

        return new DocumentClass
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            DisplayName = DeriveDisplayName(normalizedName),
            Group = NormalizeNullable(group),
            SubGroup = NormalizeNullable(subGroup),
            Description = description?.Trim() ?? string.Empty,
            IsSystemDefined = isSystemDefined,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim().ToUpperInvariant();
    }

    private static string DeriveDisplayName(string normalizedName)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
            return normalizedName;

        return normalizedName
            .Replace('_', ' ')
            .ToLowerInvariant();
    }

    private static string NormalizeNullable(string? value)
    {
        return value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
