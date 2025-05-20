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

        public async Task SendMessage(string message, string? sessionId = null)
        {
            var user = Context.User?.GetNameIdentifierId()
                ?? throw new BadRequestException("User identity is not available.");

            if (string.IsNullOrWhiteSpace(message))
                throw new BadRequestException("Message cannot be empty.");

            var sessions = await GetOrCreateSessions(user);
            var session = GetOrCreateSession(sessions, sessionId);

            session.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = message });
            await Clients.Caller.SendAsync(HubEventNames.ResponseStreamStart);

            var aiResponseBuilder = new StringBuilder();
            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(session.Messages))
            {
                aiResponseBuilder.Append(response);
                await Clients.Caller.SendAsync(HubEventNames.ResponseStreamChunk, response);
            }

            var aiResponse = aiResponseBuilder.ToString();
            session.Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = aiResponse });

            // Generate subject if missing
            if (string.IsNullOrEmpty(session.Subject))
            {
                session.Subject = await GenerateConversationSubject(session.Messages);
                await Clients.Caller.SendAsync(HubEventNames.SessionSubjectUpdated, session.Id, session.Subject);
            }

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

            // use for simulation only
            //var session = new ChatSession
            //{
            //    Id = sessionId,
            //    Messages = new List<ChatMessage>
            //    {
            //        new ChatMessage { Role = ChatRole.User, Content = "Hello, how are you?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "I'm fine, thank you!", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "What can you do?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "I can assist you with various tasks.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "Can you tell me a joke?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "Sure! Why did the scarecrow win an award? Because he was outstanding in his field!", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "That's funny!", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "I'm glad you liked it!", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "What else can you do?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "I can help you with programming, provide information, and much more!", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "Can you help me with my homework?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "Of course! What subject do you need help with?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "Math.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "Sure! What math problem do you need help with?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "Can you solve this equation: 2x + 3 = 7?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "Yes! To solve for x, subtract 3 from both sides: 2x = 4. Then divide by 2: x = 2.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "Great! Thanks for your help.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "You're welcome! If you have more questions, feel free to ask.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "What is the capital of France?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "The capital of France is Paris.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "Can you tell me more about Paris?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "Paris is known for its art, fashion, and culture. It's home to the Eiffel Tower and the Louvre Museum.", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.User, Content = "What is the Eiffel Tower?", Timestamp = DateTime.UtcNow },
            //        new ChatMessage { Role = ChatRole.Assistant, Content = "The Eiffel Tower is a wrought-iron lattice tower on the Champ de Mars in Paris. It was completed in 1889 and is one of the most recognizable structures in the world.", Timestamp = DateTime.UtcNow },
            //    }
            //};

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

        private async Task<string> GenerateConversationSubject(IEnumerable<ChatMessage> messages)
        {
            var subjectPrompt = new ChatMessage
            {
                Role = ChatRole.User,
                Content = "What is the subject of this conversation? Please respond with at most three words."
            };

            var messagesWithPrompt = messages.Append(subjectPrompt).ToList();
            var builder = new StringBuilder();

            await foreach (var response in _openAiService.GetChatResponseStreamingAsync(messagesWithPrompt))
            {
                builder.Append(response);
            }

            return builder.ToString();
        }
    }
}
