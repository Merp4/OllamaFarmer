using Microsoft.EntityFrameworkCore;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Services.Mcp.Interfaces;

namespace OllamaFarmer.Server.Services.Mcp
{
    public class McpServerRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IMcpServerRepository
    {

        public async Task<List<McpServer>> GetAllMcpServersAsync()
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<McpServer>()
                .Include(x => x.McpTools)
                .ToListAsync();
        }
        public async Task<McpServer?> GetMcpServerByIdAsync(Guid id)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<McpServer>()
                .Include(x => x.McpTools)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<McpServer> CreateMcpServerAsync(McpServer mcpServer)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.Set<McpServer>().Add(mcpServer);
            await dbContext.SaveChangesAsync();
            return mcpServer;
        }
        public async Task<McpServer?> UpdateMcpServerAsync(McpServer mcpServer)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existingServer = await dbContext.Set<McpServer>()
                .FirstOrDefaultAsync(x => x.Id == mcpServer.Id);
            if (existingServer == null)
                return null;
            existingServer.Name = mcpServer.Name;
            existingServer.Description = mcpServer.Description;
            existingServer.Version = mcpServer.Version;
            existingServer.Uri = mcpServer.Uri;
            existingServer.UpdatedAt = DateTimeOffset.UtcNow;
            existingServer.McpTools = mcpServer.McpTools ?? new(); // Update tools if provided
            await dbContext.SaveChangesAsync();
            return existingServer;
        }
        public async Task<bool> DeleteMcpServerAsync(Guid id)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var server = await dbContext.Set<McpServer>()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (server == null)
                return false;
            dbContext.Set<McpServer>().Remove(server);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<McpTool?> GetMcpToolByIdAsync(Guid id)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<McpTool>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<McpTool> CreateMcpToolAsync(McpTool mcpTool)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.Set<McpTool>().Add(mcpTool);
            await dbContext.SaveChangesAsync();
            return mcpTool;
        }
        public async Task<IEnumerable<McpTool>> CreateMcpToolsAsync(IEnumerable<McpTool> mcpTools)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Set<McpTool>().AddRangeAsync(mcpTools);
            await dbContext.SaveChangesAsync();
            return mcpTools;
        }
        public async Task<McpTool?> UpdateMcpToolAsync(McpTool mcpTool)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existingTool = await dbContext.Set<McpTool>()
                .FirstOrDefaultAsync(x => x.Id == mcpTool.Id);
            if (existingTool == null)
                return null;
            existingTool.Name = mcpTool.Name;
            existingTool.Description = mcpTool.Description;
            existingTool.Version = mcpTool.Version;
            existingTool.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
            return existingTool;
        }
        public async Task<bool> DeleteMcpToolAsync(Guid id)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var tool = await dbContext.Set<McpTool>()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (tool == null)
                return false;
            dbContext.Set<McpTool>().Remove(tool);
            await dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteMcpToolsAsync(IEnumerable<Guid> ids)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var tools = await dbContext.Set<McpTool>()
                .Where(x => ids.Contains(x.Id)).ToListAsync();
            dbContext.Set<McpTool>().RemoveRange(tools);
            await dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<List<McpTool>> GetAllMcpToolsAsync(Guid mcpServerId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<McpTool>()
                .Where(x => x.McpServerId == mcpServerId)
                .ToListAsync();
        }
        public async Task<McpTool?> GetMcpToolByNameAsync(Guid mcpServerId, string name)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<McpTool>()
                .FirstOrDefaultAsync(x => x.McpServerId == mcpServerId && x.Name == name);
        }

        public async Task<List<McpServer>> GetMcpServerByToolIds(Guid[] toolIds)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();

            return await dbContext.Set<McpServer>()
                .Include(s => s.McpTools)
                .Where(s => s.McpTools.Any(t => toolIds.Contains(t.Id)))
                .ToListAsync();
        }

        public Task<List<McpTool>> GetToolsByIdsAsync(Guid[] toolsIds)
        {
            var dbContext = dbContextFactory.CreateDbContext();
            return dbContext.Set<McpTool>()
                .Where(t => toolsIds.Contains(t.Id))
                .ToListAsync();
        }
    }
}
