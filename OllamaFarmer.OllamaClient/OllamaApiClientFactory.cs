using OllamaFarmer.OllamaClient.Interfaces;
using OllamaSharp;

namespace OllamaFarmer.OllamaClient
{
    public class OllamaApiClientFactory : IOllamaApiClientFactory
    {
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

        public OllamaApiClient CreateOllamaClient(Uri serverUri, string model)
        {
            var client = new OllamaApiClient(new HttpClient() { BaseAddress = serverUri, Timeout = _defaultTimeout }, model);
            return client;
        }
    }
}
