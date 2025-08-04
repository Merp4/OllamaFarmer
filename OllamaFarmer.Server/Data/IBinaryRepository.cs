namespace OllamaFarmer.Server.Data
{
    public interface IBinaryRepository
    {
        Task SaveFileAsync(string fileName, Stream fileStream);
        Task SaveFileAsync(string fileName, byte[] fileData);
        Task SaveFileAsync(string fileName, string fileStringContent);
        Task<Stream?> GetFileStreamAsync(string fileName);
        Task<string?> GetFileStringContentAsync(string fileName);
        Task<byte[]?> GetFileBytesAsync(string fileName);
        bool FileExists(string fileName);
        void DeleteFile(string fileName);
        IEnumerable<string> ListFiles(string path);
        void ClearRepository();
        long GetFileSize(string fileName);
        Task<FileMetaDataDto?> GetMetadataAsync(string fileName);
        IEnumerable<FileMetaDataDto> ListFilesMeta(string path, bool includeSubDirectories);
        Task CreateDirectoryAsync(string directoryName);
        Task MoveFileAsync(string sourceFileName, string destinationFileName);
        Task CopyFileAsync(string sourceFileName, string destinationFileName);
    }
}
