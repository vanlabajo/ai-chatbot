using Backend.Api.Hubs;
using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Text;

namespace Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(
        IOpenAIService openAiService,
        ICacheService cacheService,
        IChatSessionService chatSessionService,
        IHubContext<ChatHub> hubContext,
        IOptions<SystemPromptOptions> promptOptions) : ControllerBase
    {
        private readonly IOpenAIService _openAiService = openAiService;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IChatSessionService _chatSessionService = chatSessionService;
        private readonly IHubContext<ChatHub> _hubContext = hubContext;
        private readonly SystemPromptOptions _promptOptions = promptOptions.Value;
        private const string RateLimitMessage = "You have exceeded the rate limit for requests. Please try again later.";

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
            var session = GetOrCreateSession(sessions, user, request.SessionId);

            // Add the user's message to the session
            session.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = request.Message });

            string assistantResponse;
            try
            {
                // Generate the assistant's response
                assistantResponse = await _openAiService.GetChatResponseAsync(session.Messages);
            }
            catch (OpenAIRateLimitException)
            {
                assistantResponse = RateLimitMessage;
            }

            session.Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = assistantResponse });

            try
            {
                // Generate a title for the session if it doesn't already exist
                if (string.IsNullOrEmpty(session.Title))
                {
                    session.Title = await GenerateConversationTitle(session.Messages, cancellationToken);
                }
            }
            catch (OpenAIRateLimitException) { }

            await _chatSessionService.SaveSessionAsync(session, cancellationToken);
            // Save the updated sessions list back to the cache
            await _cacheService.SetAsync($"session-{user}", sessions, cancellationToken: cancellationToken);

            return Ok(new ChatResponse { Response = assistantResponse });
        }

        [HttpGet("sessions")]
        [ProducesResponseType(typeof(List<ChatSession>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        {
            var user = User.GetNameIdentifierId();
            if (user == null)
                return BadRequest("User identity is not available.");
            var sessions = await GetOrCreateSessions(user, cancellationToken);
            // Remove messages from the session objects before sending them to the client
            var sessionList = sessions.Select(s => new ChatSession
            {
                UserId = s.UserId,
                Id = s.Id,
                Title = s.Title,
                Timestamp = s.Timestamp
            }).ToList();
            return Ok(sessionList);
        }

        [HttpDelete("sessions/{sessionId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken cancellationToken)
        {
            var user = User.GetNameIdentifierId();
            if (user == null)
                return BadRequest("User identity is not available.");
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest("Session ID cannot be empty.");
            var sessions = await GetOrCreateSessions(user, cancellationToken);
            var session = sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null)
                return NotFound("Session not found.");
            await _chatSessionService.DeleteSessionAsync(user, sessionId, cancellationToken);
            sessions.Remove(session);
            await _cacheService.SetAsync($"session-{user}", sessions, cancellationToken: cancellationToken);
            await _hubContext.Clients.All.SendAsync(HubEventNames.SessionDelete, sessionId, cancellationToken);
            return NoContent();
        }

        [HttpPut("sessions/{sessionId}/title")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSessionTitle(string sessionId, [FromBody] string newTitle, CancellationToken cancellationToken)
        {
            var user = User.GetNameIdentifierId();
            if (user == null)
                return BadRequest("User identity is not available.");
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest("Session ID cannot be empty.");
            if (string.IsNullOrWhiteSpace(newTitle))
                return BadRequest("New title cannot be empty.");
            var sessions = await GetOrCreateSessions(user, cancellationToken);
            var session = sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null)
                return NotFound("Session not found.");
            session.Title = newTitle;
            await _chatSessionService.SaveSessionAsync(session, cancellationToken);
            await _cacheService.SetAsync($"session-{user}", sessions, cancellationToken: cancellationToken);
            await _hubContext.Clients.All.SendAsync(HubEventNames.SessionUpdate, session, cancellationToken);
            return NoContent();
        }

        private async Task<List<ChatSession>> GetOrCreateSessions(string user, CancellationToken cancellationToken)
        {
            var cacheKey = $"session-{user}";
            var sessions = await _cacheService.GetAsync<List<ChatSession>>(cacheKey, cancellationToken);
            if (sessions != null && sessions.Count > 0)
                return sessions;

            sessions = [.. (await _chatSessionService.GetAllSessionsForUserAsync(user, cancellationToken))];
            await _cacheService.SetAsync(cacheKey, sessions, cancellationToken: cancellationToken);
            return sessions;
        }

        private ChatSession GetOrCreateSession(List<ChatSession> sessions, string user, string? sessionId)
        {
            var session = !string.IsNullOrEmpty(sessionId)
                ? sessions.FirstOrDefault(s => s.Id == sessionId)
                : null;

            if (session == null)
            {
                session = new ChatSession
                {
                    UserId = user,
                    Id = sessionId ?? Guid.NewGuid().ToString(),
                    Messages = [.. _promptOptions.SystemPrompts.Select(p => new ChatMessage { Role = ChatRole.System, Content = p })]
                };
                sessions.Add(session);
            }

            return session;
        }

        private async Task<string> GenerateConversationTitle(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            var titlePrompt = new ChatMessage
            {
                Role = ChatRole.User,
                Content = "What is the title of this conversation? Please respond with at most four words. Do not include quotation marks or punctuation around the title."
            };

            var messagesWithPrompt = messages.Append(titlePrompt).ToList();
            var subjectBuilder = new StringBuilder();

            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(messagesWithPrompt))
            {
                subjectBuilder.Append(response);
            }

            return subjectBuilder.ToString();
        }
    }
}
