using Backend.Core.Models;
using Backend.Core.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Backend.Infrastructure.AzureCosmos
{
    public class ChatSessionRepository(Container container) : IChatSessionRepository
    {
        private readonly Container _container = container;

        public async Task DeleteSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            await _container.DeleteItemAsync<ChatSession>(sessionId, new PartitionKey(userId), cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<ChatSession>> GetAllSessionsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var query = _container.GetItemLinqQueryable<ChatSession>(allowSynchronousQueryExecution: false)
                .Where(s => s.UserId == userId)
                .ToFeedIterator();

            var results = new List<ChatSession>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }
            return results;
        }

        public async Task<ChatSession?> GetSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _container.ReadItemAsync<ChatSession>(sessionId, new PartitionKey(userId), cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
        {
            await _container.UpsertItemAsync(session, new PartitionKey(session.UserId), cancellationToken: cancellationToken);
        }
    }
}
