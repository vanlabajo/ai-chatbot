namespace Backend.Core.Exceptions
{
    public class OpenAIRateLimitException : BadRequestException
    {
        public OpenAIRateLimitException() : base("OpenAI rate limit exceeded. Please try again later.")
        {
        }
    }
}
