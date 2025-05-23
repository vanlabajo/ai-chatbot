using Backend.Api.Hubs;
using Backend.Core.Models;
using Backend.Test.IntegrationTests.Api;
using Microsoft.AspNetCore.SignalR.Client;

namespace Backend.Test.IntegrationTests.Hubs
{
    public class ChatHubTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task ChatHub_RespondsToChat()
        {
            // Arrange
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(_client.BaseAddress + "hubs/chat", options =>
                {
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                })
                .Build();

            var sessionId = Guid.NewGuid().ToString();
            ChatMessage userPrompt = new() { Role = ChatRole.User, Content = "Hello, world!" };
            ChatSession session = new()
            {
                Id = sessionId,
                Messages = [userPrompt]
            };
            var chunkBuffer = new List<string>();

            hubConnection.On<string>(HubEventNames.ResponseStreamChunk, chunk =>
            {
                chunkBuffer.Add(chunk);
            });

            hubConnection.On<ChatSession>(HubEventNames.SessionUpdate, sessionUpdate =>
            {
                if (session.Id == sessionId)
                {
                    session.Title = sessionUpdate.Title;
                }
            });

            await hubConnection.StartAsync();

            // Act
            await hubConnection.InvokeAsync("SendMessage", "Hello, world!", sessionId);

            // Allow time for response
            await Task.Delay(2000);

            // StreamChunk is a direct response from the OpenAI API
            // So convert it to a Assistant ChatMessage
            session.Messages.Add(new()
            {
                Role = ChatRole.Assistant,
                Content = string.Join("", chunkBuffer)
            });

            // Assert
            Assert.NotNull(session);
            Assert.True(session.Messages.Count > 1);
            Assert.Equal(ChatRole.User, session.Messages[0].Role);
            Assert.Equal(ChatRole.User.ToString(), session.Messages[0].RoleName);
            Assert.Equal(userPrompt.Content, session.Messages[0].Content);
            Assert.False(string.IsNullOrEmpty(session.Messages[1].Content));
            Assert.Equal(ChatRole.Assistant, session.Messages[1].Role);
            Assert.Equal(sessionId, session.Id);
            Assert.False(string.IsNullOrEmpty(session.Title));

            await hubConnection.StopAsync();
        }
    }
}
