using System.Text;
using System.Text.Json;
using Aspire_Worker_Queue_Cache.Web.Models;

namespace Aspire_Worker_Queue_Cache.Web.Services
{
    public class RequestApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RequestApiClient> _logger;

        public RequestApiClient(HttpClient httpClient, ILogger<RequestApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> SubmitRequestAsync(SubmitRequestDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/api/request", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseJson);
                    return result?["id"];
                }
                
                _logger.LogError("Failed to submit request. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting request");
                return null;
            }
        }
    }
}
