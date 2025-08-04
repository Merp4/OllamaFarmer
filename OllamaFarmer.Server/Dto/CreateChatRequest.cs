using Microsoft.OpenApi.Validations.Rules;

namespace OllamaFarmer.Server.Dto
{
    public class ExpoCreateChatRequest
    {
        /// <summary>
        /// The name of the chat. This is used to identify the chat in the database.
        /// </summary>
        public string? Name { get; set; }
        public string? Model { get; set; }
        public string? Message { get; set; }
        public string? SystemMessage { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
    }
}
