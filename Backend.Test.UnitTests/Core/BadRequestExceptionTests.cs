namespace Backend.Test.UnitTests.Core
{
    public class BadRequestExceptionTests
    {
        [Fact]
        public void Constructor_SetsMessage()
        {
            // Arrange
            var expectedMessage = "Test message";

            // Act
            var exception = new TestBadRequestException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
