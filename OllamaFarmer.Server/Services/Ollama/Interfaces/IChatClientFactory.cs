using Microsoft.Extensions.AI;

namespace OllamaFarmer.Server.Services.Ollama.Interfaces
{
    public interface IChatClientFactory
    {
        IChatClient CreateClient(Uri serverUri, string model);

    }

    
}
