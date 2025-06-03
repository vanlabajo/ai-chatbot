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
            var expectedResponse = "Hello, how can I help you?";
            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.System,
                content: new ChatMessageContent(expectedResponse));

            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completion, mockPipelineResponse.Object);

            _mockChatClient.Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .ReturnsAsync(completionResponse);

            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
            };

            // Act
            var result = await _openAIService.GetChatResponseAsync(chatMessages);

            // Assert
            Assert.Equal(expectedResponse, result);
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
            };
            // Act
            var result = await _openAIService.GetChatResponseAsync(chatMessages);
            // Assert
            Assert.Equal(string.Empty, result);
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
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
            };

            _mockTokenizerService.Setup(service => service.CountTokens(It.IsAny<string>()))
                .Returns(8001);

            // Act
            var result = await _openAIService.GetChatResponseAsync(chatMessages);

            // Assert
            Assert.Equal("Get a professional trainer...", result);
        }

        [Fact]
        public async Task GetChatResponseStreamingAsync_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "Hello, how can I help you?";

            _mockChatClient.Setup(client => client.CompleteChatStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .Returns(new AsyncStreamingChatCompletionUpdateCollection([expectedResponse]));
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
            };
            // Act
            var result = _openAIService.GetChatResponseStreamingAsync(chatMessages);
            // Assert
            await foreach (var message in result)
            {
                Assert.Equal(expectedResponse, message);
            }
        }

        [Fact]
        public async Task GetChatResponseStreamingAsync_ThrowsNotFoundException_WhenNoCompletionUpdate()
        {
            // Arrange
            _mockChatClient.Setup(client => client.CompleteChatStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .Returns(new AsyncStreamingChatCompletionUpdateCollection([]));
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
            };
            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                // Foreach here to force the async streaming to execute
                await foreach (var _ in _openAIService.GetChatResponseStreamingAsync(chatMessages)) ;
            });
            // Assert
            Assert.Equal("Chat response not found.", exception.Message);
        }

        [Fact]
        public async Task GetChatResponseStreamingAsync_ThrowsNotFoundException_SkipsWhenContentUpdateIsEmpty()
        {
            // Arrange
            var chatMessages = new List<Backend.Core.Models.ChatMessage>
            {
                new() { Role = Backend.Core.Models.ChatRole.System, Content = "You are a helpful assistant that talks like a pirate." },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "Hi, can you help me?" },
                new() { Role = Backend.Core.Models.ChatRole.Assistant, Content = "Arrr! Of course, me hearty! What can I do for ye?" },
                new() { Role = Backend.Core.Models.ChatRole.User, Content = "What's the best way to train a parrot?" }
            };

            var completionUpdate = OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                role: ChatMessageRole.System,
                contentUpdate: new ChatMessageContent([])
            );

            var mockPipelineResponse = new Mock<PipelineResponse>();
            var completionResponse = ClientResult.FromValue(completionUpdate, mockPipelineResponse.Object);

            _mockChatClient.Setup(client => client.CompleteChatStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>(), null, default))
                .Returns(new AsyncStreamingChatCompletionUpdateCollection([""]));

            var messages = new List<string>();
            // Act & Assert
            var result = _openAIService.GetChatResponseStreamingAsync(chatMessages);
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                // Foreach here to force the async streaming to execute
                await foreach (var message in _openAIService.GetChatResponseStreamingAsync(chatMessages))
                {
                    messages.Add(message);
                }
            });

            Assert.Empty(messages);
        }

        private class AsyncStreamingChatCompletionUpdateCollection(List<string> responses) : AsyncCollectionResult<StreamingChatCompletionUpdate>
        {
            private readonly IEnumerable<string> _responses = responses;

            public override ContinuationToken? GetContinuationToken(ClientResult page)
            {
                throw new NotImplementedException();
            }

            public override async IAsyncEnumerable<ClientResult> GetRawPagesAsync()
            {
                var mockPipelineResponse = new Mock<PipelineResponse>();

                if (!_responses.Any())
                {
                    yield return ClientResult.FromOptionalValue<StreamingChatCompletionUpdate?>(null, mockPipelineResponse.Object);
                }

                foreach (var response in _responses)
                {
                    var completionUpdate = OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                            role: ChatMessageRole.System,
                            contentUpdate: new ChatMessageContent(response));

                    if (response.Equals(""))
                        completionUpdate = OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                            role: ChatMessageRole.System,
                            contentUpdate: new ChatMessageContent([]));

                    yield return await Task.FromResult(ClientResult.FromValue(completionUpdate, mockPipelineResponse.Object));
                }
            }

            protected override async IAsyncEnumerable<StreamingChatCompletionUpdate> GetValuesFromPageAsync(ClientResult page)
            {
                var streamingUpdate = ((ClientResult<StreamingChatCompletionUpdate>)page).Value;
                await Task.Delay(2 * 1000);
                yield return streamingUpdate;
            }
        }
    }
}
