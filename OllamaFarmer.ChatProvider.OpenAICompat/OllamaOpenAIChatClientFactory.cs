using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaFarmer.Abstractions.AI;

namespace OllamaFarmer.ChatProvider.OpenAICompat
{
    /// <summary>
    /// IChatClientFactory that points Microsoft.Extensions.AI's OpenAI-compatible client at an Ollama /v1 endpoint.
    /// NOTE: Requires Microsoft.Extensions.AI.OpenAI package to create the actual client; currently returns
    /// a NotImplementedException to avoid accidental runtime usage before the package is added and DI is wired.
    /// </summary>
    public sealed class OllamaOpenAIChatClientFactory : IChatClientFactory
    {
        private readonly IConfiguration _config;
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

        public OllamaOpenAIChatClientFactory(IConfiguration config)
        {
            _config = config;
        }

    public IChatClient CreateOpenClient(Uri serverUri, string model)
        {
            var baseUri = new Uri(new Uri(serverUri.GetLeftPart(UriPartial.Authority)), "/v1");
            var endpointOverride = _config["AI:OllamaOpenAI:Endpoint"];
            if (!string.IsNullOrWhiteSpace(endpointOverride))
            {
                baseUri = new Uri(new Uri(endpointOverride), "/v1");
            }

            var modelId = _config["AI:OllamaOpenAI:Model"] ?? model;
            var apiKey = _config["AI:OllamaOpenAI:ApiKey"] ?? "ollama"; // Ollama ignores API key but many clients require one

            // Build an OpenAI-compatible IChatClient targeting Ollama.
            // Placeholder: wire up Microsoft.Extensions.AI.OpenAI here to point at Ollama '/v1'
            throw new NotImplementedException("OpenAI-compatible chat client not yet wired. Install Microsoft.Extensions.AI.OpenAI and return an IChatClient targeting Ollama '/v1'.");
        }
    }
}
