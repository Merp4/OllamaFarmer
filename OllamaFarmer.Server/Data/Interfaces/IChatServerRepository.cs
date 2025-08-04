using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Data.Interfaces
{
    public interface IChatServerRepository
    {
        Task<ChatServer> CreateChatServerAsync(ChatServer chatServer);
        Task<bool> DeleteChatServerAsync(Guid id);
        Task<List<ChatServer>> GetAllChatServersAsync();
        Task<ChatServer?> GetChatServerByIdAsync(Guid id);
        Task<ChatServer?> GetChatServerByUriAsync(Uri uri);
        Task<ChatServer?> UpdateChatServerAsync(ChatServer chatServer);
    }
}
