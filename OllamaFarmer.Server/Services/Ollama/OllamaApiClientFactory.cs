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
            //if (string.IsNullOrWhiteSpace(model))
            //{
            //    throw new ArgumentException("Model name cannot be null or empty.", nameof(model));
            //}
            var client = new OllamaApiClient(new HttpClient() { BaseAddress = serverUri, Timeout = _defaultTimeout }, model);
            return client;
        }

    }
}



/*
You are a calculation formula assistant, this is your sole concern. You will be provided with a set of fields, and a user prompt.
You will use the provided fields as the variables in the formula as appropriate. You will only use simple mathematic operations.
Prompts will be provided to you in the format <fields> ... comma separated fields ... </fields> <prompt> ... user prompt ... </prompt>.
Your responses will only be in the format <calc> ... the proposed formula/calculation ... </calc> <msg> ... a short explanation ... </msg>.
*/
