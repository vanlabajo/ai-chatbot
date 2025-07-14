using Backend.Api.Controllers;
using Backend.Core;
using Backend.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Backend.Test.UnitTests.Api
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task GetSessions_ReturnsOkResult_WhenSessionsExist()
        {
            // Arrange
            var mockChatSessionService = new Mock<IChatSessionService>();
            mockChatSessionService.Setup(service => service.GetAllSessionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([new ChatSession { Id = "1", UserId = "test-123", Title = "Test Session" }]);

            var controller = new AdminController(mockChatSessionService.Object);

            // Act
            var result = await controller.GetSessions(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var sessions = Assert.IsType<IEnumerable<ChatSession>>(okResult.Value, exactMatch: false);
            Assert.Single(sessions);
        }
    }
}
