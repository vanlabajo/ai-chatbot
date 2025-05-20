using Backend.Api.Controllers;
using Backend.Core;
using Backend.Core.DTOs;
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
            
            static async IAsyncEnumerable<string> GetTestValues()
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
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
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
        public async Task Chat_ReturnsOk_WhenSessionIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();

            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");

            static async IAsyncEnumerable<string> GetTestValues()
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
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
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
        public async Task Chat_ReturnsOk_WhenSessionIsEmpty()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();

            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");

            static async IAsyncEnumerable<string> GetTestValues()
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
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
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

            static async IAsyncEnumerable<string> GetTestValues()
            {
                yield return "This is the";
                yield return " Test Subject!";

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

            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            List<ChatSession>? cachedSessions = null;
            cacheService.Setup(x => x.SetAsync("session-testuser", It.IsAny<List<ChatSession>>(), null, It.IsAny<CancellationToken>()))
                .Callback<string, List<ChatSession>, TimeSpan?, CancellationToken>((key, sessions, expiration, token) =>
                {
                    cachedSessions = sessions;
                });

            var request = new ChatRequest { SessionId = "001", Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);

            Assert.NotNull(cachedSessions);
            Assert.Single(cachedSessions);
            Assert.Equal(7, cachedSessions[0].Messages.Count);
            Assert.Equal("001", cachedSessions[0].Id);
            Assert.Equal("This is the Test Subject!", cachedSessions[0].Subject);
            Assert.Equal("Hello", cachedSessions[0].Messages[5].Content);
            Assert.Equal("Hello, World!", cachedSessions[0].Messages[6].Content);
        }

        [Fact]
        public async Task Chat_AddMessageToSession_WhenSessionExists()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            static async IAsyncEnumerable<string> GetTestValues()
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
            var existingSession = new ChatSession
            {
                Id = "001",
                Messages =
                [
                    new() { Role = ChatRole.User, Content = "Hello" },
                    new() { Role = ChatRole.Assistant, Content = "Hi there!" }
                ]
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([existingSession]);
            List<ChatSession>? cachedSessions = null;
            cacheService.Setup(x => x.SetAsync("session-testuser", It.IsAny<List<ChatSession>>(), null, It.IsAny<CancellationToken>()))
                .Callback<string, List<ChatSession>, TimeSpan?, CancellationToken>((key, sessions, expiration, token) =>
                {
                    cachedSessions = sessions;
                });
            var request = new ChatRequest { SessionId = "001", Message = "How are you?" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);
            Assert.NotNull(cachedSessions);
            Assert.Single(cachedSessions);
            Assert.Equal(4, cachedSessions[0].Messages.Count);
        }

        [Fact]
        public async Task Chat_NewSession_WhenSessionDoesNotExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            static async IAsyncEnumerable<string> GetTestValues()
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
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            List<ChatSession>? cachedSessions = null;
            cacheService.Setup(x => x.SetAsync("session-testuser", It.IsAny<List<ChatSession>>(), null, It.IsAny<CancellationToken>()))
                .Callback<string, List<ChatSession>, TimeSpan?, CancellationToken>((key, sessions, expiration, token) =>
                {
                    cachedSessions = sessions;
                });
            var request = new ChatRequest { SessionId = "001", Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);
            Assert.NotNull(cachedSessions);
            Assert.Single(cachedSessions);
        }
    }
}
