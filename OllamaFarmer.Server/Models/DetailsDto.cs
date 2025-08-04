namespace OllamaFarmer.Server.Models
{
    /// <summary>
    /// Represents additional details about a model.
    /// </summary>
    public class DetailsDto
    {
        /// <summary>
        /// Gets or sets the name of the parent model on which the model is based. May be null.
        /// </summary>
        public string? ParentModel { get; set; }

        /// <summary>
        /// Gets or sets the format of the model file.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the family of the model.
        /// </summary>
        public string Family { get; set; }

        /// <summary>
        /// Gets or sets the families of the model. May be null.
        /// </summary>
        public string[]? Families { get; set; }

        /// <summary>
        /// Gets or sets the number of parameters in the model.
        /// </summary>
        public string ParameterSize { get; set; }

        /// <summary>
        /// Gets or sets the quantization level of the model.
        /// </summary>
        public string QuantizationLevel { get; set; }
    }
}
