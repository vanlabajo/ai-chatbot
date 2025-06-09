using Backend.Core.Models;

namespace Backend.Core
{
    public interface IChatSessionService
    {
        Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
        Task<ChatSession?> GetSessionByIdAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ChatSession>> GetAllSessionsForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task DeleteSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    }
}
