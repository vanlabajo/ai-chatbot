using Backend.Core.Exceptions;

namespace Backend.Test.UnitTests.Core
{
    public class NotFoundExceptionTests
    {
        [Fact]
        public void Constructor_SetsMessage()
        {
            // Arrange
            var expectedMessage = "Test message";

            // Act
            var exception = new NotFoundException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
