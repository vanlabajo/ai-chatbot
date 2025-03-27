using Backend.Core.Exceptions;

namespace Backend.Test.UnitTests.Core
{
    public class TestNotFoundException(string message): NotFoundException(message)
    {
    }
}
