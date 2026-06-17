using System.Reflection;
using System.Text;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;

namespace Doclyn.Infrastructure.AI;

public class PromptBuilder
{
    private const string PromptsNamespace = "Doclyn.Infrastructure.AI.Prompts";

    public string BuildClassificationPrompt()
    {
        return LoadPrompt("classification.prompt.md")
            .Replace("{{KNOWN_DOCUMENT_TYPES}}", string.Join(", ", KnownDocumentTypes), StringComparison.Ordinal);
    }

    public string BuildExtractionPrompt(string documentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);

        return documentType switch
        {
            DocumentTypes.RelatorioTecnicoPreliminar => LoadPrompt("relatorio-tecnico.prompt.md"),
            DocumentTypes.ContratoAdministrativo => LoadPrompt("contrato.prompt.md"),
            _ => LoadPrompt("generic.prompt.md")
                .Replace("{{DOCUMENT_TYPE}}", documentType, StringComparison.Ordinal)
        };
    }

    public string BuildDynamicExtractionPrompt(
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers)
    {
        ArgumentNullException.ThrowIfNull(documentClass);
        ArgumentNullException.ThrowIfNull(indexers);

        var activeIndexers = indexers.Where(i => i.IsActive).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("You are extracting structured data from a document.");
        sb.AppendLine($"Document class: {documentClass.DisplayName}");
        sb.AppendLine($"Class name: {documentClass.Name}");
        sb.AppendLine($"Display name: {documentClass.DisplayName}");
        sb.AppendLine($"Category: {documentClass.Group} / {documentClass.SubGroup}");
        sb.AppendLine();
        sb.AppendLine("Return ONLY valid JSON matching the provided schema.");
        sb.AppendLine("For each field, return an object with two properties:");
        sb.AppendLine("- \"value\": the extracted value, or null when absent");
        sb.AppendLine("- \"confidence\": a number between 0.0 and 1.0 indicating your certainty");
        sb.AppendLine("Give high confidence (0.85+) for clearly identifiable data.");
        sb.AppendLine("Give low confidence (<0.70) when the value is uncertain or inferred.");
        sb.AppendLine("- Do not invent or hallucinate values.");
        sb.AppendLine();
        sb.AppendLine("Fields of interest:");
        sb.AppendLine();

        foreach (var indexer in activeIndexers)
        {
            var requiredLabel = indexer.IsRequired ? "required" : "optional";
            var multipleLabel = indexer.IsMultiple ? "multiple" : "single";
            var hint = string.IsNullOrWhiteSpace(indexer.ExtractionHint)
                ? string.Empty
                : $" — hint: {indexer.ExtractionHint}";

            sb.AppendLine($"- {indexer.Name}: {requiredLabel}, {multipleLabel}, type {indexer.DataType}{hint}");
        }

        return sb.ToString();
    }

    private static string LoadPrompt(string fileName)
    {
        var resourceName = $"{PromptsNamespace}.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Prompt resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public virtual string BuildSemanticClassificationPrompt(
        IReadOnlyCollection<DocumentClass> documentClasses)
    {
        ArgumentNullException.ThrowIfNull(documentClasses);

        var sb = new StringBuilder();
        sb.AppendLine("You are a specialized document classifier for the Brazilian administrative context.");
        sb.AppendLine("Your task is to classify the provided document text into ONE of the following document types.");
        sb.AppendLine();
        sb.AppendLine("Known document classes:");
        sb.AppendLine();

        foreach (var dc in documentClasses.Where(dc => dc.IsActive && dc.Name != "DOCUMENTO_DESCONHECIDO"))
        {
            sb.AppendLine($"- {dc.Name}");
            sb.AppendLine($"  Display: {dc.DisplayName}");
            sb.AppendLine($"  Category: {dc.Group} / {dc.SubGroup}");
            if (!string.IsNullOrWhiteSpace(dc.Description))
                sb.AppendLine($"  Description: {dc.Description}");
            sb.AppendLine();
        }

        sb.AppendLine("CRITICAL RULES — follow exactly:");
        sb.AppendLine("1. You MUST classify the document into one of the known classes above. Do NOT return 'DOCUMENTO_DESCONHECIDO' or 'UNKNOWN'.");
        sb.AppendLine("2. If the document clearly matches an existing class, set reuseExistingClass to true.");
        sb.AppendLine("3. If the document does not match perfectly, still pick the CLOSEST existing class and set reuseExistingClass to true.");
        sb.AppendLine("4. Only suggest a new class when you are completely certain none of the existing classes fit.");
        sb.AppendLine("5. For classification, prefer broader administrative categories over creating new narrow classes.");
        sb.AppendLine("6. When reusing an existing class, use the exact UPPER_SNAKE_CASE name as listed above.");
        sb.AppendLine("7. Return ONLY valid JSON matching the expected schema.");
        sb.AppendLine("8. Base your answer on actual content found in the document text.");
        sb.AppendLine();
        sb.AppendLine("Examples:");
        sb.AppendLine("- A document with 'PROCESSO ADMINISTRATIVO', 'RELATORIO TECNICO', 'CONTRATO' → RELATORIO_TECNICO_PRELIMINAR");
        sb.AppendLine("- A document with 'CONTRATO', 'CLAUSULA', 'VIGENCIA', 'CONTRATANTE' → CONTRATO_ADMINISTRATIVO");
        sb.AppendLine("- A document with 'OFICIO', 'ENCAMINHAMOS', 'SOLICITAMOS' → OFICIO");
        sb.AppendLine("- A document with 'NOTA FISCAL', 'DANFE', 'EMISSAO', 'ICMS' → NOTA_FISCAL");
        sb.AppendLine("- A document with 'EXCELENTISSIMO', 'MERITISSIMO', 'REQUER', 'TUTELA' → PETICAO_JUDICIAL");

        return sb.ToString();
    }

    private static IReadOnlyCollection<string> KnownDocumentTypes =>
    [
        DocumentTypes.RelatorioTecnicoPreliminar,
        DocumentTypes.ContratoAdministrativo,
        DocumentTypes.Oficio,
        DocumentTypes.NotaFiscal,
        DocumentTypes.PeticaoJudicial,
        DocumentTypes.DocumentoDesconhecido
    ];
}
