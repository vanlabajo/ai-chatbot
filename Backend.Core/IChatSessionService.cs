using Backend.Core.Models;

namespace Backend.Core
{
    public interface IChatSessionService
    {
        Task SaveSessionAsync(ChatSession session);
        Task<ChatSession?> GetSessionByIdAsync(string userId, string sessionId);
        Task<IEnumerable<ChatSession>> GetAllSessionsForUserAsync(string userId);
        Task DeleteSessionAsync(string userId, string sessionId);
    }
}
