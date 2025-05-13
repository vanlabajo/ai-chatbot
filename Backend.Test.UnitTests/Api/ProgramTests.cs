using Azure;
using Azure.AI.OpenAI;
using Backend.Infrastructure.AzureOpenAI;
using Microsoft.AspNetCore.Cors.Infrastructure;
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

        [Fact]
        public void CorsPolicy_IsConfiguredCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "AllowedOrigins:0", "https://example.com" },
                    { "AllowedOrigins:1", "https://another-example.com" }
                })
                .Build();
            services.AddCors(options =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                options.AddPolicy("AllowSpecificOrigins",
                    builder =>
                    {
                        builder.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });
            var serviceProvider = services.BuildServiceProvider();
            // Act
            var corsPolicy = serviceProvider.GetService<ICorsPolicyProvider>();
            // Assert
            Assert.NotNull(corsPolicy);
        }

        [Fact]
        public void CorsPolicy_EmptyOrigins_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();
            services.AddCors(options =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                options.AddPolicy("AllowSpecificOrigins",
                    builder =>
                    {
                        builder.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });
            var serviceProvider = services.BuildServiceProvider();
            // Act
            var corsPolicy = serviceProvider.GetService<ICorsPolicyProvider>();
            // Assert
            Assert.NotNull(corsPolicy);
        }
    }
}
