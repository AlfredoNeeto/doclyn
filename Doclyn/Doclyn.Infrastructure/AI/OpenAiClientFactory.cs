using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Doclyn.Infrastructure.AI;

public class OpenAiClientFactory
{
    private readonly OpenAiOptions _options;

    public OpenAiClientFactory(IOptions<OpenAiOptions> options)
    {
        _options = options.Value;
    }

    public ChatClient CreateChatClient()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        return new ChatClient(_options.Model, _options.ApiKey);
    }
}
