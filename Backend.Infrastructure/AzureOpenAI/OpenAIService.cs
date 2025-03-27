using Backend.Core;
using OpenAI.Chat;

namespace Backend.Infrastructure.AzureOpenAI
{
    public class OpenAIService : IOpenAIService
    {
        private readonly ChatClient _chatClient;

        public OpenAIService(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<string> GetChatResponseAsync(string message)
        {
            var response = await _chatClient.CompleteChatAsync(message);
            return response.Value.Content[0].Text;
        }
    }
}
