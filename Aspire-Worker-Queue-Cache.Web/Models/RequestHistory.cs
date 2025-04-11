namespace Aspire_Worker_Queue_Cache.Web.Models
{
    public class RequestHistory
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Request { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? ProcessedTimestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
