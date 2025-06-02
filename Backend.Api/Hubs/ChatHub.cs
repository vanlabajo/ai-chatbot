using Backend.Core;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Identity.Web;
using System.Text;

namespace Backend.Api.Hubs
{
    [Authorize]
    public class ChatHub(IOpenAIService openAiService, ICacheService cacheService) : Hub
    {
        private readonly IOpenAIService _openAiService = openAiService;
        private readonly ICacheService _cacheService = cacheService;
        private const string RateLimitMessage = "You have exceeded the rate limit for requests. Please try again later.";

        public async Task SendMessage(string message, string? sessionId = null)
        {
            var user = Context.User?.GetNameIdentifierId()
                ?? throw new BadRequestException("User identity is not available.");

            if (string.IsNullOrWhiteSpace(message))
                throw new BadRequestException("Message cannot be empty.");

            var sessions = await GetOrCreateSessions(user);
            var session = GetOrCreateSession(sessions, sessionId);
            var sessionUpdate = new ChatSession
            {
                Id = session.Id,
                Timestamp = session.Timestamp
            };
            await Clients.Caller.SendAsync(HubEventNames.SessionUpdate, sessionUpdate);

            session.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = message });
            await Clients.Caller.SendAsync(HubEventNames.ResponseStreamStart);

            var aiResponseBuilder = new StringBuilder();
            string aiResponse;
            try
            {
                await foreach (var response in _openAiService.GetChatResponseStreamingAsync(session.Messages))
                {
                    aiResponseBuilder.Append(response);
                    await Clients.Caller.SendAsync(HubEventNames.ResponseStreamChunk, response);
                }
                aiResponse = aiResponseBuilder.ToString();
            }
            catch (OpenAIRateLimitException)
            {
                aiResponse = RateLimitMessage;
                await Clients.Caller.SendAsync(HubEventNames.ResponseStreamChunk, aiResponse);
            }
            session.Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = aiResponse });

            // Generate title if missing, but skip if rate limited
            try
            {
                if (string.IsNullOrEmpty(session.Title))
                {
                    session.Title = await GenerateConversationTitle(session.Messages);
                    sessionUpdate.Title = session.Title;
                    await Clients.Caller.SendAsync(HubEventNames.SessionUpdate, sessionUpdate);
                }
            }
            catch (OpenAIRateLimitException) { }

            await Clients.Caller.SendAsync(HubEventNames.ResponseStreamEnd);
            await _cacheService.SetAsync($"session-{user}", sessions);
        }

        public async Task GetHistory(string sessionId, int offset, int limit)
        {
            var user = Context.User?.GetNameIdentifierId()
                ?? throw new BadRequestException("User identity is not available.");

            var sessions = await GetOrCreateSessions(user);
            var session = sessions.FirstOrDefault(s => s.Id == sessionId)
                ?? throw new BadRequestException("Session not found.");

            await Clients.Caller.SendAsync(HubEventNames.HistoryStreamStart);

            var messages = session.Messages
                .Where(m => m.Role != ChatRole.System)
                .OrderByDescending(m => m.Timestamp)
                .Skip(offset)
                .Take(limit)
                .ToList();

            foreach (var message in messages)
            {
                await Clients.Caller.SendAsync(HubEventNames.HistoryStreamChunk, message);
            }

            await Clients.Caller.SendAsync(HubEventNames.HistoryStreamEnd);
        }

        private async Task<List<ChatSession>> GetOrCreateSessions(string user)
        {
            var sessions = await _cacheService.GetAsync<List<ChatSession>>($"session-{user}");
            if (sessions != null && sessions.Count > 0)
                return [.. sessions.OrderByDescending(s => s.Timestamp)];
            else
                return [];
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
                        new() { Role = ChatRole.System, Content = "You are a helpful assistant, providing informative and concise answers to user queries." },
                        new() { Role = ChatRole.System, Content = "You are trained to be a conversational AI assistant." },
                        new() { Role = ChatRole.System, Content = "You are not a medical, financial, or legal advisor." },
                        new() { Role = ChatRole.System, Content = "Avoid technical jargon unless necessary." },
                        new() { Role = ChatRole.System, Content = "You will not perform actions on behalf of the user." }
                    ]
                };
                sessions.Add(session);
            }

            return session;
        }

        private async Task<string> GenerateConversationTitle(IEnumerable<ChatMessage> messages)
        {
            var titlePrompt = new ChatMessage
            {
                Role = ChatRole.User,
                Content = "What is the title of this conversation? Please respond with at most four words. Do not include quotation marks or punctuation around the title."
            };

            var messagesWithPrompt = messages.Append(titlePrompt).ToList();
            var builder = new StringBuilder();

            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(messagesWithPrompt))
            {
                builder.Append(response);
            }

            return builder.ToString();
        }
    }
}
