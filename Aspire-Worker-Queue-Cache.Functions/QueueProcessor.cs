using System.Text.Json;
using Aspire_Worker_Queue_Cache.Functions.Models;
using Aspire_Worker_Queue_Cache.Functions.Services;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Aspire_Worker_Queue_Cache.Functions
{
    public class QueueProcessor
    {
        private readonly ILogger<QueueProcessor> _logger;
        private readonly CosmosDbService _cosmosDbService;

        public QueueProcessor(ILogger<QueueProcessor> logger, CosmosDbService cosmosDbService)
        {
            _logger = logger;
            _cosmosDbService = cosmosDbService;
        }

        [Function("ProcessQueueMessage")]
        public async Task Run([QueueTrigger("requestqueue", Connection = "ConnectionStringsQueue")] QueueMessage queueMessage, FunctionContext context)
        {
            // Get the retry count from the function context to understand where we are in the retry cycle
            var dequeueCount = context.BindingContext.BindingData.TryGetValue("DequeueCount", out var dequeueCountObj)
                ? Convert.ToInt32(dequeueCountObj)
                : 1;

            _logger.LogInformation("Queue trigger function processed: {Message} (Attempt {DequeueCount}/5)", queueMessage.Body.ToString(), dequeueCount);

            try
            {
                var options = new JsonSerializerOptions
                {
                    //PropertyNameCaseInsensitive = true,
                    //PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Deserialize the queue message into a RequestHistory object
                string messageText = queueMessage.Body.ToString();
                var requestHistory = JsonSerializer.Deserialize<RequestHistory>(messageText, options);

                if (requestHistory != null)
                {
                    // Make sure the Id property is set to a valid value
                    if (string.IsNullOrEmpty(requestHistory.Id))
                    {
                        requestHistory.Id = Guid.NewGuid().ToString();
                        _logger.LogWarning("Request history had no ID, generated new ID: {Id}", requestHistory.Id);
                    }

                    // Process the request (in a real app, this might be a longer-running operation)
                    _logger.LogInformation("Processing request for user {UserId}", requestHistory.UserId);

                    requestHistory.ProcessedTimestamp = DateTime.UtcNow;
                    requestHistory.Status = "Completed";

                    // Check if the document already exists in Cosmos DB before saving
                    bool documentExists = await _cosmosDbService.DocumentExistsAsync(requestHistory.Id);
                    if (documentExists)
                    {
                        _logger.LogInformation("Document with ID {Id} already exists, updating it", requestHistory.Id);
                        await _cosmosDbService.UpdateRequestHistoryAsync(requestHistory);
                    }
                    else
                    {
                        _logger.LogInformation("Document with ID {Id} does not exist, creating it", requestHistory.Id);
                        await _cosmosDbService.SaveRequestHistoryAsync(requestHistory);
                    }

                    //await _cosmosDbService.SaveRequestHistoryAsync(requestHistory);
                    _logger.LogInformation("Request processing completed for ID {Id}", requestHistory.Id);
                }
                else
                {
                    _logger.LogWarning("Received null RequestHistory after deserialization. Message: {Message}", messageText);
                    // Create a failure record for tracking
                    await CreateFailureRecord(messageText, "Null deserialization result");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue message: {Message}", queueMessage.Body.ToString());
                // Create a failure record but don't rethrow for non-transient errors
                await CreateFailureRecord(queueMessage.Body.ToString(), $"error: {ex.Message}");
            }
        }

        // Helper method to create a failure record in CosmosDB
        private async Task CreateFailureRecord(string rawMessage, string errorDetails)
        {
            try
            {
                // Create a generic failure record
                var failedRequest = new RequestHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = "unknown",
                    Request = "Failed message processing",
                    Timestamp = DateTime.UtcNow,
                    ProcessedTimestamp = DateTime.UtcNow,
                    Status = $"Failed - {errorDetails}",
                    RawMessage = rawMessage.Length > 500 ? rawMessage.Substring(0, 500) + "..." : rawMessage
                };

                // Use SaveRequestHistoryAsync instead of UpdateRequestHistoryAsync since this is a new record
                await _cosmosDbService.SaveRequestHistoryAsync(failedRequest);
                _logger.LogInformation("Failure record created with ID {Id}", failedRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating failure record");
                // Don't throw - this is a best effort operation
            }
        }


    }
}
