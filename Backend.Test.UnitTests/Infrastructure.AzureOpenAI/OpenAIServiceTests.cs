using Azure;
using Backend.Core;
using Backend.Core.Exceptions;
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
        private readonly Mock<ITokenizerService> _mockTokenizerService;
        private readonly OpenAIService _openAIService;

        public OpenAIServiceTests()
        {
            _mockChatClient = new Mock<ChatClient>();
            _mockTokenizerService = new Mock<ITokenizerService>();
            _openAIService = new OpenAIService(_mockChatClient.Object, _mockTokenizerService.Object);
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

        [Fact]
        public async Task GetChatResponseAsync_WithTooManyTokens_SummarizesMessages()
        {
            // Arrange
            var completion1 = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent("This is a summary of the previous conversations..."));
            var completion2 = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent("Get a professional trainer..."));

            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse1 = ClientResult.FromValue(completion1, mockPipelineResponse.Object);
            var completionResponse2 = ClientResult.FromValue(completion2, mockPipelineResponse.Object);


            _mockChatClient.SetupSequence(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ReturnsAsync(completionResponse1)
                .ReturnsAsync(completionResponse2);

            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };

            _mockTokenizerService.Setup(service => service.CountTokens(It.IsAny<string>()))
                .Returns(8001);

            // Act
            var result = await _openAIService.GetChatResponseAsync(chatMessages);

            // Assert
            _mockChatClient.Verify(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default), Times.Exactly(2));
        }

        [Fact]
        public async Task GetChatResponseAsync_WithResponseValueIsNull_ThrowsNotFoundException()
        {
            // Arrange
            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromOptionalValue<ChatCompletion?>(null, mockPipelineResponse.Object);
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
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _openAIService.GetChatResponseAsync(chatMessages));
            // Assert
            Assert.Equal("Chat response not found.", exception.Message);
        }

        [Fact]
        public async Task GetChatResponseAsync_WithRequestFailedException_ThrowsBadRequestException()
        {
            // Arrange
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent("Hello, how can I help you?"));
            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completion, mockPipelineResponse.Object);
            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ThrowsAsync(new RequestFailedException(400, "Request failed."));
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => _openAIService.GetChatResponseAsync(chatMessages));
            // Assert
            Assert.Equal("Invalid request to OpenAI API.", exception.Message);
        }

        [Fact]
        public async Task GetChatResponseAsync_WithRequestFailedException_ThrowsNotFoundException()
        {
            // Arrange
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent("Hello, how can I help you?"));
            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completion, mockPipelineResponse.Object);
            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ThrowsAsync(new RequestFailedException(404, "Request failed."));
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };
            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _openAIService.GetChatResponseAsync(chatMessages));
            // Assert
            Assert.Equal("OpenAI API endpoint not found.", exception.Message);
        }

        [Fact]
        public async Task GetChatResponseAsync_WithRequestFailedException_ThrowsGenericException()
        {
            // Arrange
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent("Hello, how can I help you?"));
            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completion, mockPipelineResponse.Object);
            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ThrowsAsync(new RequestFailedException("Request failed."));
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };
            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => _openAIService.GetChatResponseAsync(chatMessages));
            // Assert
            Assert.Equal("An unexpected error occurred: Request failed.", exception.Message);
        }

        [Fact]
        public async Task GetChatResponseAsync_WithResponseIsNull_ThrowsNotFoundException()
        {
            // Arrange
            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ReturnsAsync((ClientResult<ChatCompletion>?)null);
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };
            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _openAIService.GetChatResponseAsync(chatMessages));
            // Assert
            Assert.Equal("Chat response not found.", exception.Message);
        }

        [Fact]
        public async Task GetChatResponseAsync_WithResponseIsEmpty_ReturnsEmpty()
        {
            // Arrange
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System);
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
            Assert.Equal(ChatMessageRole.System.ToString(), result.Role);
            Assert.Equal(string.Empty, result.Content);
        }

        [Fact]
        public async Task GetChatResponseAsync_WithSummaryResponseIsEmpty_ReturnsEmpty()
        {
            // Arrange
            var completion1 = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System);
            var completion2 = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent("Get a professional trainer..."));

            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse1 = ClientResult.FromValue(completion1, mockPipelineResponse.Object);
            var completionResponse2 = ClientResult.FromValue(completion2, mockPipelineResponse.Object);


            _mockChatClient.SetupSequence(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ReturnsAsync(completionResponse1)
                .ReturnsAsync(completionResponse2);

            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = ChatMessageRole.System.ToString(), Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = ChatMessageRole.User.ToString(), Content = "Hi, can you help me?" },
                new() { Role = ChatMessageRole.Assistant.ToString(), Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = ChatMessageRole.User.ToString(), Content = "What's the best way to train a parrot?" }
            };

            _mockTokenizerService.Setup(service => service.CountTokens(It.IsAny<string>()))
                .Returns(8001);

            // Act
            var result = await _openAIService.GetChatResponseAsync(chatMessages);

            // Assert
            Assert.Equal(ChatMessageRole.System.ToString(), result.Role);
            Assert.Equal("Get a professional trainer...", result.Content);
        }
    }
}
