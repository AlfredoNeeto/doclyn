namespace Doclyn.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public const string Section = "OpenAi";

    public string Model { get; init; } = "gpt-5.4";
    public string ApiKey { get; init; } = string.Empty;
    public float Temperature { get; init; } = 0.1f;
    public int MaxTokens { get; init; } = 4000;
}
