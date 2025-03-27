using Backend.Api.Controllers;
using Backend.Core;
using Backend.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Backend.Test.UnitTests.Api
{
    public class ChatControllerTests
    {
        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenMessageIsNullOrWhitespace()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            var controller = new ChatController(openAiService.Object);
            var request = new ChatRequest { Message = null };
            // Act
            var result = await controller.Chat(request);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenMessageIsNotNullOrWhitespace()
        {
            // Arrange
            var openAiService = new Mock<IOpenAIService>();
            openAiService.Setup(x => x.GetChatResponseAsync(It.IsAny<string>())).ReturnsAsync("Hello, World!");
            var controller = new ChatController(openAiService.Object);
            var request = new ChatRequest { Message = "Hello" };
            // Act
            var result = await controller.Chat(request);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal("Hello, World!", response.Response);
        }
    }
}
