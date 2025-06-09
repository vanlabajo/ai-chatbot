using Backend.Core.Models;
using Backend.Core.Repositories;
using Backend.Infrastructure;
using Moq;

namespace Backend.Test.UnitTests.Infrastructure.Services
{
    public class ChatSessionServiceTests
    {
        [Fact]
        public async Task SaveSessionAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepository = new Mock<IChatSessionRepository>();
            var service = new ChatSessionService(mockRepository.Object);
            var session = new ChatSession { UserId = "user1", Id = "session1", Messages = [] };

            // Act
            await service.SaveSessionAsync(session);

            // Assert
            mockRepository.Verify(repo => repo.SaveSessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSessionByIdAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepository = new Mock<IChatSessionRepository>();
            var service = new ChatSessionService(mockRepository.Object);
            var userId = "user1";
            var sessionId = "session1";
            var expectedSession = new ChatSession { UserId = userId, Id = sessionId, Messages = [] };
            mockRepository.Setup(repo => repo.GetSessionAsync(userId, sessionId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(expectedSession);
            // Act
            var result = await service.GetSessionByIdAsync(userId, sessionId);
            // Assert
            Assert.Equal(expectedSession, result);
            mockRepository.Verify(repo => repo.GetSessionAsync(userId, sessionId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllSessionsForUserAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepository = new Mock<IChatSessionRepository>();
            var service = new ChatSessionService(mockRepository.Object);
            var userId = "user1";
            var expectedSessions = new List<ChatSession>
            {
                new() { UserId = userId, Id = "session1", Messages = [] },
                new() { UserId = userId, Id = "session2", Messages = [] }
            };
            mockRepository.Setup(repo => repo.GetAllSessionsForUserAsync(userId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(expectedSessions);
            // Act
            var result = await service.GetAllSessionsForUserAsync(userId);
            // Assert
            Assert.Equal(expectedSessions, result);
            mockRepository.Verify(repo => repo.GetAllSessionsForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_ShouldCallRepository()
        {
            // Arrange
            var mockRepository = new Mock<IChatSessionRepository>();
            var service = new ChatSessionService(mockRepository.Object);
            var userId = "user1";
            var sessionId = "session1";
            // Act
            await service.DeleteSessionAsync(userId, sessionId);
            // Assert
            mockRepository.Verify(repo => repo.DeleteSessionAsync(userId, sessionId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
