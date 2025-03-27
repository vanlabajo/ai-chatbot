using Azure;
using Azure.AI.OpenAI;
using Backend.Infrastructure.AzureOpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Backend.Test.UnitTests.Api
{
    public class ProgramTests
    {
        [Fact]
        public void ChatClient_IsRegisteredCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "AzureOpenAI:Endpoint", "https://example.openai.azure.com/" },
                    { "AzureOpenAI:ApiKey", "test-api-key" },
                    { "AzureOpenAI:DeploymentName", "test-deployment" }
                })
                .Build();

            services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.AzureOpenAI));
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
                var azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
                return azureClient.GetChatClient(options.DeploymentName);
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var chatClient = serviceProvider.GetService<ChatClient>();

            // Assert
            Assert.NotNull(chatClient);
        }
    }
}
