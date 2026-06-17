using System.ClientModel;
using System.Text.Json;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Doclyn.Infrastructure.AI;

public sealed class OpenAiStructuredDataExtractor : IAiStructuredDataExtractor
{
    private readonly OpenAiClientFactory _clientFactory;
    private readonly PromptBuilder _promptBuilder;
    private readonly DocumentClassAiSchemaBuilder _schemaBuilder;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiStructuredDataExtractor> _logger;

    public OpenAiStructuredDataExtractor(
        OpenAiClientFactory clientFactory,
        PromptBuilder promptBuilder,
        DocumentClassAiSchemaBuilder schemaBuilder,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiStructuredDataExtractor> logger)
    {
        _clientFactory = clientFactory;
        _promptBuilder = promptBuilder;
        _schemaBuilder = schemaBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Dictionary<string, object?>> ExtractAsync(
        string text,
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(documentClass);
        ArgumentNullException.ThrowIfNull(indexers);

        var client = _clientFactory.CreateChatClient();
        var prompt = _promptBuilder.BuildDynamicExtractionPrompt(documentClass, indexers);
        var schema = _schemaBuilder.Build(documentClass, indexers);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(prompt),
            new UserChatMessage($"DOCUMENT_TEXT:\n{text}")
        ];

        var formatName = SanitizeFormatName(documentClass.Name);

        ChatCompletionOptions options = new()
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = _options.MaxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: formatName,
                jsonSchema: schema,
                jsonSchemaIsStrict: true)
        };

        _logger.LogInformation(
            "AiExtractionRequest class:{DocumentClass} model:{Model} indexers:{IndexerCount} strict:true",
            documentClass.Name,
            _options.Model,
            indexers.Count);


        ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages, options, cancellationToken);
        var content = result.Value.Content.FirstOrDefault()?.Text
?? throw new InvalidOperationException("OpenAI extraction returned empty content.");

        using var json = JsonDocument.Parse(content);
        return ConvertElementToDictionary(json.RootElement);

    }

    private static string SanitizeFormatName(string name)
    {
        var sanitized = new string(name.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrEmpty(sanitized) ? "document_extraction" : sanitized.ToLowerInvariant();
    }

    private static Dictionary<string, object?> ConvertElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertElement(property.Value);
        }

        return result;
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
