using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Exceptions;
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
            var session = GetOrCreateSession(sessions, request.SessionId);

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
                Id = s.Id,
                Title = s.Title,
                Timestamp = s.Timestamp
            }).ToList();
            return Ok(sessionList);
        }

        private async Task<List<ChatSession>> GetOrCreateSessions(string user, CancellationToken cancellationToken)
        {
            var sessions = await _cacheService.GetAsync<List<ChatSession>>($"session-{user}", cancellationToken);
            return sessions ?? [];
        }

        private static ChatSession GetOrCreateSession(List<ChatSession> sessions, string? sessionId)
        {
            var session = !string.IsNullOrEmpty(sessionId)
                ? sessions.FirstOrDefault(s => s.Id == sessionId)
                : null;

            if (session == null)
            {
                session = new ChatSession
                {
                    Id = sessionId ?? Guid.NewGuid().ToString(),
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
