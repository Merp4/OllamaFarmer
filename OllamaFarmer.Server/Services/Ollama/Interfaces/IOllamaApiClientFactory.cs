using OllamaSharp;

namespace OllamaFarmer.Server.Services.Ollama.Interfaces
{
    public interface IOllamaApiClientFactory
    {
        OllamaApiClient CreateOllamaClient(Uri serverUri, string model);
    }

    
}
