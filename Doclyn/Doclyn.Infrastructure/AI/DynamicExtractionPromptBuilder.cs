using System.Text;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;

namespace Doclyn.Infrastructure.AI;

public class DynamicExtractionPromptBuilder : IExtractionPromptBuilder
{
    public string Build(
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers,
        string documentText)
    {
        ArgumentNullException.ThrowIfNull(documentClass);
        ArgumentNullException.ThrowIfNull(indexers);

        var activeIndexers = indexers.Where(i => i.IsActive).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"You are analyzing a document of class: {documentClass.Name}");
        sb.AppendLine($"Display name: {documentClass.DisplayName}");
        sb.AppendLine($"Group: {documentClass.Group}");
        sb.AppendLine($"SubGroup: {documentClass.SubGroup}");

        if (!string.IsNullOrWhiteSpace(documentClass.Description))
        {
            sb.AppendLine($"Description: {documentClass.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("Extract ONLY the following fields from the document text:");
        sb.AppendLine();

        foreach (var indexer in activeIndexers)
        {
            var requiredLabel = indexer.IsRequired ? "required" : "optional";
            var multipleLabel = indexer.IsMultiple ? "multiple" : "single";

            sb.AppendLine($"- {indexer.Name}");
            sb.AppendLine($"  Type: {indexer.DataType}, {requiredLabel}, {multipleLabel}");

            if (!string.IsNullOrWhiteSpace(indexer.Description))
            {
                sb.AppendLine($"  Description: {indexer.Description}");
            }

            if (!string.IsNullOrWhiteSpace(indexer.ExtractionHint))
            {
                sb.AppendLine($"  Hint: {indexer.ExtractionHint}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("Instructions:");
        sb.AppendLine("- Return ONLY valid JSON matching the provided schema.");
        sb.AppendLine("- Use the exact field name as given in the schema.");
        sb.AppendLine("- Return null when you cannot find a value.");
        sb.AppendLine("- For multiple fields, return an array.");
        sb.AppendLine("- Do not invent or hallucinate values.");

        return sb.ToString();
    }
}
