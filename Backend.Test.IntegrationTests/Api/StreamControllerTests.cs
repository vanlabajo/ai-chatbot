using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Backend.Test.IntegrationTests.Api
{
    public class StreamControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Chat_ReturnsBadRequest_WhenNotWebSocketRequest()
        {
            // Act
            var response = await _client.GetAsync("/stream/chat");
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Chat_ReturnsOk_WhenWebSocketRequest()
        {
            // Arrange
            var client = factory.Server.CreateWebSocketClient();
            using var webSocket = await client.ConnectAsync(new Uri("ws://localhost/stream/chat"), CancellationToken.None);
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
