using Backend.Api.Controllers;
using Backend.Api.Hubs;
using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
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
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object);
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
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();

            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            promptOptionsMock
                .Setup(p => p.Value)
                .Returns(new SystemPromptOptions
                {
                    SystemPrompts = ["System prompt 1", "System prompt 2"]
                });

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
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
            chatSessionService.Verify(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenUserIdentityIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
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
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();

            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            promptOptionsMock
                .Setup(p => p.Value)
                .Returns(new SystemPromptOptions
                {
                    SystemPrompts = ["System prompt 1", "System prompt 2"]
                });

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
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
            chatSessionService.Verify(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenSessionIsEmpty()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();

            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            promptOptionsMock
                .Setup(p => p.Value)
                .Returns(new SystemPromptOptions
                {
                    SystemPrompts = ["System prompt 1", "System prompt 2"]
                });

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
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request, default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
            chatSessionService.Verify(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Chat_ServiceResponseIsAddedToCache()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();

            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");

            static async IAsyncEnumerable<string> GetTestValues()
            {
                yield return "This is the";
                yield return " Test Title!";
                await Task.CompletedTask;
            }

            openAiService.Setup(x => x.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(GetTestValues);
            promptOptionsMock
                .Setup(p => p.Value)
                .Returns(new SystemPromptOptions
                {
                    SystemPrompts = ["System prompt 1", "System prompt 2", "System prompt 3", "System prompt 4", "System prompt 5"]
                });

            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };

            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
            Assert.Equal("This is the Test Title!", cachedSessions[0].Title);
            Assert.Equal("Hello", cachedSessions[0].Messages[5].Content);
            Assert.Equal("Hello, World!", cachedSessions[0].Messages[6].Content);

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
            chatSessionService.Verify(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Chat_AddMessageToSession_WhenSessionExists()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
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
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var existingSession = new ChatSession
            {
                UserId = "user-123",
                Id = "001",
                Messages =
                [
                    new() { Role = ChatRole.User, Content = "Hello" },
                    new() { Role = ChatRole.Assistant, Content = "Hi there!" }
                ]
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([existingSession]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
            chatSessionService.Verify(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Chat_NewSession_WhenSessionDoesNotExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            static async IAsyncEnumerable<string> GetTestValues()
            {
                yield return "Hello,";
                yield return " World!";
                await Task.CompletedTask;
            }
            openAiService.Setup(x => x.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(GetTestValues);
            promptOptionsMock
                .Setup(p => p.Value)
                .Returns(new SystemPromptOptions
                {
                    SystemPrompts = ["System prompt 1", "System prompt 2"]
                });
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
            chatSessionService.Verify(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSessions_ReturnsOk_WhenSessionsExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var existingSession = new ChatSession
            {
                UserId = "user-123",
                Id = "001",
                Messages =
                [
                    new() { Role = ChatRole.User, Content = "Hello" },
                    new() { Role = ChatRole.Assistant, Content = "Hi there!" }
                ]
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([existingSession]);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([existingSession]);
            // Act
            var result = await controller.GetSessions(default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var sessions = Assert.IsType<List<ChatSession>>(okResult.Value);
            Assert.Single(sessions);

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetSessions_ReturnsOk_WhenNoSessionsExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            // Act
            var result = await controller.GetSessions(default);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var sessions = Assert.IsType<List<ChatSession>>(okResult.Value);
            Assert.Empty(sessions);

            chatSessionService.Verify(x => x.GetAllSessionsForUserAsync("testuser", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSessions_ReturnsBadRequest_WhenUserIdentityIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            // Act
            var result = await controller.GetSessions(default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSession_ReturnsNoContent_WhenSessionIsDeleted()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var clientProxy = new Mock<IClientProxy>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var sessionId = "001";
            var existingSession = new ChatSession
            {
                UserId = "testuser",
                Id = sessionId,
                Messages =
                [
                    new() { Role = ChatRole.User, Content = "Hello" },
                    new() { Role = ChatRole.Assistant, Content = "Hi there!" }
                ]
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([existingSession]);
            chatSessionService.Setup(x => x.DeleteSessionAsync("testuser", sessionId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            hubContext.Setup(x => x.Clients.All).Returns(clientProxy.Object);
            clientProxy
                .Setup(x => x.SendCoreAsync(
                    HubEventNames.SessionDelete,
                    It.Is<object[]>(o => o.Length == 1 && (string)o[0] == sessionId),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await controller.DeleteSession(sessionId, default);
            // Assert
            Assert.IsType<NoContentResult>(result);
            chatSessionService.Verify(x => x.DeleteSessionAsync("testuser", sessionId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSession_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var sessionId = "001";
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.DeleteSessionAsync("testuser", sessionId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("Session not found"));
            // Act
            var result = await controller.DeleteSession(sessionId, default);
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSession_ReturnsBadRequest_WhenUserIdentityIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            // Act
            var result = await controller.DeleteSession("001", default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionTitle_ReturnsOk_WhenTitleIsUpdated()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var clientProxy = new Mock<IClientProxy>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var sessionId = "001";
            var existingSession = new ChatSession
            {
                UserId = "testuser",
                Id = sessionId,
                Messages =
                [
                    new() { Role = ChatRole.User, Content = "Hello" },
                    new() { Role = ChatRole.Assistant, Content = "Hi there!" }
                ]
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([existingSession]);
            chatSessionService.Setup(x => x.SaveSessionAsync(existingSession, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            hubContext.Setup(x => x.Clients.All).Returns(clientProxy.Object);
            clientProxy
                .Setup(x => x.SendCoreAsync(
                    HubEventNames.SessionUpdate,
                    It.Is<object[]>(o => o.Length == 1 && ((ChatSession)o[0]).Id == sessionId),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await controller.UpdateSessionTitle(sessionId, "New Title", default);
            // Assert
            Assert.IsType<NoContentResult>(result);
            chatSessionService.Verify(
                x => x.SaveSessionAsync(
                    It.Is<ChatSession>(s => s.Title == "New Title"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSessionTitle_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            var sessionId = "001";
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionService.Setup(x => x.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("Session not found"));
            // Act
            var result = await controller.UpdateSessionTitle(sessionId, "New Title", default);
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionTitle_ReturnsBadRequest_WhenUserIdentityIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            // Act
            var result = await controller.UpdateSessionTitle("001", "New Title", default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionTitle_ReturnsBadRequest_WhenTitleIsEmpty()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            // Act
            var result = await controller.UpdateSessionTitle("001", "", default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionTitle_ReturnsBadRequest_WhenTitleIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            // Act
            var result = await controller.UpdateSessionTitle("001", null!, default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionTitle_ReturnsBadRequest_WhenSessionIdIsNull()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var chatSessionService = new Mock<IChatSessionService>();
            var hubContext = new Mock<IHubContext<ChatHub>>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var controller = new ChatController(openAiService.Object, cacheService.Object, chatSessionService.Object, hubContext.Object, promptOptionsMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            // Act
            var result = await controller.UpdateSessionTitle(null!, "New Title", default);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
