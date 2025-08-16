using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolBagsController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<ToolBagsController> _logger;

        public ToolBagsController(IDbContextFactory<AppDbContext> dbFactory, ILogger<ToolBagsController> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToolBagDto>>> GetBagsAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            var bags = await db.ToolBags
                .Include(b => b.Tools)
                .ToListAsync();
            var dtos = bags.Select(ToDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ToolBagDto>> GetBagAsync(Guid id)
        {
            using var db = _dbFactory.CreateDbContext();
            var bag = await db.ToolBags
                .Include(b => b.Tools)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (bag == null) return NotFound();
            return Ok(ToDto(bag));
        }

        [HttpPost]
        public async Task<ActionResult<ToolBagDto>> CreateAsync([FromBody] CreateToolBagRequest request)
        {
            using var db = _dbFactory.CreateDbContext();
            var bag = new ToolBag
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.ToolBags.Add(bag);
            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBagAsync), new { id = bag.Id }, ToDto(bag));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ToolBagDto>> UpdateAsync(Guid id, [FromBody] UpdateToolBagRequest request)
        {
            using var db = _dbFactory.CreateDbContext();
            var bag = await db.ToolBags.FirstOrDefaultAsync(b => b.Id == id);
            if (bag == null) return NotFound();
            bag.Name = request.Name ?? bag.Name;
            bag.Description = request.Description ?? bag.Description;
            bag.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Ok(ToDto(bag));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(Guid id)
        {
            using var db = _dbFactory.CreateDbContext();
            var bag = await db.ToolBags.Include(b => b.Tools).FirstOrDefaultAsync(b => b.Id == id);
            if (bag == null) return NotFound();
            // Remove tools in the bag first (FK)
            if (bag.Tools.Any())
                db.ToolBagTools.RemoveRange(bag.Tools);
            db.ToolBags.Remove(bag);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/tools")]
        public async Task<ActionResult<IEnumerable<Guid>>> GetToolsAsync(Guid id)
        {
            using var db = _dbFactory.CreateDbContext();
            var exists = await db.ToolBags.AnyAsync(b => b.Id == id);
            if (!exists) return NotFound();
            var toolIds = await db.ToolBagTools
                .Where(t => t.ToolBagId == id)
                .Select(t => t.McpToolId)
                .ToListAsync();
            return Ok(toolIds);
        }

        [HttpPut("{id}/tools")]
        public async Task<ActionResult> SetToolsAsync(Guid id, [FromBody] UpdateToolBagToolsRequest request)
        {
            using var db = _dbFactory.CreateDbContext();
            var bag = await db.ToolBags.Include(b => b.Tools).FirstOrDefaultAsync(b => b.Id == id);
            if (bag == null) return NotFound();

            // Replace assignments with the provided list
            var existing = await db.ToolBagTools.Where(t => t.ToolBagId == id).ToListAsync();
            db.ToolBagTools.RemoveRange(existing);

            var toAdd = (request.ToolIds ?? new List<Guid>()).Distinct()
                .Select(tid => new ToolBagTool { ToolBagId = id, McpToolId = tid }).ToList();
            if (toAdd.Count > 0) db.ToolBagTools.AddRange(toAdd);
            bag.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return NoContent();
        }

        private static ToolBagDto ToDto(ToolBag bag)
        {
            return new ToolBagDto
            {
                Id = bag.Id,
                Name = bag.Name,
                Description = bag.Description,
                CreatedAt = bag.CreatedAt,
                UpdatedAt = bag.UpdatedAt,
                ToolIds = bag.Tools?.Select(t => t.McpToolId).ToList() ?? new List<Guid>()
            };
        }
    }

    public class ToolBagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<Guid> ToolIds { get; set; } = new();
    }

    public class CreateToolBagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateToolBagRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateToolBagToolsRequest
    {
        public List<Guid>? ToolIds { get; set; }
    }
}
