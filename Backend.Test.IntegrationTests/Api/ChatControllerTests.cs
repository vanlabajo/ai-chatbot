using Backend.Core.DTOs;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
            try
            {
                // Act
                var response = await _client.PostAsync("/api/chat", content);
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<ChatResponse>(responseString, _jsonOptions);
                Assert.NotEqual(string.Empty, responseObj!.Response);
            }
            catch (TaskCanceledException)
            {
                // Skip the test if the OpenAI service is not available
            }
        }

        [Fact]
        public async Task Chat_CorsPolicy_AllowsConfiguredOrigin()
        {
            // Arrange
            var client = factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowSpecificOrigins", policy =>
                            {
                                policy
                                    .WithOrigins("http://localhost:12345")
                                    .AllowAnyMethod()
                                    .AllowAnyHeader()
                                    .AllowCredentials();
                            });
                        });
                    });
                })
                .CreateClient();
            client.DefaultRequestHeaders.Add("Origin", "http://localhost:12345");
            // Act
            var response = await client.GetAsync("/api/chat/sessions");
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.Equal("http://localhost:12345", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        }

        [Fact]
        public async Task Chat_CorsPolicy_DoesNotAllowAnyOrigin_WhenAllowedOriginsIsEmpty()
        {
            // Arrange
            var client = factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowSpecificOrigins", policy =>
                            {
                                policy
                                    .WithOrigins([])
                                    .AllowAnyMethod()
                                    .AllowAnyHeader()
                                    .AllowCredentials();
                            });
                        });
                    });
                })
                .CreateClient();
            client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
            // Act
            var response = await client.GetAsync("/api/chat/sessions");
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }
}
