using Backend.Api.Controllers;
using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

namespace Backend.Test.UnitTests.Api
{
    public class StreamControllerTests
    {
        [Fact]
        public async Task Chat_ThrowsBadRequest_WhenUserNotAuthenticated()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var context = new DefaultHttpContext();
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(true);
            webSocketFeature.Setup(f => f.AcceptAsync(It.IsAny<WebSocketAcceptContext>()))
                .ReturnsAsync(new Mock<WebSocket>().Object);
            context.Features.Set(webSocketFeature.Object);
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            var request = new ChatRequest { Message = null };
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(async () => await controller.Chat(null, default));
            // Assert
            Assert.Equal("User identity is not available.", exception.Message);
        }

        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenNotWebSocketRequest()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var context = new DefaultHttpContext();
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(false);
            webSocketFeature.Setup(f => f.AcceptAsync(It.IsAny<WebSocketAcceptContext>()))
                .ReturnsAsync(new Mock<WebSocket>().Object);
            context.Features.Set(webSocketFeature.Object);
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            var request = new ChatRequest { Message = null };
            // Act
            await controller.Chat(null, default);
            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task Chat_AddMessageToSession_WhenSessionExists()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");

            static async IAsyncEnumerable<string> GetTestValues()
            {
                yield return "This is the";
                yield return " Test Subject!";

                await Task.CompletedTask;
            }

            openAiService.Setup(x => x.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(GetTestValues);

            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            var webSocket = new Mock<WebSocket>();
            var messageBytes = Encoding.UTF8.GetBytes("Hello, WebSocket!");
            var receiveCallCount = 0;

            webSocket.Setup(f => f.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .Callback<ArraySegment<byte>, CancellationToken>((buffer, token) =>
                {
                    if (receiveCallCount == 0)
                    {
                        // Copy the message into the buffer on the first call
                        Array.Copy(messageBytes, buffer.Array!, messageBytes.Length);
                    }
                })
                .ReturnsAsync(() =>
                {
                    if (receiveCallCount == 0)
                    {
                        receiveCallCount++;
                        return new WebSocketReceiveResult(messageBytes.Length, WebSocketMessageType.Text, true);
                    }
                    else
                    {
                        // Simulate WebSocket closure on the second call
                        return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "Closing");
                    }
                });
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(true);
            webSocketFeature.Setup(f => f.AcceptAsync(It.IsAny<WebSocketAcceptContext>()))
                .ReturnsAsync(webSocket.Object);
            context.Features.Set(webSocketFeature.Object);
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var sessionId = "session1";
            var session = new ChatSession { SessionId = sessionId, Messages = [] };
            var sessions = new List<ChatSession> { session };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>($"session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            // Act
            await controller.Chat(sessionId, default);
            // Assert
            Assert.Contains(session, sessions);
        }

        [Fact]
        public async Task Chat_ThrowsBadRequest_WhenMessageIsEmpty()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            var webSocket = new Mock<WebSocket>();

            webSocket.Setup(f => f.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebSocketReceiveResult(0, WebSocketMessageType.Text, true));
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(true);
            webSocketFeature.Setup(f => f.AcceptAsync(It.IsAny<WebSocketAcceptContext>()))
                .ReturnsAsync(webSocket.Object);
            context.Features.Set(webSocketFeature.Object);
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var sessionId = "session1";
            var session = new ChatSession { SessionId = sessionId, Messages = [] };
            var sessions = new List<ChatSession> { session };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>($"session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(async () => await controller.Chat(null, default));
            // Assert
            Assert.Equal("Message cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task Chat_NewSession_WhenSessionDoesNotExist()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<IEnumerable<ChatMessage>>())).ReturnsAsync("Hello, World!");
            static async IAsyncEnumerable<string> GetTestValues()
            {
                yield return "This is the";
                yield return " Test Subject!";
                await Task.CompletedTask;
            }
            openAiService.Setup(x => x.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(GetTestValues);
            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            var webSocket = new Mock<WebSocket>();
            var messageBytes = Encoding.UTF8.GetBytes("Hello, WebSocket!");
            var receiveCallCount = 0;
            webSocket.Setup(f => f.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .Callback<ArraySegment<byte>, CancellationToken>((buffer, token) =>
                {
                    if (receiveCallCount == 0)
                    {
                        // Copy the message into the buffer on the first call
                        Array.Copy(messageBytes, buffer.Array!, messageBytes.Length);
                    }
                })
                .ReturnsAsync(() =>
                {
                    if (receiveCallCount == 0)
                    {
                        receiveCallCount++;
                        return new WebSocketReceiveResult(messageBytes.Length, WebSocketMessageType.Text, true);
                    }
                    else
                    {
                        // Simulate WebSocket closure on the second call
                        return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "Closing");
                    }
                });
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(true);
            webSocketFeature.Setup(f => f.AcceptAsync(It.IsAny<WebSocketAcceptContext>()))
                .ReturnsAsync(webSocket.Object);
            context.Features.Set(webSocketFeature.Object);
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var sessionId = "session1";
            var initialMessages = new List<ChatMessage>
            {
                new() { Role = ChatRole.System, Content = "You are Rick from the TV show Rick & Morty. Pretend to be Rick." },
                new() { Role = ChatRole.User, Content = "Introduce yourself." }
            };
            var sessions = new List<ChatSession>();
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>($"session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            // Act
            await controller.Chat(sessionId, default);
            // Assert
            var sessionMessages = sessions.SelectMany(session => session.Messages).ToList();
            Assert.All(initialMessages, initialMessage =>
            {
                Assert.Contains(sessionMessages, sessionMessage =>
                    sessionMessage.Role == initialMessage.Role && sessionMessage.Content == initialMessage.Content);
            });
        }

        [Fact]
        public async Task GetSessions_ThrowsBadRequest_WhenUserNotAuthenticated()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var context = new DefaultHttpContext();
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(async () => {
                await controller.GetSessions(default);
            });
            // Assert
            Assert.Equal("User identity is not available.", exception.Message);
        }

        [Fact]
        public async Task GetSessions_ReturnsSessions_WhenUserAuthenticated()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            // Use a MemoryStream to capture the response
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            var sessions = new List<ChatSession>
            {
                new() { SessionId = "session1", Subject = "Test Subject", Messages = [] },
                new() { SessionId = "session2", Subject = "Test Subject", Messages = [] }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);

            var result = new List<ChatSession>();

            // Act
            try
            {
                var cts = new CancellationTokenSource();
                // Cancel after the first iteration to avoid infinite loop
                cts.CancelAfter(100);

                // Act
                await controller.GetSessions(cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected exception due to cancellation
            }

            // Assert
            memoryStream.Position = 0;
            var output = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Contains("session1", output);
            Assert.Contains("Test Subject", output);
            Assert.Contains("data:", output);
        }

        [Fact]
        public async Task GetSessions_ReturnsEmpty_WhenNoSessions()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            // Use a MemoryStream to capture the response
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            // Act
            var result = new List<ChatSession>();

            try
            {
                var cts = new CancellationTokenSource();
                // Cancel after the first iteration to avoid infinite loop
                cts.CancelAfter(100);

                // Act
                await controller.GetSessions(cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected exception due to cancellation
            }
            // Assert
            memoryStream.Position = 0;
            var output = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.DoesNotContain("session1", output);
            Assert.DoesNotContain("Test Subject", output);
            Assert.DoesNotContain("data:", output);
        }

        [Fact]
        public async Task GetSessionMessages_ThrowsBadRequest_WhenUserNotAuthenticated()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var context = new DefaultHttpContext();
            // Use a MemoryStream to capture the response
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };

            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(async () => {
                await controller.GetSessionMessages("session1", default);
            });
            // Assert
            Assert.Equal("User identity is not available.", exception.Message);
        }

        [Fact]
        public async Task GetSessionMessages_ReturnsMessages_WhenUserAuthenticated()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            // Use a MemoryStream to capture the response
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            var sessionId = "session1";
            var messages = new List<ChatMessage>
            {
                new() { Role = ChatRole.User, Content = "Hello!" },
                new() { Role = ChatRole.Assistant, Content = "Hi there!" }
            };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var session = new ChatSession { SessionId = sessionId, Messages = messages };
            var sessions = new List<ChatSession> { session };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>($"session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            
            var result = new List<ChatMessage>();

            // Act
            try
            {
                var cts = new CancellationTokenSource();
                // Cancel after the first iteration to avoid infinite loop
                cts.CancelAfter(100);

                // Act
                await controller.GetSessionMessages(sessionId, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected exception due to cancellation
            }

            // Assert
            memoryStream.Position = 0;
            var output = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Contains("Hello!", output);
            Assert.Contains("Hi there!", output);
            Assert.Contains("data:", output);
        }

        [Fact]
        public async Task GetSessionMessages_ReturnsEmpty_WhenNoMessages()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var cacheService = new Mock<ICacheService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
            ], "mock"));
            var context = new DefaultHttpContext() { User = user };
            // Use a MemoryStream to capture the response
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            var controller = new StreamController(openAiService.Object, cacheService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
            var sessionId = "session1";
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>("session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            var session = new ChatSession { SessionId = sessionId, Messages = [] };
            var sessions = new List<ChatSession> { session };
            cacheService.Setup(x => x.GetAsync<List<ChatSession>>($"session-testuser", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            // Act
            try
            {
                var cts = new CancellationTokenSource();
                // Cancel after the first iteration to avoid infinite loop
                cts.CancelAfter(100);

                // Act
                await controller.GetSessionMessages(sessionId, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected exception due to cancellation
            }

            // Assert
            memoryStream.Position = 0;
            var output = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.DoesNotContain("session1", output);
            Assert.DoesNotContain("Test Subject", output);
            Assert.DoesNotContain("data:", output);
        }
    }
}
