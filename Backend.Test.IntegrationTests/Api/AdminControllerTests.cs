using Backend.Core.Models;
using System.Net;
using System.Text.Json;

namespace Backend.Test.IntegrationTests.Api
{
    public class AdminControllerTests(AdminWebApplicationFactory factory) : IClassFixture<AdminWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public async Task GetSessions_ReturnsOkResult_WhenSessionsExist()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/sessions");
            // Act
            var response = await _client.SendAsync(request);
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var sessions = JsonSerializer.Deserialize<IEnumerable<ChatSession>>(responseString, _jsonOptions);
            Assert.NotNull(sessions);
            Assert.NotEmpty(sessions);
        }
    }
}
