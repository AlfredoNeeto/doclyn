using System.Text.Json;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;

namespace Doclyn.Infrastructure.AI;

public sealed class DocumentClassAiSchemaBuilder
{
    public BinaryData Build(
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers)
    {
        ArgumentNullException.ThrowIfNull(documentClass);
        ArgumentNullException.ThrowIfNull(indexers);

        var activeIndexers = indexers.Where(i => i.IsActive).ToList();

        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var required = new List<string>();

        foreach (var indexer in activeIndexers)
        {
            properties[indexer.Name] = BuildPropertySchema(indexer);
            required.Add(indexer.Name);
        }

        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required.ToArray(),
            ["additionalProperties"] = false
        };

        return BinaryData.FromString(JsonSerializer.Serialize(schema));
    }

    private static object BuildPropertySchema(DocumentClassIndexer indexer)
    {
        var baseType = MapDataType(indexer.DataType);

        var fieldSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["value"] = indexer.IsMultiple
                    ? new Dictionary<string, object?>
                    {
                        ["type"] = "array",
                        ["items"] = baseType
                    }
                    : baseType,
                ["confidence"] = new Dictionary<string, object?>
                {
                    ["type"] = "number"
                }
            },
            ["required"] = new[] { "value", "confidence" },
            ["additionalProperties"] = false
        };

        return fieldSchema;
    }

    private static object MapDataType(IndexerDataType dataType)
    {
        return dataType switch
        {
            IndexerDataType.Text or
            IndexerDataType.Cpf or
            IndexerDataType.Cnpj or
            IndexerDataType.Email or
            IndexerDataType.Phone or
            IndexerDataType.Cep or
            IndexerDataType.Currency or
            IndexerDataType.Date => new Dictionary<string, object?>
            {
                ["type"] = new[] { "string", "null" }
            },
            IndexerDataType.Number or IndexerDataType.Decimal => new Dictionary<string, object?>
            {
                ["type"] = new[] { "number", "null" }
            },
            IndexerDataType.Boolean => new Dictionary<string, object?>
            {
                ["type"] = new[] { "boolean", "null" }
            },
            IndexerDataType.Object => new Dictionary<string, object?>
            {
                ["type"] = new[] { "object", "null" },
                ["additionalProperties"] = true
            },
            IndexerDataType.Array => new Dictionary<string, object?>
            {
                ["type"] = "array",
                ["items"] = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                }
            },
            _ => new Dictionary<string, object?>
            {
                ["type"] = new[] { "string", "null" }
            }
        };
    }
}
