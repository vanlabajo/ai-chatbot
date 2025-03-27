using Azure;
using Azure.AI.OpenAI;
using Backend.Infrastructure.AzureOpenAI;
using Microsoft.Extensions.Configuration;

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

            var openAIService = new OpenAIService(chatClient);
            // Act
            var result = await openAIService.GetChatResponseAsync("Hello");
            // Assert
            Assert.NotEqual(string.Empty, result);
        }
    }
}
