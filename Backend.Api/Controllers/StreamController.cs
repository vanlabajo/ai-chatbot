using Backend.Core;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Net.WebSockets;
using System.Text;

namespace Backend.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class StreamController(IOpenAIService openAiService, ICacheService cacheService) : ControllerBase
    {
        private readonly IOpenAIService _openAiService = openAiService;
        private readonly ICacheService _cacheService = cacheService;

        [HttpGet("chat")]
        public async Task Chat(string? sessionId, CancellationToken cancellationToken)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await ProcessWebSocketMessages(webSocket, sessionId, cancellationToken);
        }

        private async Task ProcessWebSocketMessages(WebSocket webSocket, string? sessionId, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            var user = User.GetNameIdentifierId() ?? throw new BadRequestException("User identity is not available.");
            var sessions = await GetOrCreateSessions(user, cancellationToken);
            var session = GetOrCreateSession(sessions, sessionId);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (string.IsNullOrEmpty(message)) throw new BadRequestException("Message cannot be empty.");

                session.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = message });

                var aiResponse = await GenerateAIResponse(webSocket, session.Messages, cancellationToken);
                session.Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = aiResponse });

                if (string.IsNullOrEmpty(session.Subject))
                {
                    session.Subject = await GenerateConversationSubject(session.Messages, cancellationToken);
                }

                // Save the updated sessions list back to the cache
                await _cacheService.SetAsync($"session-{user}", sessions, cancellationToken: cancellationToken);

                // Send an "end message" to indicate the stream is complete
                var endMessage = Encoding.UTF8.GetBytes("[END]");
                await webSocket.SendAsync(new ArraySegment<byte>(endMessage), WebSocketMessageType.Text, true, cancellationToken);

                // Wait for the next message from the client
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private async Task<List<ChatSession>> GetOrCreateSessions(string user, CancellationToken cancellationToken)
        {
            var sessions = await _cacheService.GetAsync<List<ChatSession>>($"session-{user}", cancellationToken);
            return sessions ?? [];
        }

        private static ChatSession GetOrCreateSession(List<ChatSession> sessions, string? sessionId)
        {
            var session = !string.IsNullOrEmpty(sessionId)
                ? sessions.FirstOrDefault(s => s.SessionId == sessionId)
                : null;

            if (session == null)
            {
                session = new ChatSession
                {
                    SessionId = sessionId ?? Guid.NewGuid().ToString(),
                    Messages =
                    [
                        new() { Role = ChatRole.System, Content = "You are Rick from the TV show Rick & Morty. Pretend to be Rick." },
                        new() { Role = ChatRole.User, Content = "Introduce yourself." }
                    ]
                };
                sessions.Add(session);
            }

            return session;
        }

        private async Task<string> GenerateAIResponse(WebSocket webSocket, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            var chatResponseBuilder = new StringBuilder();
            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(messages))
            {
                chatResponseBuilder.Append(response);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cancellationToken);
            }

            return chatResponseBuilder.ToString();
        }

        private async Task<string> GenerateConversationSubject(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            var subjectRequest = new ChatMessage
            {
                Role = ChatRole.User,
                Content = "What is the subject of this conversation? Please respond with at most three words."
            };

            var messagesWithSubjectRequest = messages.Append(subjectRequest).ToList();
            var subjectBuilder = new StringBuilder();

            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(messagesWithSubjectRequest))
            {
                subjectBuilder.Append(response);
            }

            return subjectBuilder.ToString();
        }
    }
}
