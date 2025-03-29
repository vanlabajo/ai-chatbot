using Backend.Infrastructure.Tiktoken;
using Tiktoken;

namespace Backend.Test.UnitTests.Infrastructure.Tiktoken
{
    public class TokenizerServiceTests
    {
        [Fact]
        public void Tokenize_ReturnsExpectedTokens()
        {
            // Arrange
            var tokenizerService = new TokenizerService(ModelToEncoder.For("gpt-4"));
            var text = "Hello, how are you?";
            var expectedCount = 6;
            // Act
            var result = tokenizerService.CountTokens(text);
            // Assert
            Assert.Equal(expectedCount, result);
        }
    }
}
