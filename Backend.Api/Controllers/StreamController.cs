using Backend.Core;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Backend.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class StreamController(IOpenAIService openAiService, ICacheService cacheService) : ControllerBase
    {
        private readonly IOpenAIService _openAiService = openAiService;
        private readonly ICacheService _cacheService = cacheService;

        [HttpGet("chat/{sessionId?}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        [HttpGet("chat/sessions")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task GetSessions(CancellationToken cancellationToken)
        {
            var user = User.GetNameIdentifierId() ?? throw new BadRequestException("User identity is not available.");

            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");

            var responseStream = Response.Body;
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var sentSessionIds = new HashSet<string>();
            var sessionsWithMissingSubject = new Dictionary<string, string?>();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sessions = await _cacheService.GetAsync<List<ChatSession>>($"session-{user}", cancellationToken);
                    if (sessions != null)
                    {
                        foreach (var session in sessions)
                        {
                            // New session: send it and track if subject is missing
                            if (sentSessionIds.Add(session.SessionId))
                            {
                                var data = new ChatSession { SessionId = session.SessionId, Subject = session.Subject };
                                var json = JsonSerializer.Serialize(data, options);
                                var jsonBytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
                                await responseStream.WriteAsync(jsonBytes, cancellationToken);
                                await responseStream.FlushAsync(cancellationToken);

                                if (string.IsNullOrEmpty(session.Subject))
                                    sessionsWithMissingSubject[session.SessionId] = null;
                            }
                            // Existing session with missing subject: check if subject is now set
                            else if (sessionsWithMissingSubject.ContainsKey(session.SessionId) && !string.IsNullOrEmpty(session.Subject))
                            {
                                var data = new ChatSession { SessionId = session.SessionId, Subject = session.Subject };
                                var json = JsonSerializer.Serialize(data, options);
                                var jsonBytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
                                await responseStream.WriteAsync(jsonBytes, cancellationToken);
                                await responseStream.FlushAsync(cancellationToken);

                                // Remove from tracking since subject is now set
                                sessionsWithMissingSubject.Remove(session.SessionId);
                            }
                        }
                    }
                    await Task.Delay(1500, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }


        [HttpGet("chat/sessions/{sessionId}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task GetSessionMessages(string sessionId, CancellationToken cancellationToken)
        {
            var user = User.GetNameIdentifierId() ?? throw new BadRequestException("User identity is not available.");

            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");

            var responseStream = Response.Body;
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Only track message IDs that have been sent in this connection
            var sentMessageIds = new HashSet<string>();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sessions = await _cacheService.GetAsync<List<ChatSession>>($"session-{user}", cancellationToken);
                    if (sessions != null)
                    {
                        var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
                        if (session != null)
                        {
                            foreach (var message in session.Messages)
                            {
                                if (sentMessageIds.Add(message.ChatMessageId))
                                {
                                    // Only yield if this message hasn't been sent before
                                    var json = JsonSerializer.Serialize(message, options);
                                    var jsonBytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
                                    await responseStream.WriteAsync(jsonBytes, cancellationToken);
                                    await responseStream.FlushAsync(cancellationToken);
                                }
                            }
                        }
                    }
                    // Use a slightly longer delay to reduce polling frequency and CPU usage
                    await Task.Delay(1500, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
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
                        new() { Role = ChatRole.System, Content = "You are a helpful assistant, providing informative and concise answers to user queries. Your goal is to be informative, respectful, and helpful." },
                        new() { Role = ChatRole.System, Content = "You are trained to be a conversational AI assistant." },
                        new() { Role = ChatRole.System, Content = "You are available to answer questions on a wide range of topics, but you are not a medical professional, financial advisor, or lawyer." },
                        new() { Role = ChatRole.System, Content = "You will answer questions in a way that is easy to understand, avoiding technical jargon unless necessary." },
                        new() { Role = ChatRole.System, Content = "If you are asked to perform actions (e.g., make a booking), you will advise the user on how to do so, but you will not perform the action yourself." }
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
