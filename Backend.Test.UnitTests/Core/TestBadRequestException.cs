using Backend.Core.Exceptions;

namespace Backend.Test.UnitTests.Core
{
    public class TestBadRequestException(string message) : BadRequestException(message)
    {
    }
}
