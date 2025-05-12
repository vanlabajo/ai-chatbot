using Backend.Core.DTOs;
using System.Net;
using System.Net.WebSockets;
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

        [Fact]
        public async Task HandleWebSocket_ReturnsBadRequest_WhenNotWebSocketRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/chat/ws");
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task HandleWebSocket_ReturnsOk_WhenWebSocketRequest()
        {
            // Arrange
            var client = factory.Server.CreateWebSocketClient();
            using var webSocket = await client.ConnectAsync(new Uri("ws://localhost/api/chat/ws"), CancellationToken.None);
            // Act
            var message = Encoding.UTF8.GetBytes("Hello");
            await webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
            var buffer = new byte[1024];
            var accumulatedMessage = new StringBuilder();
            while (!webSocket.CloseStatus.HasValue)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                accumulatedMessage.AppendLine(response);
                if (response.Equals("[END]"))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            // Assert
            Assert.False(string.IsNullOrEmpty(accumulatedMessage.ToString()));
        }
    }
}
