using Backend.Infrastructure.AzureOpenAI;
using Moq;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace Backend.Test.UnitTests.Infrastructure.AzureOpenAI
{
    public class OpenAIServiceTests
    {
        private readonly Mock<ChatClient> _mockChatClient;
        private readonly OpenAIService _openAIService;

        public OpenAIServiceTests()
        {
            _mockChatClient = new Mock<ChatClient>();
            _openAIService = new OpenAIService(_mockChatClient.Object);
        }

        [Fact]
        public async Task GetChatResponseAsync_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = new Backend.Core.Models.ChatMessage
            {
                Role = ChatMessageRole.System.ToString(),
                Content = "Hello, how can I help you?"
            };
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent(expectedResponse.Content));

            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completion, mockPipelineResponse.Object);

            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ReturnsAsync(completionResponse);

            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };

            // Act
            var result = await _openAIService.GetChatResponseAsync(chatMessages);

            // Assert
            Assert.Equal(expectedResponse.Role, result.Role);
            Assert.Equal(expectedResponse.Content, result.Content);
        }
    }
}
