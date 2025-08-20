using Microsoft.Extensions.AI;

namespace OllamaFarmer.Abstractions.AI
{
    /// <summary>
    /// Factory for creating provider-agnostic chat clients.
    /// Implementations may wrap local (Ollama) or hosted (OpenAI-compatible) endpoints.
    /// </summary>
    public interface IChatClientFactory
    {
        // General OpenAI-compatible chat client (used for chat flows)
        IChatClient CreateOpenClient(Uri serverUri, string model);

        // Optional: raw Ollama client creation is not part of abstractions; server keeps its IOllamaApiClientFactory.
    }
}
