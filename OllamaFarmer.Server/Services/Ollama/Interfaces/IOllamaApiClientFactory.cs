using Microsoft.Extensions.AI;
using OllamaSharp;

namespace OllamaFarmer.Server.Services.Ollama.Interfaces
{
    public interface IOllamaApiClientFactory
    {
        OllamaApiClient CreateClient(Uri serverUri, string model);
        IChatClient CreateOpenClient(Uri serverUri, string model);

    }
}
