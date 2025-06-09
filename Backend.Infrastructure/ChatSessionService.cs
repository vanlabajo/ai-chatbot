using Backend.Core;
using Backend.Core.Models;
using Backend.Core.Repositories;

namespace Backend.Infrastructure
{
    public class ChatSessionService(IChatSessionRepository chatSessionRepository) : IChatSessionService
    {
        private readonly IChatSessionRepository _chatSessionRepository = chatSessionRepository;

        public Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
        {
            return _chatSessionRepository.SaveSessionAsync(session, cancellationToken);
        }

        public Task<ChatSession?> GetSessionByIdAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            return _chatSessionRepository.GetSessionAsync(userId, sessionId, cancellationToken);
        }

        public Task<IEnumerable<ChatSession>> GetAllSessionsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return _chatSessionRepository.GetAllSessionsForUserAsync(userId, cancellationToken);
        }

        public Task DeleteSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            return _chatSessionRepository.DeleteSessionAsync(userId, sessionId, cancellationToken);
        }
    }
}
