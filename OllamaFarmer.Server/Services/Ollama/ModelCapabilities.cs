namespace OllamaFarmer.Server.Services.Ollama
{
    public class ModelCapabilities
    {
        public string[] Strings { get; set; }

        public bool SupportsCompletion { get; set; }
        public bool SupportsTools { get; set; }
        public bool SupportsVision { get; set; }
        public bool SupportsThinking { get; set; }

        public ModelCapabilities()
        {
            Strings = Array.Empty<string>();
        }

        public ModelCapabilities(IEnumerable<string>? strings)
        {
            Strings = strings?.ToArray() ?? [];

            foreach (var str in Strings)
            {
                if (string.IsNullOrWhiteSpace(str))
                    continue;
                if (str.Equals("completion", StringComparison.OrdinalIgnoreCase))
                    SupportsCompletion = true;
                else if (str.Equals("tools", StringComparison.OrdinalIgnoreCase))
                    SupportsTools = true;
                else if (str.Equals("vision", StringComparison.OrdinalIgnoreCase))
                    SupportsVision = true;
                else if (str.Equals("thinking", StringComparison.OrdinalIgnoreCase))
                    SupportsThinking = true;
            }
        }
    }
}
