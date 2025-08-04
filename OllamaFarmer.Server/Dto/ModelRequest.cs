namespace OllamaFarmer.Server.Dto
{
    // turn this in to a DTO
    //         [FromBody] string from,
    //    [FromBody]
    //string systemMessage,
    //    [FromBody] string template,
    //    [FromBody] float temperature = 0.7f,
    //    [FromBody] float topP = 1.0f,
    //    [FromBody] float frequencyPenalty = 0.0f,
    //    [FromBody] float presencePenalty = 0.0f

    public class ModelRequest
    {
        public string Model { get; set; }
        public string SystemMessage { get; set; }
        public string Template { get; set; }
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 1.0f;
        public float FrequencyPenalty { get; set; } = 0.0f;
        public float PresencePenalty { get; set; } = 0.0f;
        public string NewModelName { get; set; }
    }
}
