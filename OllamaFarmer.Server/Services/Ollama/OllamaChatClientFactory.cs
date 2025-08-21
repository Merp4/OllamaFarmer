using Microsoft.Extensions.AI;
using OllamaFarmer.Abstractions.AI;
using OllamaFarmer.OllamaClient.Interfaces;

namespace OllamaFarmer.Server.Services.Ollama
{
    /// <summary>
    /// Bridges OllamaSharp's client to Microsoft.Extensions.AI's IChatClient for chat flows.
    /// </summary>
    public class OllamaChatClientFactory : IChatClientFactory
    {
        private readonly IOllamaApiClientFactory _ollamaFactory;

        public OllamaChatClientFactory(IOllamaApiClientFactory ollamaFactory)
        {
            _ollamaFactory = ollamaFactory;
        }

        public IChatClient CreateOpenClient(Uri serverUri, string model)
        {
            var ollama = _ollamaFactory.CreateOllamaClient(serverUri, model);
            return ollama as IChatClient
                ?? throw new InvalidOperationException($"Ollama client could not be used as IChatClient for model '{model}'.");
        }
    }
}
