using Aspire_Worker_Queue_Cache.Web.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;

namespace Aspire_Worker_Queue_Cache.Web.Services
{
    public class CosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly ILogger<CosmosDbService> _logger;

        public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger)
        {
            _cosmosClient = cosmosClient;
            _logger = logger;
            try
            {
                var database = _cosmosClient.GetDatabase("requesthistorydb");
                _container = database.GetContainer("requesthistory");
                _logger.LogInformation("Successfully connected to CosmosDB database and container");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize CosmosDB database and container");
                throw;
            }
        }

        public async Task<IEnumerable<RequestHistory>> GetRequestHistoryByUserIdAsync(string userId)
        {
            try
            {
                var queryable = _container.GetItemLinqQueryable<RequestHistory>()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Timestamp);
                
                using FeedIterator<RequestHistory> iterator = queryable.ToFeedIterator();
                
                var results = new List<RequestHistory>();
                while (iterator.HasMoreResults)
                {
                    try
                    {
                        var response = await iterator.ReadNextAsync();
                        results.AddRange(response);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limited by Cosmos DB. Retrying after delay...");
                        await Task.Delay(1000); // Simple delay before retry
                        continue;
                    }
                    catch (CosmosException ex)
                    {
                        _logger.LogError(ex, "Cosmos DB error during query iteration: {StatusCode}, {Message}", 
                            ex.StatusCode, ex.Message);
                        throw new ApplicationException($"Database error (Status: {ex.StatusCode}). Please ensure the Cosmos DB.", ex);
                    }
                }
                
                return results;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable || 
                                            ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                _logger.LogError(ex, "Cosmos DB service unavailable or request timed out. Status: {StatusCode}", ex.StatusCode);
                throw new ApplicationException("Unable to connect to the database service. Please ensure the Cosmos DB.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request history for user {UserId}", userId);
                throw new ApplicationException("Failed to query the database. Please check connection and ensure the Cosmos DB.", ex);
            }
        }

        public async Task<IEnumerable<RequestHistory>> GetAllRequestHistoryAsync()
        {
            try
            {
                var queryable = _container.GetItemLinqQueryable<RequestHistory>()
                    .OrderByDescending(r => r.Timestamp);
                
                using FeedIterator<RequestHistory> iterator = queryable.ToFeedIterator();
                
                var results = new List<RequestHistory>();
                while (iterator.HasMoreResults)
                {
                    try
                    {
                        var response = await iterator.ReadNextAsync();
                        results.AddRange(response);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limited by Cosmos DB. Retrying after delay...");
                        await Task.Delay(1000); // Simple delay before retry
                        continue;
                    }
                    catch (CosmosException ex)
                    {
                        _logger.LogError(ex, "Cosmos DB error during query iteration: {StatusCode}, {Message}", 
                            ex.StatusCode, ex.Message);
                        throw new ApplicationException($"Database error (Status: {ex.StatusCode}). Please ensure the Cosmos DB.", ex);
                    }
                }
                
                return results;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable || 
                                            ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                _logger.LogError(ex, "Cosmos DB service unavailable or request timed out. Status: {StatusCode}", ex.StatusCode);
                throw new ApplicationException("Unable to connect to the database service. Please ensure the Cosmos DB.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all request history: {Message}", ex.Message);
                throw new ApplicationException("Failed to query the database. Please check connection and ensure the Cosmos DB.", ex);
            }
        }
    }
}
