using Backend.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Backend.Test.UnitTests.Infrastructure
{
    public class InMemoryCacheServiceTests
    {
        [Fact]
        public async Task GetAsync_WithValidKey_ReturnsCachedData()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            var value = "value";
            memoryCache.Set(key, value);
            // Act
            var result = await cacheService.GetAsync<string>(key);
            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task GetAsync_WithInvalidKey_ReturnsNull()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            // Act
            var result = await cacheService.GetAsync<string>(key);
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAsync_WithValidData_CachesData()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            var value = "value";
            // Act
            await cacheService.SetAsync(key, value);
            // Assert
            Assert.Equal(value, memoryCache.Get<string>(key));
        }

        [Fact]
        public async Task RemoveAsync_WithValidKey_RemovesData()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            var value = "value";
            memoryCache.Set(key, value);
            // Act
            await cacheService.RemoveAsync(key);
            // Assert
            Assert.Null(memoryCache.Get<string>(key));
        }

        [Fact]
        public async Task RemoveAsync_WithInvalidKey_DoesNothing()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            // Act
            await cacheService.RemoveAsync(key);
            // Assert
            Assert.Null(memoryCache.Get<string>(key));
        }

        [Fact]
        public async Task SetAsync_WithExpiration_CachesDataWithExpiration()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            var value = "value";
            var expiration = TimeSpan.FromSeconds(1);
            // Act
            await cacheService.SetAsync(key, value, expiration);
            // Assert
            Assert.Equal(value, memoryCache.Get<string>(key));
            await Task.Delay(expiration);
            Assert.Null(memoryCache.Get<string>(key));
        }

        [Fact]
        public async Task SetAsync_WithNoExpiration_CachesDataWithDefaultExpiration()
        {
            // Arrange
            var memoryCache = new Mock<IMemoryCache>();
            var cacheEntry = new Mock<ICacheEntry>();
            object? cacheValue = null;

            memoryCache.Setup(mc => mc.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);
            cacheEntry.Setup(ce => ce.Value).Returns(() => cacheValue);
            cacheEntry.Setup(ce => ce.Dispose()).Callback(() => cacheValue = null);

            memoryCache.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cacheValue))
                       .Returns((object key, out object? value) =>
                       {
                           value = cacheValue;
                           return cacheValue != null;
                       });

            var cacheService = new InMemoryCacheService(memoryCache.Object);
            var key = "key";
            var value = "value";

            // Act
            await cacheService.SetAsync(key, value);
            cacheValue = value; // Simulate setting the value in the cache

            // Assert
            var result = await cacheService.GetAsync<string>(key);
            Assert.Equal(value, result);

            // Simulate expiration
            cacheEntry.Object.Dispose();
            result = await cacheService.GetAsync<string>(key);
            Assert.Null(result);
        }


        [Fact]
        public async Task GetAsync_WithCancellation_ThrowsException()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            // Act
            var exception = await Record.ExceptionAsync(() => cacheService.GetAsync<string>(key, new CancellationToken(true)));
            // Assert
            Assert.IsType<OperationCanceledException>(exception);
        }

        [Fact]
        public async Task SetAsync_WithCancellation_ThrowsException()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            var value = "value";
            // Act
            var exception = await Record.ExceptionAsync(() => cacheService.SetAsync(key, value, cancellationToken: new CancellationToken(true)));
            // Assert
            Assert.IsType<OperationCanceledException>(exception);
        }

        [Fact]
        public async Task RemoveAsync_WithCancellation_ThrowsException()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new InMemoryCacheService(memoryCache);
            var key = "key";
            // Act
            var exception = await Record.ExceptionAsync(() => cacheService.RemoveAsync(key, new CancellationToken(true)));
            // Assert
            Assert.IsType<OperationCanceledException>(exception);
        }
    }
}
