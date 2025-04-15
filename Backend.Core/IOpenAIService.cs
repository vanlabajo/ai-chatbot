using Backend.Core.Models;

namespace Backend.Core
{
    public interface IOpenAIService
    {
        Task<ChatMessage> GetChatResponseAsync(IEnumerable<ChatMessage> messages);
        IAsyncEnumerable<ChatMessage> GetChatResponseStreamingAsync(IEnumerable<ChatMessage> messages);
    }
}
