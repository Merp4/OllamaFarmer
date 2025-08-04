using OllamaFarmer.Server.Dto;

namespace OllamaFarmer.Server.Models
{
    /// <summary>
    /// Combines a model's API details and chat-specific details.
    /// </summary>
    public class ChatModelAndDetails
    {
        /// <summary>
        /// Gets or sets the API model details. May be null if not available.
        /// </summary>
        public ModelDto? ApiModel { get; set; }

        /// <summary>
        /// Gets or sets the chat model details.
        /// </summary>
        public ChatModelDto ChatModel { get; set; }
    }
}
