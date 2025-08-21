using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaFarmer.Abstractions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

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
            // Determine base endpoint for OpenAI-compatible API (Ollama's OpenAI shim is at /v1)
            var baseUri = new Uri(new Uri(serverUri.GetLeftPart(UriPartial.Authority)), "/v1");
            var endpointOverride = _config["AI:OllamaOpenAI:Endpoint"];
            if (!string.IsNullOrWhiteSpace(endpointOverride))
            {
                baseUri = new Uri(new Uri(endpointOverride), "/v1");
            }

            var modelId = model ?? _config["AI:OllamaOpenAI:Model"];
            var apiKey = _config["AI:OllamaOpenAI:ApiKey"] ?? "ollama"; // Ollama ignores API key but many clients require one

            // Build an OpenAI-compatible IChatClient targeting Ollama.
            // The OpenAI .NET client supports custom endpoints; point it to Ollama's /v1 root.
            var options = new OpenAIClientOptions
            {
                Endpoint = baseUri,
            };

            // Create the OpenAI ChatClient and adapt it to IChatClient
            var credential = new ApiKeyCredential(apiKey);
            ChatClient openAIChatClient = new ChatClient(
                model: modelId,
                credential: credential,
                options: options);

            // Optionally, we could wrap with ChatClientBuilder to add features (telemetry, tools, etc.)
            IChatClient client = openAIChatClient.AsIChatClient();
            return client;
        }
    }
}
