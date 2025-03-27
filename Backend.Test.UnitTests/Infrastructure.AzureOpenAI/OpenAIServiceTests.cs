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
            var expectedResponse = "Hello, how can I help you?";
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.User,
                content: new ChatMessageContent(ChatMessageContentPart.CreateTextPart(expectedResponse)));

            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completion, mockPipelineResponse.Object);


            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<ChatMessage[]>()))
                .ReturnsAsync(completionResponse);


            // Act
            var result = await _openAIService.GetChatResponseAsync("Hello");

            // Assert
            Assert.Equal(expectedResponse, result);
        }
    }
}
