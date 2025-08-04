using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Services.Ollama.Interfaces;
using OllamaSharp;
using static OllamaSharp.OllamaApiClient;

namespace OllamaFarmer.Server.Services.Ollama
{
    public class OllamaApiClientFactory : IOllamaApiClientFactory
    {
        //private readonly Uri _ollamaUri;
        private TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

        public OllamaApiClientFactory()//Uri ollamaUri)
        {
            //_ollamaUri = ollamaUri ?? throw new ArgumentNullException(nameof(ollamaUri));
        }

        public IChatClient CreateOpenClient(Uri serverUri, string model)
        {
            return CreateClient(serverUri, model) as IChatClient 
                   ?? throw new InvalidOperationException($"Failed to create a chat client for model '{model}'. Ensure the model is valid and the Ollama API is accessible.");
        }

        public OllamaApiClient CreateClient(Uri serverUri, string model)
        {
            var client = new OllamaApiClient(new HttpClient() { BaseAddress = serverUri, Timeout = _defaultTimeout }, model);
            return client;
        }

    }
}