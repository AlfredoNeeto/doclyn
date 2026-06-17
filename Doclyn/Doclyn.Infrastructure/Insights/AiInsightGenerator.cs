using System.ClientModel;
using System.Text;
using System.Text.Json;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Insights;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Doclyn.Infrastructure.Insights;

public sealed class AiInsightGenerator : IAiInsightGenerator
{
    private static readonly BinaryData InsightsSchema = BinaryData.FromString(
        """
        {
          "type": "object",
          "properties": {
            "insights": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "type": { "type": "string", "enum": ["RiskMentioned", "ActionRequired", "GenericObservation", "LowConfidenceField", "MissingRequiredField", "HighValueDocument", "LegalDeadlineMentioned", "ContractExpired", "ContractExpiringSoon"] },
                  "severity": { "type": "string", "enum": ["Info", "Warning", "Critical"] },
                  "title": { "type": "string" },
                  "message": { "type": "string" },
                  "confidence": { "type": "number" },
                  "relatedFieldName": { "type": ["string", "null"] }
                },
                "required": ["type", "severity", "title", "message", "confidence"],
                "additionalProperties": false
              }
            }
          },
          "required": ["insights"],
          "additionalProperties": false
        }
        """);

    private static readonly Dictionary<string, DocumentInsightType> TypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RiskMentioned"] = DocumentInsightType.RiskMentioned,
        ["ActionRequired"] = DocumentInsightType.ActionRequired,
        ["GenericObservation"] = DocumentInsightType.GenericObservation,
        ["LowConfidenceField"] = DocumentInsightType.LowConfidenceField,
        ["MissingRequiredField"] = DocumentInsightType.MissingRequiredField,
        ["HighValueDocument"] = DocumentInsightType.HighValueDocument,
        ["LegalDeadlineMentioned"] = DocumentInsightType.LegalDeadlineMentioned,
        ["ContractExpired"] = DocumentInsightType.ContractExpired,
        ["ContractExpiringSoon"] = DocumentInsightType.ContractExpiringSoon,
    };

    private static readonly Dictionary<string, DocumentInsightSeverity> SeverityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Info"] = DocumentInsightSeverity.Info,
        ["Warning"] = DocumentInsightSeverity.Warning,
        ["Critical"] = DocumentInsightSeverity.Critical,
    };

    private readonly OpenAiClientFactory _clientFactory;
    private readonly OpenAiOptions _options;
    private readonly ILogger<AiInsightGenerator> _logger;

    public AiInsightGenerator(
        OpenAiClientFactory clientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<AiInsightGenerator> logger)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<DocumentInsightResult>> GenerateAsync(
        string documentText,
        ExtractedDocumentData extractedData,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentText);
        ArgumentNullException.ThrowIfNull(extractedData);

        _logger.LogInformation("AiInsightsStarted for document {DocumentId}.", extractedData.DocumentId);

        try
        {
            var client = _clientFactory.CreateChatClient();
            var systemPrompt = BuildInsightPrompt(extractedData);

            List<ChatMessage> messages =
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"DOCUMENT_TEXT:\n{documentText}")
            ];

            ChatCompletionOptions options = new()
            {
                Temperature = _options.Temperature,
                MaxOutputTokenCount = _options.MaxTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "document_insights",
                    jsonSchema: InsightsSchema,
                    jsonSchemaIsStrict: true)
            };

            ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages, options, cancellationToken);
            var content = result.Value.Content.FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("OpenAI insights returned empty content.");

            var insights = ParseInsights(content, extractedData.DocumentId);

            if (insights.Count == 0)
            {
                _logger.LogInformation("AiInsightsEmpty: AI returned no valid insights for document {DocumentId}.", extractedData.DocumentId);
            }
            else
            {
                _logger.LogInformation("AiInsightsGenerated: {Count} AI insights for document {DocumentId}.", insights.Count, extractedData.DocumentId);
            }

            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiInsightsFailed for document {DocumentId}.", extractedData.DocumentId);
            return [];
        }
    }

    private static string BuildInsightPrompt(ExtractedDocumentData extractedData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are analyzing document extraction results for quality, risks, and actionable insights.");
        sb.AppendLine($"Document type: {extractedData.DocumentType}");
        sb.AppendLine();
        sb.AppendLine("Extracted fields:");
        sb.AppendLine();

        foreach (var (fieldName, field) in extractedData.Fields)
        {
            sb.AppendLine($"- {fieldName}: value={field.Value ?? "null"}, confidence={field.Confidence}, status={field.ValidationStatus}");
        }

        sb.AppendLine();
        sb.AppendLine("Generate insights about:");
        sb.AppendLine("- Fields with low confidence that may need review");
        sb.AppendLine("- Important fields that were not extracted");
        sb.AppendLine("- Risks or action items mentioned in the document");
        sb.AppendLine("- Deadlines or dates that require attention");
        sb.AppendLine("- High-value amounts that need verification");
        sb.AppendLine("- Any other observation valuable for document review");
        sb.AppendLine();
        sb.AppendLine("Return at most 5 insights. Only return insights backed by the provided extracted fields or the document text.");
        sb.AppendLine("Do not invent or guess fields that are not present in the data.");

        return sb.ToString();
    }

    private static IReadOnlyCollection<DocumentInsightResult> ParseInsights(string json, Guid documentId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("insights", out var insightsElement)
            || insightsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<DocumentInsightResult>();
        }

        var results = new List<DocumentInsightResult>();

        foreach (var item in insightsElement.EnumerateArray())
        {
            try
            {
                var typeStr = item.GetProperty("type").GetString();
                var severityStr = item.GetProperty("severity").GetString();
                var title = item.GetProperty("title").GetString() ?? string.Empty;
                var message = item.GetProperty("message").GetString() ?? string.Empty;
                var confidence = item.GetProperty("confidence").GetDecimal();
                var relatedFieldName = item.TryGetProperty("relatedFieldName", out var related)
                    && related.ValueKind != JsonValueKind.Null
                    ? related.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(typeStr) || string.IsNullOrWhiteSpace(severityStr))
                    continue;

                if (!TypeMap.TryGetValue(typeStr, out var type))
                    continue;

                if (!SeverityMap.TryGetValue(severityStr, out var severity))
                    continue;

                if (confidence < 0 || confidence > 1)
                    confidence = 0.8m;

                results.Add(new DocumentInsightResult(
                    type,
                    severity,
                    title,
                    message,
                    confidence,
                    DocumentInsightSource.AI,
                    relatedFieldName));
            }
            catch (KeyNotFoundException)
            {
            }
        }

        return results;
    }
}
