using Backend.Core.Models;

namespace Backend.Core
{
    public interface IOpenAIService
    {
        Task<string> GetChatResponseAsync(IEnumerable<ChatMessage> messages);
        IAsyncEnumerable<string> GetChatResponseStreamingAsync(IEnumerable<ChatMessage> messages);
    }
}
