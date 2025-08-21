namespace OllamaFarmer.Server.Services.Ollama
{
    // Back-compat shim: derive from the canonical Data.Entities.ModelCapabilities
    public class ModelCapabilities : OllamaFarmer.Data.Entities.ModelCapabilities
    {
        public ModelCapabilities() : base() { }
        public ModelCapabilities(IEnumerable<string>? strings) : base(strings) { }
    }
}
