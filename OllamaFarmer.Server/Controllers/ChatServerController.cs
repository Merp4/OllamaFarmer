using Microsoft.AspNetCore.Mvc;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Data.Interfaces;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatServerController(ILogger<ChatServerController> logger, IChatServerRepository chatServerRepository)
        : ControllerBase
    {


        [HttpGet("{id}")]
        public async Task<ActionResult<ChatServerDto>> GetChatServerAsync(Guid id)
        {
            var server = await chatServerRepository.GetChatServerByIdAsync(id);
            if (server == null)
                return NotFound();
            return Ok(new ChatServerDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                Version = server.Version,
                CreatedAt = server.CreatedAt,
                UpdatedAt = server.UpdatedAt,
                Uri = server.Uri
            });
        }


        [HttpGet("all")]
        public async Task<IEnumerable<ChatServerDto>> GetAllChatServersAsync()
        {
            var servers = await chatServerRepository.GetAllChatServersAsync();
            return servers.Select(s => new ChatServerDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Version = s.Version,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                Uri = s.Uri
            });
        }

        // CRUD
        [HttpPost]
        public async Task<ActionResult<ChatServerDto>> CreateChatServerAsync([FromBody] ChatServerDto chatServerDto)
        {
            if (chatServerDto == null)
                return BadRequest("Chat server data is required.");

            var chatServer = new ChatServer
            {
                Id = chatServerDto.Id,
                Name = chatServerDto.Name,
                Description = chatServerDto.Description,
                Version = chatServerDto.Version,
                CreatedAt = chatServerDto.CreatedAt,
                UpdatedAt = chatServerDto.UpdatedAt,
                Uri = chatServerDto.Uri
            };

            await chatServerRepository.CreateChatServerAsync(chatServer);
            return CreatedAtAction(nameof(GetChatServerAsync), new { id = chatServer.Id }, chatServerDto);
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult<ChatServerDto>> UpdateChatServerAsync(Guid id, [FromBody] ChatServerDto chatServerDto)
        {
            if (chatServerDto == null || chatServerDto.Id != id)
                return BadRequest("Chat server data is invalid.");

            var existingServer = await chatServerRepository.GetChatServerByIdAsync(id);
            if (existingServer == null)
                return NotFound();

            existingServer.Name = chatServerDto.Name;
            existingServer.Description = chatServerDto.Description;
            existingServer.Version = chatServerDto.Version;
            existingServer.UpdatedAt = DateTimeOffset.UtcNow;
            existingServer.Uri = chatServerDto.Uri;

            await chatServerRepository.UpdateChatServerAsync(existingServer);
            return Ok(chatServerDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatServerAsync(Guid id)
        {
            var existingServer = await chatServerRepository.GetChatServerByIdAsync(id);
            if (existingServer == null)
                return NotFound();

            await chatServerRepository.DeleteChatServerAsync(id);
            return NoContent();
        }




    }

    public class ChatServerDto
    {
        // ala ChatServer
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
            
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public Uri Uri { get; set; } = null!;
    }
}
