namespace OllamaFarmer.Server.Data
{
    public class BinaryRepositoryConfiguration
    {
        public string RootDirectory { get; set; } = "file_repository";
        public bool IsAbsolutePath { get; set; } = false;
        public long MaxFileSizeInBytes { get; set; } = 104857600; // 100 MB
        public List<string> AllowedFileExtensions { get; set; } = new List<string>
        {
            ".txt", ".json", ".md", ".mp3", ".mp4", ".jpg", ".jpeg", ".png", ".gif", ".csv"
        };

        /// <summary>
        /// This gets the absolute path to the base directory where files are stored.
        /// </summary>
        /// <returns></returns>
        public string GetBasePath()
        {
            return IsAbsolutePath ? RootDirectory : Path.Combine(GetExecutionDirectoryPath(), RootDirectory);
        }

        private static string GetExecutionDirectoryPath()
        {
            var path = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Application context base directory path is not set.");
            }
            return path;
        }
    }
}
