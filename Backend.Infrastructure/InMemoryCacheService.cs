using Backend.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Infrastructure
{
    public sealed class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
    {
        private readonly IMemoryCache _memoryCache = memoryCache;

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedData = _memoryCache.Get<T>(key);
            return Task.FromResult(cachedData);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            _memoryCache.Set(key, value, expiration ?? TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }
    }
}
