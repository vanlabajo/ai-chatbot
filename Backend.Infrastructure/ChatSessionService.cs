using Backend.Core;
using Backend.Core.Models;
using Backend.Core.Repositories;

namespace Backend.Infrastructure
{
    public class ChatSessionService(IChatSessionRepository chatSessionRepository) : IChatSessionService
    {
        private readonly IChatSessionRepository _chatSessionRepository = chatSessionRepository;

        public Task SaveSessionAsync(ChatSession session)
        {
            return _chatSessionRepository.SaveSessionAsync(session);
        }

        public Task<ChatSession?> GetSessionByIdAsync(string userId, string sessionId)
        {
            return _chatSessionRepository.GetSessionAsync(userId, sessionId);
        }

        public Task<IEnumerable<ChatSession>> GetAllSessionsForUserAsync(string userId)
        {
            return _chatSessionRepository.GetAllSessionsForUserAsync(userId);
        }

        public Task DeleteSessionAsync(string userId, string sessionId)
        {
            return _chatSessionRepository.DeleteSessionAsync(userId, sessionId);
        }
    }
}
