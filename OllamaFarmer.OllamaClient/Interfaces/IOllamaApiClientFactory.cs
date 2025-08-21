using OllamaSharp;

namespace OllamaFarmer.OllamaClient.Interfaces
{
    public interface IOllamaApiClientFactory
    {
        OllamaApiClient CreateOllamaClient(Uri serverUri, string model);
    }
}
