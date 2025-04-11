using Aspire_Worker_Queue_Cache.ApiService.Models;
using Aspire_Worker_Queue_Cache.ApiService.Services;
using Microsoft.Extensions.Hosting;


var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Azure Storage Queue client
builder.AddAzureQueueClient(connectionName: "requestqueue");

// Register the queue storage service
builder.Services.AddSingleton<QueueStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Define summaries array at the top level, before it's used
string[] Summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Existing weather forecast endpoint
app.MapGet("/weatherforecast", (HttpContext httpContext) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = GetSummary(Random.Shared.Next(0, Summaries.Length))
        })
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// New endpoint to submit a request to be processed
app.MapPost("/api/request", async (RequestHistoryDto request, QueueStorageService queueService) =>
{
    // Create a request history item with additional properties
    var requestHistory = new
    {
        Id = Guid.NewGuid().ToString(),
        request.UserId,
        request.Request,
        Timestamp = DateTime.UtcNow,
        Status = "Pending"
    };

    // Send the request to the queue for processing
    bool success = await queueService.SendMessageAsync(requestHistory);

    if (success)
    {
        return Results.Ok(new { Id = requestHistory.Id, Message = "Request submitted successfully" });
    }
    
    return Results.StatusCode(500);
})
.WithName("SubmitRequest")
.WithOpenApi();

// Map default endpoints like health checks
app.MapDefaultEndpoints();

app.Run();

string GetSummary(int index) => Summaries[index];

internal record WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
