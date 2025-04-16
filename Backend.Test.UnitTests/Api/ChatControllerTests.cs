using Backend.Api.Controllers;
using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Backend.Test.UnitTests.Api
{
    public class ChatControllerTests
    {
        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenMessageIsNullOrWhitespace()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var controller = new ChatController(openAiService.Object, cacheService.Object);
            var request = new ChatRequest { Message = null };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenMessageIsNotNullOrWhitespace()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                    new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatMessage>>("conversations-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);
        }

        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenUserIdentityIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenConversationsIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                    new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatMessage>>("conversations-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenConversationsIsEmpty()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            new List<Claim>
            {
                    new Claim(ClaimTypes.NameIdentifier, "testuser"),
            }, "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatMessage>>("conversations-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChatMessage>());
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);
        }

        [Fact]
        public async Task Chat_ServiceResponseIsAddedToCache()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };

            cacheService.Setup(x => x.GetAsync<List<ChatMessage>>("conversations-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            List<ChatMessage>? cachedMessages = null;
            cacheService.Setup(x => x.SetAsync("conversations-testuser", It.IsAny<List<ChatMessage>>(), null, It.IsAny<CancellationToken>()))
                .Callback<string, List<ChatMessage>, TimeSpan?, CancellationToken>((key, messages, expiration, token) =>
                {
                    cachedMessages = messages;
                });

            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);

            Assert.NotNull(cachedMessages);
            Assert.Equal(2, cachedMessages.Count);
            Assert.Equal("Hello", cachedMessages[0].Content);
            Assert.Equal("Hello, World!", cachedMessages[1].Content);
        }

        [Fact]
        public async Task StreamChat_ReturnsOk_WhenMessageIsNotNullOrWhitespace()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();

            async IAsyncEnumerable<string> GetTestValues()
            {
                yield return "Hello,";
                yield return " World!";

                await Task.CompletedTask;
            }


            openAiService.Setup(x => x.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(GetTestValues);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                    new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = controller.StreamChat(request, default);
            // Assert
            await foreach (var item in result)
            {
                Assert.NotNull(item);
                Assert.NotEmpty(item);
            }
        }

        [Fact]
        public async Task StreamChat_ReturnsBadRequest_WhenMessageIsNullOrWhitespace()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var controller = new ChatController(openAiService.Object, cacheService.Object);
            var request = new ChatRequest { Message = null };
            // Act
            var result = controller.StreamChat(request, default);
            // Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await foreach (var item in result)
                {   
                    // This should not be reached
                }
            });
        }

        [Fact]
        public async Task StreamChat_ReturnsBadRequest_WhenUserIdentityIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var controller = new ChatController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = controller.StreamChat(request, default);
            // Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await foreach (var item in result)
                {
                    // This should not be reached
                }
            });
        }
    }
}
