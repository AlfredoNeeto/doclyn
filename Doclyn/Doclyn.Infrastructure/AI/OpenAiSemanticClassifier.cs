using System.ClientModel;
using System.Text.Json;
using Doclyn.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Doclyn.Infrastructure.AI;

public class OpenAiSemanticClassifier
{
    private static readonly BinaryData SemanticSchema = BinaryData.FromString(
        """
        {
          "type": "object",
          "properties": {
            "documentClassName": { "type": "string" },
            "group": { "type": "string" },
            "subGroup": { "type": "string" },
            "reuseExistingClass": { "type": "boolean" },
            "confidence": { "type": "number" }
          },
          "required": ["documentClassName", "group", "subGroup", "reuseExistingClass", "confidence"],
          "additionalProperties": false
        }
        """);

    private readonly OpenAiClientFactory _clientFactory;
    private readonly PromptBuilder _promptBuilder;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiSemanticClassifier> _logger;

    public OpenAiSemanticClassifier(
        OpenAiClientFactory clientFactory,
        PromptBuilder promptBuilder,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiSemanticClassifier> logger)
    {
        _clientFactory = clientFactory;
        _promptBuilder = promptBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public virtual async Task<RawSemanticClassificationResult> ClassifyAsync(
        string text,
        IReadOnlyCollection<DocumentClass> documentClasses,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(documentClasses);

        var client = _clientFactory.CreateChatClient();
        var prompt = _promptBuilder.BuildSemanticClassificationPrompt(documentClasses);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(prompt),
            new UserChatMessage($"DOCUMENT_TEXT:\n{text}")
        ];

        ChatCompletionOptions options = new()
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = _options.MaxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "semantic_classification",
                jsonSchema: SemanticSchema,
                jsonSchemaIsStrict: true)
        };

        ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages, options, cancellationToken);
        var content = result.Value.Content.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("OpenAI semantic classification returned empty content.");

        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        var documentClassName = root.GetProperty("documentClassName").GetString()?.Trim().ToUpperInvariant() ?? "UNKNOWN";
        var group = root.GetProperty("group").GetString()?.Trim().ToUpperInvariant() ?? "UNKNOWN";
        var subGroup = root.GetProperty("subGroup").GetString()?.Trim().ToUpperInvariant() ?? "UNKNOWN";
        var reuseExistingClass = root.GetProperty("reuseExistingClass").GetBoolean();
        var confidence = root.GetProperty("confidence").GetDecimal();

        _logger.LogInformation(
            "AI semantic classification: {DocumentClassName}, reuse={ReuseExistingClass}, confidence={Confidence}",
            documentClassName,
            reuseExistingClass,
            confidence);

        return new RawSemanticClassificationResult(
            documentClassName,
            group,
            subGroup,
            reuseExistingClass,
            confidence);
    }
}
