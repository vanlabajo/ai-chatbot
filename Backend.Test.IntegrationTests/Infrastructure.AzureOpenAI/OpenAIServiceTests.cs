using Azure;
using Azure.AI.OpenAI;
using Backend.Core.Models;
using Backend.Infrastructure.AzureOpenAI;
using Backend.Infrastructure.Tiktoken;
using Microsoft.Extensions.Configuration;
using System.Text;
using Tiktoken;

namespace Backend.Test.IntegrationTests.Infrastructure.AzureOpenAI
{
    public class OpenAIServiceTests
    {
        private readonly IConfigurationRoot _configuration;

        public OpenAIServiceTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<OpenAIServiceTests>()
                .AddEnvironmentVariables()
                .Build();
        }

        [Fact]
        public async Task GetChatResponseAsync_ReturnsExpectedResponse()
        {
            // Arrange
            var configSection = _configuration.GetSection(AzureOpenAIOptions.AzureOpenAI);
            var options = new AzureOpenAIOptions
            {
                Endpoint = configSection["Endpoint"]!,
                ApiKey = configSection["ApiKey"]!,
                DeploymentName = configSection["DeploymentName"]!
            };

            var azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
            var chatClient = azureClient.GetChatClient(options.DeploymentName);

            var openAIService = new OpenAIService(chatClient, new TokenizerService(ModelToEncoder.For("gpt-4")));
            // Act & Assert
            try
            {
                var result = await openAIService.GetChatResponseAsync([
                    new Core.Models.ChatMessage { Role = ChatRole.User, Content = "Hello" }
                ]);
                // Assert
                Assert.NotNull(result);
                Assert.NotEqual(string.Empty, result);
            }
            catch (Exception ex)
            {
                Assert.Contains("HTTP 429", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task GetChatResponseStreamingAsync_ReturnsExpectedResponse()
        {
            // Arrange
            var configSection = _configuration.GetSection(AzureOpenAIOptions.AzureOpenAI);
            var options = new AzureOpenAIOptions
            {
                Endpoint = configSection["Endpoint"]!,
                ApiKey = configSection["ApiKey"]!,
                DeploymentName = configSection["DeploymentName"]!
            };
            var azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
            var chatClient = azureClient.GetChatClient(options.DeploymentName);
            var openAIService = new OpenAIService(chatClient, new TokenizerService(ModelToEncoder.For("gpt-4")));
            // Act & Asset
            try
            {
                var result = openAIService.GetChatResponseStreamingAsync([
                new Core.Models.ChatMessage { Role = ChatRole.User, Content = "Hello" }
            ]);
                // Assert
                var responseBuilder = new StringBuilder();
                await foreach (var part in result)
                {
                    responseBuilder.Append(part);
                }
                Assert.NotNull(responseBuilder.ToString());
                Assert.NotEqual(string.Empty, responseBuilder.ToString());
            }
            catch (RequestFailedException ex)
            {
                // Assert
                Assert.Equal(429, ex.Status);
                // Optionally check the message
                Assert.Contains("Too Many Requests", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
