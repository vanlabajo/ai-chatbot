using Backend.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Backend.Test.UnitTests.Api
{
    public class QueryStringTokenMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsAuthorizationHeader_WhenAccessTokenIsPresentInQuery()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(true);
            context.Features.Set(webSocketFeature.Object);
            context.Request.QueryString = new QueryString("?access_token=test_token");
            var next = new RequestDelegate((innerContext) => Task.CompletedTask);
            var middleware = new QueryStringTokenMiddleware(next);
            // Act
            await middleware.InvokeAsync(context);
            // Assert
            Assert.Equal("Bearer test_token", context.Request.Headers.Authorization.ToString());
        }

        [Fact]
        public async Task InvokeAsync_DoesNotModifyRequest_WhenAccessTokenIsNotPresentInQuery()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(true);
            context.Features.Set(webSocketFeature.Object);
            var next = new RequestDelegate((innerContext) => Task.CompletedTask);
            var middleware = new QueryStringTokenMiddleware(next);
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
            var webSocketFeature = new Mock<IHttpWebSocketFeature>();
            webSocketFeature.Setup(f => f.IsWebSocketRequest).Returns(false);
            context.Features.Set(webSocketFeature.Object);
            var nextCalled = false;
            var next = new RequestDelegate((innerContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
            var middleware = new QueryStringTokenMiddleware(next);
            // Act
            await middleware.InvokeAsync(context);
            // Assert
            Assert.True(nextCalled);
        }
    }
}
