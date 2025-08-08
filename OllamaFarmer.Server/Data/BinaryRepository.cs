using MimeKit;

namespace OllamaFarmer.Server.Data
{
    /// <summary>
    /// Repository for filesystem storage of binary files.
    /// </summary>
    public class BinaryRepository : IBinaryRepository
    {
        private readonly ILogger<BinaryRepository> logger;
        private BinaryRepositoryConfiguration config;
        private string rootPath;


        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryRepository"/> class.
        /// </summary>
        /// <param name="basePath">The base directory path for storing files.</param>
        public BinaryRepository(ILogger<BinaryRepository> logger, BinaryRepositoryConfiguration? configuration = null)
        {
            this.logger = logger;
            config = configuration ?? new();
            rootPath = config.GetBasePath();
            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException("Root path for BinaryRepository is not set.");
            
            logger.LogInformation("BinaryRepository initialized with root path: {RootPath}", rootPath);

            if (!Directory.Exists(rootPath))
            {
                logger.LogInformation("Creating root directory for BinaryRepository at: {RootPath}", rootPath);
                Directory.CreateDirectory(rootPath);
            }
        }

        /// <summary>
        /// Resolves a file or directory path and ensures it is within the base directory.
        /// Throws UnauthorizedAccessException if the resolved path is outside the base directory.
        /// </summary>
        private string ResolvePath(string relativePath)
        {
            // Handle empty/null paths as root directory
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/" || relativePath == "\\")
                return rootPath;

            // Check for explicit traversal attempts
            if (relativePath.Contains("..") || 
                !string.IsNullOrEmpty(Path.GetPathRoot(relativePath))) // Absolute paths or drive letters
            {
                throw new UnauthorizedAccessException($"Invalid path detected: '{relativePath}' contains prohibited characters or is absolute.");
            }

            // Resolve the path within the root directory
            var combinedPath = Path.Combine(rootPath, relativePath);
            var fullPath = Path.GetFullPath(combinedPath);
            
            // Ensure the canonical root path ends with directory separator for accurate comparison
            var canonicalRoot = Path.GetFullPath(rootPath);
            if (!canonicalRoot.EndsWith(Path.DirectorySeparatorChar))
                canonicalRoot += Path.DirectorySeparatorChar;

            // Ensure resolved path is within the root directory
            if (!fullPath.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"Path traversal attempt detected: '{relativePath}' resolves outside base directory.");

            return fullPath;
        }

        public async Task SaveFileAsync(string fileName, Stream fileStream)
        {
            var filePath = ResolvePath(fileName);
            
            // Ensure the directory exists
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            using var file = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(file);
        }

        public async Task SaveFileAsync(string fileName, byte[] fileData)
        {
            using var fileStream = new MemoryStream(fileData);
            await SaveFileAsync(fileName, fileStream);
        }

        public async Task SaveFileAsync(string fileName, string fileStringContent)
        {
            var filePath = ResolvePath(fileName);
            await File.WriteAllTextAsync(filePath, fileStringContent);
        }

        public async Task<Stream?> GetFileStreamAsync(string fileName)
        {
            var filePath = ResolvePath(fileName);
            if (File.Exists(filePath))
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            return null;
        }

        public async Task<string?> GetFileStringContentAsync(string fileName)
        {
            var filePath = ResolvePath(fileName);
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            return null;
        }

        public async Task<byte[]?> GetFileBytesAsync(string fileName)
        {
            var filePath = ResolvePath(fileName);
            if (File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }
            return null;
        }

        public bool FileExists(string fileName)
        {
            var filePath = ResolvePath(fileName);
            return File.Exists(filePath);
        }

        public void DeleteFile(string fileName)
        {
            var filePath = ResolvePath(fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public IEnumerable<string> ListFiles(string path)
        {
            var fullPath = ResolvePath(path);
            if (!Directory.Exists(fullPath))
            {
                return Enumerable.Empty<string>();
            }
            return Directory.GetFiles(fullPath).Select(Path.GetFileName).Where(fileName => fileName != null)!;
        }

        public IEnumerable<FileMetaDataDto> ListFilesMeta(string path, bool includeSubDirectories)
        {
            var fullPath = ResolvePath(path);
            if (!Directory.Exists(fullPath))
            {
                return Enumerable.Empty<FileMetaDataDto>();
            }
            var files = Directory.GetFiles(fullPath)
                .Select(Path.GetFileName)
                .Where(fileName => !string.IsNullOrEmpty(fileName)) // Ensure fileName is not null or empty
                .Select(f => GetMetadataAsync(f!).Result) // Use null-forgiving operator to suppress CS8604
                .Where(meta => meta != null) // Filter out any null metadata
                .Select(meta => meta!); // Use null-forgiving operator to suppress CS8604

            if (includeSubDirectories)
            {
                var directories = Directory.GetDirectories(fullPath)
                    .Select(Path.GetFileName)
                    .Where(dirName => !string.IsNullOrEmpty(dirName)) // Ensure dirName is not null or empty
                    .Select(d => new FileMetaDataDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = Path.GetFileName(d!),
                        IsDirectory = true,
                        Type = FileType.Directory,
                        MimeType = "inode/directory",
                        Path = Path.Combine(path, Path.GetFileName(d!)),
                        UpdatedAt = Directory.GetLastWriteTime(Path.Combine(fullPath, d!)).ToString("o"),
                        Size = 0
                    });

                return files.Concat(directories);
            }
            return files;
        }

        /// <summary>
        /// Retrieves metadata for a specified file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>A dictionary containing metadata about the file.</returns>
        public async Task<FileMetaDataDto?> GetMetadataAsync(string fileName)
        {
            var filePath = ResolvePath(fileName);
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return new FileMetaDataDto
                {
                    Id = Guid.NewGuid().ToString(), // Generate a unique ID for the file
                    Name = fileInfo.Name,
                    IsDirectory = false, // Assuming files are not directories
                    Type = GetFileType(fileInfo),
                    MimeType = MimeTypes.GetMimeType(fileInfo.Name),
                    Path = Path.GetRelativePath(rootPath, fileInfo.FullName),
                    UpdatedAt = fileInfo.LastWriteTime.ToString("o"), // ISO 8601 format
                    Size = fileInfo.Length // Get file size in bytes
                };
            }
            return null;
        }

        private FileType GetFileType(FileInfo fi)
        {
            if (fi.Attributes.HasFlag(FileAttributes.Directory))
            {
                return FileType.Directory;
            }
            var extension = fi.Extension.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => FileType.Image,
                ".mp4" or ".avi" or ".mkv" => FileType.Video,
                ".mp3" or ".wav" => FileType.Audio,
                ".txt" or ".md" => FileType.Text,
                ".zip" or ".rar" or ".7zip" => FileType.Archive,
                _ => FileType.File
            };
        }

        public void ClearRepository()
        {
            foreach (var file in Directory.GetFiles(rootPath))
            {
                File.Delete(file);
            }
        }

        public long GetFileSize(string fileName)
        {
            var filePath = ResolvePath(fileName);
            if (File.Exists(filePath))
            {
                return new FileInfo(filePath).Length;
            }
            return 0;
        }

        public Task CreateDirectoryAsync(string directoryName)
        {
            var dirPath = ResolvePath(directoryName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            return Task.CompletedTask;
        }

        public Task MoveFileAsync(string sourceFileName, string destinationFileName)
        {
            var sourcePath = ResolvePath(sourceFileName);
            var destinationPath = ResolvePath(destinationFileName);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Source file does not exist.", sourceFileName);
            }
            if (File.Exists(destinationPath))
            {
                throw new IOException("Destination file already exists.");
            }
            File.Move(sourcePath, destinationPath);
            return Task.CompletedTask;
        }

        public Task CopyFileAsync(string sourceFileName, string destinationFileName)
        {
            var sourcePath = ResolvePath(sourceFileName);
            var destinationPath = ResolvePath(destinationFileName);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Source file does not exist.", sourceFileName);
            }
            if (File.Exists(destinationPath))
            {
                throw new IOException("Destination file already exists.");
            }
            File.Copy(sourcePath, destinationPath);
            return Task.CompletedTask;
        }
    }
}
