using System.Text.RegularExpressions;
using Doclyn.Domain.Enums;

namespace Doclyn.Domain.Entities;

public sealed class DocumentClassIndexer : AuditableEntity
{
    public Guid DocumentClassId { get; private set; }
    public DocumentClass DocumentClass { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IndexerDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsMultiple { get; private set; }
    public string? ExtractionHint { get; private set; }
    public string? RegexPattern { get; private set; }
    public bool IsActive { get; private set; }

    private DocumentClassIndexer()
    {
    }

    public static DocumentClassIndexer Create(
        Guid documentClassId,
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint = null,
        string? regexPattern = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var normalizedName = NormalizeName(name);
        ValidateCamelCaseName(normalizedName);
        ValidateRegexPattern(regexPattern);

        return new DocumentClassIndexer
        {
            Id = Guid.NewGuid(),
            DocumentClassId = documentClassId,
            Name = normalizedName,
            DisplayName = displayName.Trim(),
            Description = description?.Trim() ?? string.Empty,
            DataType = dataType,
            IsRequired = isRequired,
            IsMultiple = isMultiple,
            ExtractionHint = extractionHint?.Trim(),
            RegexPattern = regexPattern?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint = null,
        string? regexPattern = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var normalizedName = NormalizeName(name);
        ValidateCamelCaseName(normalizedName);
        ValidateRegexPattern(regexPattern);

        Name = normalizedName;
        DisplayName = displayName.Trim();
        Description = description?.Trim() ?? string.Empty;
        DataType = dataType;
        IsRequired = isRequired;
        IsMultiple = isMultiple;
        ExtractionHint = extractionHint?.Trim();
        RegexPattern = regexPattern?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }

    private static void ValidateCamelCaseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Indexer name cannot be empty.", nameof(name));

        if (!char.IsLower(name[0]))
            throw new ArgumentException($"Indexer name '{name}' must start with a lowercase letter (camelCase).", nameof(name));

        foreach (var ch in name)
        {
            if (!char.IsLetterOrDigit(ch))
                throw new ArgumentException($"Indexer name '{name}' can only contain letters and digits (camelCase).", nameof(name));
        }
    }

    private static void ValidateRegexPattern(string? regexPattern)
    {
        if (string.IsNullOrWhiteSpace(regexPattern))
            return;

        try
        {
            _ = new Regex(regexPattern.Trim(), RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        }
        catch (RegexParseException ex)
        {
            throw new ArgumentException($"Invalid regex pattern: {ex.Message}", nameof(regexPattern), ex);
        }
    }
}
