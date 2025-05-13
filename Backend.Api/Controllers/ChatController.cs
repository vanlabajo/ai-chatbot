using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
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

            // Retrieve or create the chat sessions
            var sessions = await GetOrCreateSessions(user, cancellationToken);
            var session = GetOrCreateSession(sessions, request.SessionId);

            // Add the user's message to the session
            session.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = request.Message });

            // Generate the assistant's response
            var assistantResponse = await _openAiService.GetChatResponseAsync(session.Messages);
            session.Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = assistantResponse });

            // Generate a subject for the session if it doesn't already exist
            if (string.IsNullOrEmpty(session.Subject))
            {
                session.Subject = await GenerateConversationSubject(session.Messages, cancellationToken);
            }

            // Save the updated sessions list back to the cache
            await _cacheService.SetAsync($"session-{user}", sessions, cancellationToken: cancellationToken);

            return Ok(new ChatResponse { Response = assistantResponse });
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
