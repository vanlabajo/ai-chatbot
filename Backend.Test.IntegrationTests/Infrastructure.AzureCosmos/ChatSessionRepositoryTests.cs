using Backend.Core.Models;
using Backend.Infrastructure.AzureCosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Backend.Test.IntegrationTests.Infrastructure.AzureCosmos
{
    public class ChatSessionRepositoryTests
    {
        private readonly IConfigurationRoot _configuration;

        public ChatSessionRepositoryTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<ChatSessionRepositoryTests>()
                .AddEnvironmentVariables()
                .Build();
        }

        [Fact]
        public async Task GetChatSessionAsync_ReturnsNull_WhenSessionDoesNotExist()
        {
            // Arrange
            var configSection = _configuration.GetSection("CosmosDb");
            var options = new AzureCosmosDbOptions
            {
                Endpoint = configSection["Endpoint"]!,
                Key = configSection["Key"]!,
                DatabaseName = configSection["DatabaseName"]!,
                ContainerName = configSection["ContainerName"]!
            };
            var cosmosClient = new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
            var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
            var repository = new ChatSessionRepository(container);
            // Act
            var sessionId = Guid.NewGuid().ToString(); // unlikely to exist
            var session = await repository.GetSessionAsync("test-123", sessionId);
            // Assert
            Assert.Null(session);
        }

        [Fact]
        public async Task GetChatSessionAsync_ReturnsSession_WhenSessionExists()
        {
            // Arrange
            var configSection = _configuration.GetSection("CosmosDb");
            var options = new AzureCosmosDbOptions
            {
                Endpoint = configSection["Endpoint"]!,
                Key = configSection["Key"]!,
                DatabaseName = configSection["DatabaseName"]!,
                ContainerName = configSection["ContainerName"]!
            };
            var cosmosClient = new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, SerializerOptions = new() { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });
            var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
            var repository = new ChatSessionRepository(container);

            var userId = $"test-user-{Guid.NewGuid()}";
            var sessionId = Guid.NewGuid().ToString();
            var session = new ChatSession
            {
                Id = sessionId,
                UserId = userId,
                Title = "Test Session",
                Timestamp = DateTime.UtcNow,
                Messages = [
                    new() { Content = "Hello", Timestamp = DateTime.UtcNow, Role = ChatRole.System },
                ]
            };

            // Insert the session
            await repository.SaveSessionAsync(session);

            try
            {
                // Act
                var fetchedSession = await repository.GetSessionAsync(userId, sessionId);

                // Assert
                Assert.NotNull(fetchedSession);
                Assert.Equal(sessionId, fetchedSession.Id);
                Assert.Equal(userId, fetchedSession.UserId);
                Assert.Equal("Test Session", fetchedSession.Title);
                Assert.Equal(ChatRole.System, fetchedSession.Messages[0].Role);
            }
            finally
            {
                // Clean up
                await repository.DeleteSessionAsync(userId, sessionId);
            }
        }

        [Fact]
        public async Task GetAllSessionsForUserAsync_ReturnsEmpty_WhenNoSessionsExist()
        {
            // Arrange
            var configSection = _configuration.GetSection("CosmosDb");
            var options = new AzureCosmosDbOptions
            {
                Endpoint = configSection["Endpoint"]!,
                Key = configSection["Key"]!,
                DatabaseName = configSection["DatabaseName"]!,
                ContainerName = configSection["ContainerName"]!
            };
            var cosmosClient = new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
            var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
            var repository = new ChatSessionRepository(container);
            // Act
            var sessions = await repository.GetAllSessionsForUserAsync("test-user");
            // Assert
            Assert.Empty(sessions);
        }

        [Fact]
        public async Task GetAllSessionsForUserAsync_ReturnsSessions_WhenSessionsExist()
        {
            // Arrange
            var configSection = _configuration.GetSection("CosmosDb");
            var options = new AzureCosmosDbOptions
            {
                Endpoint = configSection["Endpoint"]!,
                Key = configSection["Key"]!,
                DatabaseName = configSection["DatabaseName"]!,
                ContainerName = configSection["ContainerName"]!
            };
            var cosmosClient = new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, SerializerOptions = new() { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });
            var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
            var repository = new ChatSessionRepository(container);
            var userId = $"test-user-{Guid.NewGuid()}";
            var sessionId1 = Guid.NewGuid().ToString();
            var session1 = new ChatSession
            {
                Id = sessionId1,
                UserId = userId,
                Title = "Test Session 1",
                Timestamp = DateTime.UtcNow,
                Messages = [
                    new() { Content = "Hello from session 1", Timestamp = DateTime.UtcNow, Role = ChatRole.System },
                ]
            };
            var sessionId2 = Guid.NewGuid().ToString();
            var session2 = new ChatSession
            {
                Id = sessionId2,
                UserId = userId,
                Title = "Test Session 2",
                Timestamp = DateTime.UtcNow,
                Messages = [
                    new() { Content = "Hello from session 2", Timestamp = DateTime.UtcNow, Role = ChatRole.Assistant },
                ]
            };
            // Insert the sessions
            await repository.SaveSessionAsync(session1);
            await repository.SaveSessionAsync(session2);

            try
            {
                // Act
                var sessions = (await repository.GetAllSessionsForUserAsync(userId)).ToList();
                // Assert
                Assert.NotEmpty(sessions);
                Assert.True(sessions.Count >= 2);
                Assert.Contains(sessions, s => s.Id == sessionId1);
                Assert.Contains(sessions, s => s.Id == sessionId2);

                var fetched1 = sessions.First(s => s.Id == sessionId1);
                var fetched2 = sessions.First(s => s.Id == sessionId2);
                Assert.Equal(ChatRole.System, fetched1.Messages[0].Role);
                Assert.Equal(ChatRole.Assistant, fetched2.Messages[0].Role);
            }
            finally
            {
                // Clean up
                await repository.DeleteSessionAsync(userId, sessionId1);
                await repository.DeleteSessionAsync(userId, sessionId2);
            }
        }

        [Fact]
        public async Task GetAllSessionsAsync_ReturnsSessions_WhenSessionsExist()
        {
            // Arrange
            var configSection = _configuration.GetSection("CosmosDb");
            var options = new AzureCosmosDbOptions
            {
                Endpoint = configSection["Endpoint"]!,
                Key = configSection["Key"]!,
                DatabaseName = configSection["DatabaseName"]!,
                ContainerName = configSection["ContainerName"]!
            };
            var cosmosClient = new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, SerializerOptions = new() { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });
            var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
            var repository = new ChatSessionRepository(container);
            var sessionId1 = Guid.NewGuid().ToString();
            var session1 = new ChatSession
            {
                Id = sessionId1,
                UserId = "test-user-1",
                Title = "Test Session 1",
                Timestamp = DateTime.UtcNow,
                Messages = [
                    new() { Content = "Hello from session 1", Timestamp = DateTime.UtcNow, Role = ChatRole.System },
                ]
            };
            var sessionId2 = Guid.NewGuid().ToString();
            var session2 = new ChatSession
            {
                Id = sessionId2,
                UserId = "test-user-2",
                Title = "Test Session 2",
                Timestamp = DateTime.UtcNow,
                Messages = [
                    new() { Content = "Hello from session 2", Timestamp = DateTime.UtcNow, Role = ChatRole.Assistant },
                ]
            };
            // Insert the sessions
            await repository.SaveSessionAsync(session1);
            await repository.SaveSessionAsync(session2);
            try
            {
                // Act
                var sessions = (await repository.GetAllSessionsAsync()).ToList();
                // Assert
                Assert.NotEmpty(sessions);
                Assert.True(sessions.Count >= 2);
                Assert.Contains(sessions, s => s.Id == sessionId1);
                Assert.Contains(sessions, s => s.Id == sessionId2);
            }
            finally
            {
                // Clean up
                await repository.DeleteSessionAsync(session1.UserId, sessionId1);
                await repository.DeleteSessionAsync(session2.UserId, sessionId2);
            }
        }
    }
}
