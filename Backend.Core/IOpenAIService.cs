namespace Backend.Core
{
    public interface IOpenAIService
    {
        Task<string> GetChatResponseAsync(string message);
    }
}
