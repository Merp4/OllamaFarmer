using OllamaFarmer.OllamaClient.Interfaces;
using OllamaSharp;
using static OllamaSharp.OllamaApiClient;

namespace OllamaFarmer.Server.Services.Ollama
{
    public class OllamaApiClientFactory : IOllamaApiClientFactory
    {
        // Todo: configurable timeout
        private TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

        public OllamaApiClientFactory()
        {
        }
        
    public OllamaApiClient CreateOllamaClient(Uri serverUri, string model)
        {
            var client = new OllamaApiClient(new HttpClient() { BaseAddress = serverUri, Timeout = _defaultTimeout }, model);
            return client;
        }

    }
}