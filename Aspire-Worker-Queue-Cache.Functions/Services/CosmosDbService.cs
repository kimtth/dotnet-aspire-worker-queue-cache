using Aspire_Worker_Queue_Cache.Functions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text;

namespace Aspire_Worker_Queue_Cache.Functions.Services
{
    public class CosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly ILogger<CosmosDbService> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _cache;
        private readonly TimeSpan _defaultCacheExpiry = TimeSpan.FromMinutes(15);

        public CosmosDbService(CosmosClient cosmosClient, IConnectionMultiplexer redis, ILogger<CosmosDbService> logger)
        {
            _cosmosClient = cosmosClient;
            _redis = redis;
            _logger = logger;
            _cache = _redis.GetDatabase();
            var database = _cosmosClient.GetDatabase("requesthistorydb");
            _container = database.GetContainer("requesthistory");
        }

        public async Task<RequestHistory> SaveRequestHistoryAsync(RequestHistory requestHistory)
        {
            try
            {
                var response = await _container.CreateItemAsync(requestHistory);
                _logger.LogInformation("Request history saved with ID: {Id}", requestHistory.Id);
                
                // Store in cache
                string cacheKey = $"request:{requestHistory.Id}";
                await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(response.Resource), _defaultCacheExpiry);
                
                // Also invalidate user's request history list cache
                string userCacheKey = $"user:{requestHistory.UserId}:requests";
                await _cache.KeyDeleteAsync(userCacheKey);
                
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving request history");
                throw;
            }
        }

        public async Task<RequestHistory> UpdateRequestHistoryAsync(RequestHistory requestHistory)
        {
            try
            {
                var response = await _container.ReplaceItemAsync(requestHistory, requestHistory.Id);
                _logger.LogInformation("Request history updated with ID: {Id}", requestHistory.Id);
                
                // Update cache
                string cacheKey = $"request:{requestHistory.Id}";
                await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(response.Resource), _defaultCacheExpiry);
                
                // Also invalidate user's request history list cache
                string userCacheKey = $"user:{requestHistory.UserId}:requests";
                await _cache.KeyDeleteAsync(userCacheKey);
                
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request history");
                throw;
            }
        }
        // Add this method to your CosmosDbService class
        public async Task<bool> DocumentExistsAsync(string id)
        {
            try
            {
                var partitionKey = new PartitionKey(id);
                var response = await _container.ReadItemAsync<RequestHistory>(id, partitionKey);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if document exists with ID: {Id}", id);
                throw;
            }
        }

        public async Task<RequestHistory> CreateOrUpdateRequestHistoryAsync(RequestHistory requestHistory)
        {
            try
            {
                bool exists = await DocumentExistsAsync(requestHistory.Id);

                if (exists)
                {
                    _logger.LogInformation("Document with ID: {Id} already exists, updating it", requestHistory.Id);
                    return await UpdateRequestHistoryAsync(requestHistory);
                }
                else
                {
                    _logger.LogInformation("Document with ID: {Id} does not exist, creating it", requestHistory.Id);
                    return await SaveRequestHistoryAsync(requestHistory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating request history");
                throw;
            }
        }
    }
}
