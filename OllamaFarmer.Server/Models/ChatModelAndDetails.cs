using OllamaFarmer.Server.Dto;
using OllamaSharp.Models;

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

    /// <summary>
    /// Data transfer object representing a model and its metadata.
    /// </summary>
    public class ModelDto
    {

        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the time the model was created or last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the size of the model file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets a cryptographic hash of the model file.
        /// </summary>
        public string Digest { get; set; }

        /// <summary>
        /// Gets or sets additional details about the model.
        /// </summary>
        public DetailsDto Details { get; set; }

        /// <summary>
        /// The amount of vram (in bytes) currently in use by the model.
        /// </summary>
        public long SizeVram { get; set; } = 0;


        public ModelDto(RunningModel model)
            : this((Model)model)
        {
            SizeVram = model.SizeVram;
        }

        public ModelDto(Model model)
        {
            if (model == null)
            {
                // Initialize with default values to prevent null reference exceptions
                Name = string.Empty;
                ModifiedAt = DateTime.MinValue;
                Size = 0;
                Digest = string.Empty;
                Details = new DetailsDto
                {
                    ParentModel = null,
                    Format = "unknown",
                    Family = "unknown",
                    Families = Array.Empty<string>(),
                    ParameterSize = "unknown",
                    QuantizationLevel = "unknown"
                };
                return;
            }

            Name = model.Name;
            ModifiedAt = model.ModifiedAt;
            Size = model.Size;
            Digest = model.Digest;
            Details = new DetailsDto
            {
                ParentModel = model.Details?.ParentModel,
                Format = model.Details?.Format ?? "unknown",
                Family = model.Details?.Family ?? "unknown",
                Families = model.Details?.Families ?? Array.Empty<string>(),
                ParameterSize = model.Details?.ParameterSize ?? "unknown",
                QuantizationLevel = model.Details?.QuantizationLevel ?? "unknown"
            };
        }
    }

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
