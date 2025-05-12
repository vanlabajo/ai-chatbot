using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Net.WebSockets;
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

        [HttpGet("ws")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task HandleWebSocket(CancellationToken cancellationToken)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await ProcessWebSocketMessages(webSocket, cancellationToken);
        }

        private async Task ProcessWebSocketMessages(WebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (string.IsNullOrEmpty(message)) throw new BadRequestException("Message cannot be empty.");

                var user = User.GetNameIdentifierId() ?? throw new BadRequestException("User identity is not available.");

                var conversations = await _cacheService.GetAsync<List<ChatMessage>>($"conversations-{user}", cancellationToken) ?? new List<ChatMessage>();
                conversations.Add(new ChatMessage { Role = "user", Content = message });

                var chatResponseBuilder = new StringBuilder();
                await foreach (var response in _openAiService.GetChatResponseStreamingAsync(conversations))
                {
                    chatResponseBuilder.Append(response);
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cancellationToken);
                }

                conversations.Add(new ChatMessage { Role = "assistant", Content = chatResponseBuilder.ToString() });
                await _cacheService.SetAsync($"conversations-{user}", conversations, cancellationToken: cancellationToken);

                // Send an "end message" to indicate the stream is complete
                var endMessage = Encoding.UTF8.GetBytes("[END]");
                await webSocket.SendAsync(new ArraySegment<byte>(endMessage), WebSocketMessageType.Text, true, cancellationToken);

                // Wait for the next message from the client
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
