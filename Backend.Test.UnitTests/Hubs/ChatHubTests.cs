﻿using Backend.Api.Hubs;
using Backend.Core;
using Backend.Core.Exceptions;
using Backend.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Backend.Test.UnitTests.Hubs
{
    public class ChatHubTests
    {
        [Fact]
        public async Task SendMessage_ShouldThrowBadRequest_WhenNoClaimsPrincipal()
        {
            // Arrange
            var contextMock = new Mock<HubCallerContext>();
            var openAiServiceMock = new Mock<IOpenAIService>();
            var cacheServiceMock = new Mock<ICacheService>();
            var chatSessionServiceMock = new Mock<IChatSessionService>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var chatHub = new ChatHub(openAiServiceMock.Object, cacheServiceMock.Object, chatSessionServiceMock.Object, promptOptionsMock.Object)
            {
                Context = contextMock.Object,
                Clients = Mock.Of<IHubCallerClients>(c =>
                    c.Caller == Mock.Of<IClientProxy>() &&
                    c.All == Mock.Of<IClientProxy>()
                )
            };
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => chatHub.SendMessage(""));
            // Assert
            Assert.Equal("User identity is not available.", exception.Message);
        }

        [Fact]
        public async Task SendMessage_ShouldThrowBadRequest_WhenMessageIsEmpty()
        {
            // Arrange
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-123") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.User).Returns(claimsPrincipal);
            contextMock.Setup(c => c.ConnectionId).Returns("conn-123");
            contextMock.Setup(c => c.UserIdentifier).Returns("user-123");

            var openAiServiceMock = new Mock<IOpenAIService>();
            var cacheServiceMock = new Mock<ICacheService>();
            var chatSessionServiceMock = new Mock<IChatSessionService>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var chatHub = new ChatHub(openAiServiceMock.Object, cacheServiceMock.Object, chatSessionServiceMock.Object, promptOptionsMock.Object)
            {
                Context = contextMock.Object,
                Clients = Mock.Of<IHubCallerClients>(c =>
                    c.Caller == Mock.Of<IClientProxy>() &&
                    c.All == Mock.Of<IClientProxy>()
                )
            };
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => chatHub.SendMessage(""));
            // Assert
            Assert.Equal("Message cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task SendMessage_ShouldStreamResponse()
        {
            var fullResponse = "AI response";
            // Arrange
            async IAsyncEnumerable<string> MockStream()
            {
                foreach (var ch in fullResponse)
                {
                    await Task.Delay(10);
                    yield return ch.ToString();
                }
            }
            var openAiServiceMock = new Mock<IOpenAIService>();
            openAiServiceMock
                .Setup(s => s.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(MockStream());
            var cacheServiceMock = new Mock<ICacheService>();
            var chatSessionServiceMock = new Mock<IChatSessionService>();

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-123") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.User).Returns(claimsPrincipal);
            contextMock.Setup(c => c.ConnectionId).Returns("conn-123");
            contextMock.Setup(c => c.UserIdentifier).Returns("user-123");

            var clientsMock = new Mock<IHubCallerClients>();
            var callerMock = new Mock<ISingleClientProxy>();

            clientsMock.Setup(c => c.Caller).Returns(callerMock.Object);
            clientsMock.Setup(c => c.All).Returns(callerMock.Object);

            callerMock
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sessionId = "sessionId";
            var message = "Hello, AI!";
            var session = new ChatSession
            {
                UserId = "user-123",
                Id = sessionId,
                Messages =
                [
                    new ChatMessage { Role = ChatRole.User, Content = message }
                ]
            };
            var sessions = new List<ChatSession> { session };
            cacheServiceMock
                .Setup(s => s.GetAsync<List<ChatSession>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            chatSessionServiceMock
                .Setup(s => s.GetAllSessionsForUserAsync("user-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);
            chatSessionServiceMock
                .Setup(s => s.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var chatHub = new ChatHub(openAiServiceMock.Object, cacheServiceMock.Object, chatSessionServiceMock.Object, promptOptionsMock.Object)
            {
                Context = contextMock.Object,
                Clients = clientsMock.Object
            };

            // Act
            await chatHub.SendMessage(message, sessionId);

            // Assert
            foreach (var chunk in fullResponse.Select(s => s.ToString()))
            {
                callerMock.Verify(c =>
                    c.SendCoreAsync(HubEventNames.ResponseStreamChunk,
                        It.Is<object[]>(args => args.Length == 1 && (string)args[0] == chunk),
                        It.IsAny<CancellationToken>()),
                    Times.AtLeastOnce);
            }

            callerMock.Verify(c =>
                c.SendCoreAsync(HubEventNames.SessionUpdate,
                    It.Is<object[]>(args =>
                        args.Length == 1 &&
                        args[0] is ChatSession &&
                        ((ChatSession)args[0]).Id == sessionId),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // Verify session is saved
            chatSessionServiceMock.Verify(s =>
                s.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendMessage_ShouldCreateNewSession_WhenNoSessionExists()
        {
            // Arrange
            var fullResponse = "AI response";
            async IAsyncEnumerable<string> MockStream()
            {
                foreach (var ch in fullResponse)
                {
                    await Task.Delay(10);
                    yield return ch.ToString();
                }
            }
            var openAiServiceMock = new Mock<IOpenAIService>();
            openAiServiceMock
                .Setup(s => s.GetChatResponseStreamingAsync(It.IsAny<IEnumerable<ChatMessage>>()))
                .Returns(MockStream());
            var cacheServiceMock = new Mock<ICacheService>();
            var chatSessionServiceMock = new Mock<IChatSessionService>();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-123") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.User).Returns(claimsPrincipal);
            contextMock.Setup(c => c.ConnectionId).Returns("conn-123");
            contextMock.Setup(c => c.UserIdentifier).Returns("user-123");
            var clientsMock = new Mock<IHubCallerClients>();
            var callerMock = new Mock<ISingleClientProxy>();
            clientsMock.Setup(c => c.Caller).Returns(callerMock.Object);
            clientsMock.Setup(c => c.All).Returns(callerMock.Object);
            callerMock
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            cacheServiceMock
                .Setup(s => s.GetAsync<List<ChatSession>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => default!);
            chatSessionServiceMock
                .Setup(s => s.GetAllSessionsForUserAsync("user-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionServiceMock
                .Setup(s => s.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            promptOptionsMock
                .Setup(p => p.Value)
                .Returns(new SystemPromptOptions
                {
                    SystemPrompts = ["System prompt 1", "System prompt 2", "System prompt 3", "System prompt 4", "System prompt 5"]
                });

            var chatHub = new ChatHub(openAiServiceMock.Object, cacheServiceMock.Object, chatSessionServiceMock.Object, promptOptionsMock.Object)
            {
                Context = contextMock.Object,
                Clients = clientsMock.Object
            };

            var message = "Hello, AI!";

            // Act
            await chatHub.SendMessage(message);

            // Assert
            cacheServiceMock.Verify(s =>
                s.SetAsync(It.Is<string>(key => key == "session-user-123"), It.Is<List<ChatSession>>(s => s.Count == 1), null, default),
                Times.Exactly(2));
            chatSessionServiceMock.Verify(s =>
                s.GetAllSessionsForUserAsync("user-123", It.IsAny<CancellationToken>()),
                Times.Once);
            chatSessionServiceMock.Verify(s =>
                s.SaveSessionAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetHistory_ThrowsBadRequest_WhenNoClaimsPrincipal()
        {
            // Arrange
            var contextMock = new Mock<HubCallerContext>();
            var openAiServiceMock = new Mock<IOpenAIService>();
            var cacheServiceMock = new Mock<ICacheService>();
            var chatSessionServiceMock = new Mock<IChatSessionService>();
            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var chatHub = new ChatHub(openAiServiceMock.Object, cacheServiceMock.Object, chatSessionServiceMock.Object, promptOptionsMock.Object)
            {
                Context = contextMock.Object,
                Clients = Mock.Of<IHubCallerClients>(c =>
                    c.Caller == Mock.Of<IClientProxy>() &&
                    c.All == Mock.Of<IClientProxy>()
                )
            };
            // Act
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => chatHub.GetHistory("sessionId", 0, 20));
            // Assert
            Assert.Equal("User identity is not available.", exception.Message);
        }

        [Fact]
        public async Task GetHistory_ShouldChunks()
        {
            // Arrange
            var openAiServiceMock = new Mock<IOpenAIService>();
            var cacheServiceMock = new Mock<ICacheService>();
            var chatSessionServiceMock = new Mock<IChatSessionService>();

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-123") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.User).Returns(claimsPrincipal);
            contextMock.Setup(c => c.ConnectionId).Returns("conn-123");
            contextMock.Setup(c => c.UserIdentifier).Returns("user-123");

            var clientsMock = new Mock<IHubCallerClients>();
            var callerMock = new Mock<ISingleClientProxy>();

            clientsMock.Setup(c => c.Caller).Returns(callerMock.Object);
            clientsMock.Setup(c => c.All).Returns(callerMock.Object);

            callerMock
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var session = new ChatSession
            {
                UserId = "user-123",
                Id = "sessionId",
                Messages =
                [
                    new ChatMessage { Role = ChatRole.User, Content = "Hello!" },
                    new ChatMessage { Role = ChatRole.Assistant, Content = "Hi there!" },
                ]
            };
            var sessions = new List<ChatSession> { session };
            cacheServiceMock
                .Setup(s => s.GetAsync<List<ChatSession>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            chatSessionServiceMock
                .Setup(s => s.GetAllSessionsForUserAsync("user-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);

            var promptOptionsMock = new Mock<IOptions<SystemPromptOptions>>();
            var chatHub = new ChatHub(openAiServiceMock.Object, cacheServiceMock.Object, chatSessionServiceMock.Object, promptOptionsMock.Object)
            {
                Context = contextMock.Object,
                Clients = clientsMock.Object
            };

            // Act
            await chatHub.GetHistory("sessionId", 0, 20);

            // Assert
            callerMock.Verify(c =>
                c.SendCoreAsync(
                    HubEventNames.HistoryStreamChunk,
                    It.Is<object[]>(args => args.Length == 1 && args[0] is ChatMessage),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            chatSessionServiceMock.Verify(s =>
                s.GetAllSessionsForUserAsync("user-123", It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
