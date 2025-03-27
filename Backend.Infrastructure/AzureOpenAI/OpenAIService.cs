using Backend.Core;
using OpenAI.Chat;

namespace Backend.Infrastructure.AzureOpenAI
{
    public class OpenAIService(ChatClient chatClient) : IOpenAIService
    {
        private readonly ChatClient _chatClient = chatClient;

        public async Task<Core.Models.ChatMessage> GetChatResponseAsync(IEnumerable<Core.Models.ChatMessage> messages)
        {
            var chatMessages = new List<ChatMessage>();
            foreach (var message in messages)
            {
                if (message.Role.Contains("system", StringComparison.CurrentCultureIgnoreCase))
                {
                    chatMessages.Add(new SystemChatMessage(message.Content));
                }
                else if (message.Role.Contains("user", StringComparison.CurrentCultureIgnoreCase))
                {
                    chatMessages.Add(new UserChatMessage(message.Content));
                }
                else if (message.Role.Contains("assistant", StringComparison.CurrentCultureIgnoreCase))
                {
                    chatMessages.Add(new AssistantChatMessage(message.Content));
                }
            }

            var response = await _chatClient.CompleteChatAsync(chatMessages);
            return new Core.Models.ChatMessage
            {
                Role = response.Value.Role.ToString(),
                Content = response.Value.Content[0].Text
            };
        }
    }
}
