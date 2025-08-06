
using Microsoft.AspNetCore.Mvc;
using OllamaFarmer.Server.Data.Interfaces;
using OllamaFarmer.Server.Dto;
using OllamaFarmer.Server.Services.Interfaces;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppChatController(IAppChatService chatService, ILogger<AppChatController> logger, IChatServerRepository serverRepository)
        : ControllerBase
    {

        [HttpGet("{id}")]
        public async Task<ActionResult<AppChatDto>> GetChat(Guid id)
        {
            var chat = await chatService.GetChatAsync(id);
            if (chat == null)
                return NotFound();
            return new AppChatDto(chat);
        }

        [HttpPost]
        public async Task<ActionResult<AppChatDto>> CreateChat([FromBody] CreateChatDto request)
        {
            var chat = await chatService.CreateChatAsync(request.ServerId, request.Name, request.Model, request.SystemMessage, 
                new() { 
                    Temperature = request.Temperature,
                    TopP = request.TopP,
                    FrequencyPenalty = request.FrequencyPenalty,
                    PresencePenalty = request.PresencePenalty,
                    EnabledToolIds = request.EnabledToolIds
                }
            );

            return CreatedAtAction(nameof(GetChat), new { id = chat.Id }, chat);
        }

        [HttpGet("")]
        public async Task<AppChatDetailsResponse> GetChatsAsync([FromQuery] Guid? serverId, AppChatDetails? filter, int cursor = 0, int pageSize = 10)
        {
            var chats = await chatService.ListChatsAsync(cursor, pageSize);

            // Fix CS0266: Await all tasks and project to AppChatDetails synchronously
            var dbChats = chats
                .Skip(cursor * pageSize)
                .Take(pageSize);

            if(serverId.HasValue)
                dbChats = dbChats.Where(c => c.ChatServerId == serverId.Value);

            var chatDetailsTasks = dbChats
                .Select(async c =>
                {
                    var chatServer = await serverRepository.GetChatServerByIdAsync(c.ChatServerId);
                    // Fix CS8602: Use null-conditional operator and null-coalescing operator
                    var serverName = chatServer?.Name ?? string.Empty;
                    return new AppChatDetails(c, serverName);
                })
                .ToList();

            var chatDetails = await Task.WhenAll(chatDetailsTasks);

            return new AppChatDetailsResponse
            {
                Cursor = cursor,
                FilteredCount = chats.Count,
                TotalCount = chats.Count,
                ChatDetails = chatDetails,
                Filter = filter
            };
        }

        [HttpPost("{id}")]
        public async Task<AppMessageDto?> SendMessageAsync(Guid id, [FromBody] SendMessageDto request)
        {
            var chat = await chatService.GetChatAsync(id);
            if (chat == null)
                return null;
            chat = await chatService.AddMessageAsync(chat, request.Message, request.Role, request.Images ?? new List<string>());
            chat.Options.EnabledToolIds = request.EnabledToolIds;
            await chatService.UpdateChatOptions(chat.Id, chat.Options);
            chat = await chatService.SubmitChatAsync(chat.ChatServerId, chat);
            return new AppMessageDto(chat.Messages.OrderBy(m => m.Index).LastOrDefault(), chat.Options.EnabledToolIds);
        }

        [HttpPost("{id}/resubmit")]
        public async Task<AppMessageDto?> ResubmitAsync(Guid id)
        {
            var chat = await chatService.GetChatAsync(id);
            if (chat == null)
                return null;
            chat = await chatService.SubmitChatAsync(chat.ChatServerId, chat);
            return new AppMessageDto(chat.Messages.OrderBy(m => m.Index).LastOrDefault(), chat.Options.EnabledToolIds);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AddMessageAsync([FromQuery] Guid serverId, Guid id, [FromBody] SendMessageDto request)
        {
            var chat = await chatService.GetChatAsync(id);
            if (chat == null)
                return NotFound();
            chat = await chatService.AddMessageAsync(chat, request.Message, request.Role, request.Images ?? new List<string>());
            return Ok();
        }

        [HttpDelete("{id}/{messageId}")]
        public async Task<IActionResult> DeleteMessageAsync(Guid id, Guid messageId)
        {
            await chatService.DeleteMessageAsync(messageId);
            return NoContent();
        }

        [HttpPut("{id}/{messageId}")]
        public async Task<IActionResult> UpdateMessageAsync(Guid id, Guid messageId, [FromBody] SendMessageDto request)
        {
            var chat = await chatService.GetChatAsync(id);
            if (chat == null)
                return NotFound();
            await chatService.UpdateMessageAsync(messageId, request.Role, request.Message);
            return NoContent();
        }


        [HttpPost("{id}/create-model")]
        public async Task<CreateModelFromChatResponse> CreateModelAsync(Guid id, [FromBody] ModelRequest request)
        {
            var safeName = request.NewModelName.Replace(" ", "_").Replace("/", "_").Replace(".", "_").ToLower();
            request.NewModelName = safeName;
            request.NewModelName = request.NewModelName.TrimEnd('.').TrimEnd('_');
            await chatService.CreateModelFromChatAsync(id, request);

            return new CreateModelFromChatResponse
            {
                Success = true,
                Model = request.NewModelName,
            };
        }

        [HttpPut("{id}/options")]
        public async Task<IActionResult> UpdateChatOptionsAsync(Guid id, [FromBody] Models.AppChatOptions request)
        {
            var chat = await chatService.GetChatAsync(id);
            if (chat == null)
                return NotFound();
            
            await chatService.UpdateChatOptions(chat.Id, request);
            return NoContent();
        }

        [HttpPost("{id}/clone")]
        public async Task<ActionResult<AppChatDto>> CloneChatAsync(Guid id, [FromBody] CloneChatRequest? request = null)
        {
            try
            {
                var clonedChat = await chatService.CloneChatAsync(id, request?.Name);
                return CreatedAtAction(nameof(GetChat), new { id = clonedChat.Id }, new AppChatDto(clonedChat));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to clone chat {ChatId}: {Message}", id, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cloning chat {ChatId}", id);
                return StatusCode(500, "An error occurred while cloning the chat.");
            }
        }

    }

    public class CloneChatRequest
    {
        public string? Name { get; set; }
    }
}
