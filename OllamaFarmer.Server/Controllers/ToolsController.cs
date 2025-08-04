using Microsoft.AspNetCore.Mvc;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services.Interfaces;
using OllamaFarmer.Server.Services.Mcp;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolsController : ControllerBase
    {
        private readonly ILogger<ToolsController> logger;

        private readonly IMcpToolService mcpService;

        public ToolsController(ILogger<ToolsController> logger, IMcpToolService mcpService)
        {
            this.logger = logger;
            this.mcpService = mcpService;
        }

        [HttpGet]
        public async Task<IEnumerable<McpToolDto>> List([FromQuery] Guid? serverId)
        {
            return await GetTools(serverId);
        }

        private async Task<List<McpToolDto>> GetTools(Guid? serverId)
        {
            var tools = await mcpService.ListToolsFromDbAsync(serverId ?? Guid.Empty);
            return tools.Select(tool => new McpToolDto
            {
                Name = tool.Name,
                Description = tool.Description,
                Id = tool.Id,
                McpServerId = tool.McpServerId,
                Version = tool.Version,
                CreatedAt = tool.CreatedAt,
                UpdatedAt = tool.UpdatedAt
            }).ToList();
            
        }


        [HttpGet("paged")]
        public async Task<PagedResponse<IEnumerable<McpToolDto>>> ListPagedAsync([FromQuery] Guid? serverId, int cursor = 0, int pageSize = 10)
        {
            var tools = await GetTools(serverId);
            return new PagedResponse<IEnumerable<McpToolDto>>()
            { Cursor = cursor, PageSize = pageSize, FilteredCount = tools.Count, TotalCount = tools.Count, Data = tools.Skip(cursor * pageSize).Take(pageSize) };
        }
    }

    public class McpServerDetailsDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public required Uri Uri { get; set; }

        public List<McpToolDto> Tools { get; set; } = new List<McpToolDto>();
    }

    public class McpToolDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid McpServerId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
