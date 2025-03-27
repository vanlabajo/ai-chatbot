using Backend.Core.DTOs;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Backend.Test.IntegrationTests.Api
{
    public class ChatControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenMessageIsNullOrWhitespace()
        {
            // Arrange
            var request = new ChatRequest { Message = null };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            // Act
            var response = await _client.PostAsync("/api/chat", content);
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenMessageIsNotNullOrWhitespace()
        {
            // Arrange
            var request = new ChatRequest { Message = "Hello" };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            // Act
            var response = await _client.PostAsync("/api/chat", content);
            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ChatResponse>(responseString, _jsonOptions);
            Assert.NotEqual(string.Empty, responseObj!.Response);
        }
    }
}
