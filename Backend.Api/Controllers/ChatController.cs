using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Runtime.CompilerServices;
using System.Text;

namespace Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(IOpenAIService openAiService, ICacheService cacheService) : ControllerBase
    {
        private readonly IOpenAIService _openAiService = openAiService;
        private readonly ICacheService _cacheService = cacheService;

        [HttpPost]
        [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty.");

            var user = User.GetNameIdentifierId();
            if (user == null)
                return BadRequest("User identity is not available.");

            var conversations = await _cacheService.GetAsync<List<ChatMessage>>($"conversations-{user}", cancellationToken);
            if (conversations == null || conversations.Count == 0)
            {
                conversations = [];
            }

            conversations.Add(new ChatMessage { Role = "user", Content = request.Message });

            var response = await _openAiService.GetChatResponseAsync(conversations);

            conversations.Add(new ChatMessage { Role = "assistant", Content = response });
            await _cacheService.SetAsync($"conversations-{user}", conversations, cancellationToken: cancellationToken);

            return Ok(new ChatResponse { Response = response });
        }

        [HttpPost("stream")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async IAsyncEnumerable<string> StreamChat([FromBody] ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new BadRequestException("Message cannot be empty.");

            var user = User.GetNameIdentifierId() ?? throw new BadRequestException("User identity is not available.");

            var conversations = await _cacheService.GetAsync<List<ChatMessage>>($"conversations-{user}", cancellationToken);
            if (conversations == null || conversations.Count == 0)
            {
                conversations = [];
            }

            Response.Headers.Append("Content-Type", "text/event-stream");

            conversations.Add(new ChatMessage { Role = "user", Content = request.Message });

            var chatResponseBuilder = new StringBuilder();
            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(conversations))
            {
                chatResponseBuilder.Append(response);
                yield return response;
            }

            conversations.Add(new ChatMessage { Role = "assistant", Content = chatResponseBuilder.ToString() });
        }
    }
}
