using Backend.Api;
using Backend.Test.UnitTests.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Backend.Test.UnitTests.Api
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(logger.Object);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var next = new RequestDelegate((innerContext) =>
            {
                throw new Exception("Test Exception");
            });
            // Act
            await middleware.InvokeAsync(context, next);
            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            Assert.Equal("Test Exception", response!["error"]);
        }

        [Fact]
        public async Task InvokeAsync_ReturnsBadRequest_WhenBadRequestExceptionIsThrown()
        {
            // Arrange
            var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(logger.Object);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var next = new RequestDelegate((innerContext) =>
            {
                throw new TestBadRequestException("Test Exception");
            });
            // Act
            await middleware.InvokeAsync(context, next);
            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            Assert.Equal("Test Exception", response!["error"]);
        }

        [Fact]
        public async Task InvokeAsync_ReturnsNotFound_WhenNotFoundExceptionIsThrown()
        {
            // Arrange
            var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(logger.Object);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var next = new RequestDelegate((innerContext) =>
            {
                throw new TestNotFoundException("Test Exception");
            });
            // Act
            await middleware.InvokeAsync(context, next);
            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            Assert.Equal("Test Exception", response!["error"]);
        }
    }
}
