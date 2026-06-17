using System.ClientModel;
using System.Text.Json;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Doclyn.Infrastructure.AI;

public sealed class OpenAiDocumentClassifier : IAiDocumentClassifier
{
    private static readonly BinaryData ClassificationSchema = BinaryData.FromString(
        """
        {
          "type": "object",
          "properties": {
            "documentType": { "type": "string" },
            "group": { "type": "string" },
            "subGroup": { "type": "string" },
            "confidence": { "type": ["number", "null"] }
          },
          "required": ["documentType", "group", "subGroup", "confidence"],
          "additionalProperties": false
        }
        """);

    private static readonly HashSet<string> KnownDocumentTypes =
    [
        DocumentTypes.RelatorioTecnicoPreliminar,
        DocumentTypes.ContratoAdministrativo,
        DocumentTypes.Oficio,
        DocumentTypes.NotaFiscal,
        DocumentTypes.PeticaoJudicial,
        DocumentTypes.DocumentoDesconhecido
    ];

    private readonly OpenAiClientFactory _clientFactory;
    private readonly PromptBuilder _promptBuilder;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiDocumentClassifier> _logger;

    public OpenAiDocumentClassifier(
        OpenAiClientFactory clientFactory,
        PromptBuilder promptBuilder,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiDocumentClassifier> logger)
    {
        _clientFactory = clientFactory;
        _promptBuilder = promptBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocumentClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var client = _clientFactory.CreateChatClient();
        List<ChatMessage> messages =
        [
            new SystemChatMessage(_promptBuilder.BuildClassificationPrompt()),
            new UserChatMessage($"DOCUMENT_TEXT:\n{text}")
        ];

        ChatCompletionOptions options = new()
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = _options.MaxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "document_classification",
                jsonSchema: ClassificationSchema,
                jsonSchemaIsStrict: true)
        };

        ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages, options, cancellationToken);
        var content = result.Value.Content.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("OpenAI classification returned empty content.");

        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        var documentType = NormalizeDocumentType(root.GetProperty("documentType").GetString());
        var group = root.GetProperty("group").GetString()?.Trim() ?? "UNKNOWN";
        var subgroup = root.GetProperty("subGroup").GetString()?.Trim() ?? "UNKNOWN";
        double? confidence = root.GetProperty("confidence").ValueKind == JsonValueKind.Null
            ? null
            : root.GetProperty("confidence").GetDouble();

        _logger.LogInformation("AI classified document as {DocumentType}", documentType);

        return new DocumentClassificationResult(documentType, group, subgroup, confidence);
    }

    private static string NormalizeDocumentType(string? documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            return DocumentTypes.DocumentoDesconhecido;
        }

        var normalized = documentType.Trim().ToUpperInvariant();
        return KnownDocumentTypes.Contains(normalized)
            ? normalized
            : DocumentTypes.DocumentoDesconhecido;
    }
}
