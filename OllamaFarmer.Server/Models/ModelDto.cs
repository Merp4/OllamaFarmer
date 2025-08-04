using OllamaSharp.Models;

namespace OllamaFarmer.Server.Models
{
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
}
