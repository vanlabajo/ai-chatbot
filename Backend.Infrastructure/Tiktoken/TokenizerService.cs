using Backend.Core;
using Tiktoken;

namespace Backend.Infrastructure.Tiktoken
{
    public class TokenizerService(Encoder encoder) : ITokenizerService
    {
        private readonly Encoder _encoder = encoder;

        public int CountTokens(string text)
        {
            return _encoder.CountTokens(text);
        }
    }
}
