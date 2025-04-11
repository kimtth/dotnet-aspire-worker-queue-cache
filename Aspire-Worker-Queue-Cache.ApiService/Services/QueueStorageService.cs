using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;

namespace Aspire_Worker_Queue_Cache.ApiService.Services
{
    public class QueueStorageService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly QueueClient _queueClient;
        private readonly ILogger<QueueStorageService> _logger;

        public QueueStorageService(QueueServiceClient serviceClient, ILogger<QueueStorageService> logger)
        {
            _queueServiceClient = serviceClient;
            _queueClient = _queueServiceClient.GetQueueClient("requestqueue");
            _queueClient.CreateIfNotExists();
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync<T>(T message)
        {
            try
            {
                string messageJson = JsonSerializer.Serialize(message);
                var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(messageJson));
                await _queueClient.SendMessageAsync(data);
                _logger.LogInformation("Message sent to queue successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue");
                return false;
            }
        }
    }
}
