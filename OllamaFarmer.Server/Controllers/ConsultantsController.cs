using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultantsController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<ConsultantsController> _logger;

        public ConsultantsController(IDbContextFactory<AppDbContext> dbFactory, ILogger<ConsultantsController> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultantDto>>> GetAllAsync([FromQuery] Guid? serverId = null)
        {
            using var db = _dbFactory.CreateDbContext();
            var query = db.Consultants.AsQueryable();
            if (serverId.HasValue)
                query = query.Where(c => c.ChatServerId == serverId.Value);
            var items = await query.OrderBy(c => c.Topic).ThenBy(c => c.Name).ToListAsync();
            return Ok(items.Select(ConsultantDto.FromEntity));
        }

        [HttpGet("{id}", Name = "GetConsultantById")]
        public async Task<ActionResult<ConsultantDto>> GetAsync(Guid id)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = await db.Consultants.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();
            return Ok(ConsultantDto.FromEntity(entity));
        }

        [HttpPost]
        public async Task<ActionResult<ConsultantDto>> CreateAsync([FromBody] CreateConsultantRequest request)
        {
            using var db = _dbFactory.CreateDbContext();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required");

            var model = await db.Models.FirstOrDefaultAsync(m => m.Id == request.ChatModelId);
            if (model == null) return BadRequest("Chat model not found");

            var entity = new Consultant
            {
                ConsultantId = await GenerateUniqueConsultantCodeAsync(db),
                Name = request.Name.Trim(),
                Topic = request.Topic?.Trim() ?? "General",
                ChatModelId = request.ChatModelId,
                ChatServerId = model.ChatServerId,
                ExpertiseLevel = request.ExpertiseLevel,
                SystemMessage = request.SystemMessage,
                Temperature = request.Temperature,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Consultants.Add(entity);
            await db.SaveChangesAsync();
            var dto = ConsultantDto.FromEntity(entity);
            return CreatedAtRoute("GetConsultantById", new { id = entity.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ConsultantDto>> UpdateAsync(Guid id, [FromBody] UpdateConsultantRequest request)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = await db.Consultants.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Topic)) entity.Topic = request.Topic.Trim();
            if (request.ChatModelId.HasValue && request.ChatModelId != entity.ChatModelId)
            {
                var model = await db.Models.FirstOrDefaultAsync(m => m.Id == request.ChatModelId);
                if (model == null) return BadRequest("Chat model not found");
                entity.ChatModelId = request.ChatModelId.Value;
                entity.ChatServerId = model.ChatServerId;
            }
            if (request.ExpertiseLevel.HasValue) entity.ExpertiseLevel = request.ExpertiseLevel.Value;
            entity.SystemMessage = request.SystemMessage ?? entity.SystemMessage;
            if (request.Temperature.HasValue) entity.Temperature = request.Temperature.Value;
            if (request.TopP.HasValue) entity.TopP = request.TopP.Value;
            if (request.FrequencyPenalty.HasValue) entity.FrequencyPenalty = request.FrequencyPenalty.Value;
            if (request.PresencePenalty.HasValue) entity.PresencePenalty = request.PresencePenalty.Value;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Ok(ConsultantDto.FromEntity(entity));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(Guid id)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = await db.Consultants.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();
            db.Consultants.Remove(entity);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("by-code/{consultantId}")]
        public async Task<ActionResult<ConsultantDto>> GetByConsultantIdAsync(string consultantId)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = await db.Consultants.FirstOrDefaultAsync(c => c.ConsultantId == consultantId);
            if (entity == null) return NotFound();
            return Ok(ConsultantDto.FromEntity(entity));
        }

        private static async Task<string> GenerateUniqueConsultantCodeAsync(AppDbContext db)
        {
            while (true)
            {
                var code = $"CONS-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                var exists = await db.Consultants.AnyAsync(c => c.ConsultantId == code);
                if (!exists) return code;
            }
        }
    }

    public record CreateConsultantRequest(
        string Name,
        string Topic,
        Guid ChatModelId,
        ConsultantExpertiseLevel ExpertiseLevel,
        string? SystemMessage,
        float Temperature = 0.7f,
        float TopP = 1.0f,
        float FrequencyPenalty = 0.0f,
        float PresencePenalty = 0.0f
    );

    public class UpdateConsultantRequest
    {
        public string? Name { get; set; }
        public string? Topic { get; set; }
        public Guid? ChatModelId { get; set; }
        public ConsultantExpertiseLevel? ExpertiseLevel { get; set; }
        public string? SystemMessage { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
    }

    public class ConsultantDto
    {
        public Guid Id { get; set; }
        public string ConsultantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ChatModelId { get; set; }
        public Guid ChatServerId { get; set; }
        public string? SystemMessage { get; set; }
        public ConsultantExpertiseLevel ExpertiseLevel { get; set; }
        public float Temperature { get; set; }
        public float TopP { get; set; }
        public float FrequencyPenalty { get; set; }
        public float PresencePenalty { get; set; }

        public static ConsultantDto FromEntity(Consultant c) => new ConsultantDto
        {
            Id = c.Id,
            ConsultantId = c.ConsultantId,
            Name = c.Name,
            Topic = c.Topic,
            ChatModelId = c.ChatModelId,
            ChatServerId = c.ChatServerId,
            SystemMessage = c.SystemMessage,
            ExpertiseLevel = c.ExpertiseLevel,
            Temperature = c.Temperature,
            TopP = c.TopP,
            FrequencyPenalty = c.FrequencyPenalty,
            PresencePenalty = c.PresencePenalty
        };
    }
}
