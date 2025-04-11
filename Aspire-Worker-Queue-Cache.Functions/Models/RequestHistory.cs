using System.Text.Json.Serialization;

namespace Aspire_Worker_Queue_Cache.Functions.Models
{
    public class RequestHistory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("request")]
        public string Request { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("processedTimestamp")]
        public DateTime? ProcessedTimestamp { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending";

        public string? RawMessage { get; set; }
    }
}
