using Backend.Api;
using Microsoft.AspNetCore.Http;

namespace Backend.Test.UnitTests.Api
{
    public class CustomAuthenticationMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsAuthorizationHeader_WhenAccessTokenIsPresentInQuery()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?access_token=test_token");
            var next = new RequestDelegate((innerContext) => Task.CompletedTask);
            var middleware = new WebSocketAuthenticationMiddleware(next);
            // Act
            await middleware.InvokeAsync(context);
            // Assert
            Assert.Equal("Bearer test_token", context.Request.Headers["Authorization"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_DoesNotModifyRequest_WhenAccessTokenIsNotPresentInQuery()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var next = new RequestDelegate((innerContext) => Task.CompletedTask);
            var middleware = new WebSocketAuthenticationMiddleware(next);
            // Act
            await middleware.InvokeAsync(context);
            // Assert
            Assert.False(context.Request.Headers.ContainsKey("Authorization"));
        }

        [Fact]
        public async Task InvokeAsync_CallsNextMiddleware()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            var next = new RequestDelegate((innerContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var middleware = new WebSocketAuthenticationMiddleware(next);
            // Act
            await middleware.InvokeAsync(context);
            // Assert
            Assert.True(nextCalled);
        }
    }
}
