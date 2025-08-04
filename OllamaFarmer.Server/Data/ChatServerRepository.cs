using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Data.Interfaces;

namespace OllamaFarmer.Server.Data
{
    public class ChatServerRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IChatServerRepository
    {

        public async Task<List<ChatServer>> GetAllChatServersAsync()
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<ChatServer>()
                .ToListAsync();
        }
        public async Task<ChatServer?> GetChatServerByIdAsync(Guid id)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<ChatServer>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<ChatServer?> GetChatServerByUriAsync(Uri uri)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<ChatServer>()
                .FirstOrDefaultAsync(x => x.Uri == uri);
        }
        public async Task<ChatServer> CreateChatServerAsync(ChatServer chatServer)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.Set<ChatServer>().Add(chatServer);
            await dbContext.SaveChangesAsync();
            return chatServer;
        }
        public async Task<ChatServer?> UpdateChatServerAsync(ChatServer chatServer)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existingServer = await dbContext.Set<ChatServer>()
                .FirstOrDefaultAsync(x => x.Id == chatServer.Id);
            if (existingServer == null)
                return null;
            existingServer.Name = chatServer.Name;
            existingServer.Description = chatServer.Description;
            existingServer.Version = chatServer.Version;
            existingServer.Uri = chatServer.Uri;
            existingServer.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
            return existingServer;
        }

        public async Task<bool> DeleteChatServerAsync(Guid id)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var server = await dbContext.Set<ChatServer>()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (server == null)
                return false;
            dbContext.Set<ChatServer>().Remove(server);
            await dbContext.SaveChangesAsync();
            return true;
        }


    }
}
