using Aspire_Worker_Queue_Cache.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

// Add Redis cache from Aspire service discovery
builder.AddRedisClient(connectionName: "cache");

// Add Azure Storage Queue client
builder.AddAzureQueueClient(connectionName: "requestqueue");

// Register the CosmosDB client
builder.AddAzureCosmosClient(connectionName: "cosmosdb");

// Register the CosmosDbService
builder.Services.AddScoped<CosmosDbService>();

builder.Build().Run();
