var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// When using local emulator for the development, use RunAsEmulator:
// https://aka.ms/dotnet/aspire/azure/provisioning 
// https://learn.microsoft.com/en-us/dotnet/aspire/storage/azure-storage-queues-integration
var storage = builder.AddAzureStorage("storage");
var queue = storage.AddQueues("requestqueue");

// Add Azure Cosmos DB for document storage
// DO NOT use the Cosmos emulator, as the emulator object does not support a property to disable the SSL certificate.
// https://github.com/Azure/azure-cosmos-dotnet-v3/issues/4222
// https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-integration
var cosmos = builder.AddAzureCosmosDB("cosmosdb");
var db = cosmos.AddCosmosDatabase("requesthistorydb");
var container = db.AddContainer("requesthistory", "/Id");

var apiService = builder.AddProject<Projects.Aspire_Worker_Queue_Cache_ApiService>("apiservice")
    .WithReference(queue)
    .WaitFor(queue);

builder.AddProject<Projects.Aspire_Worker_Queue_Cache_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(cosmos)
    .WaitFor(cosmos);

// Add Azure Function project and connect it to the storage and cosmos
// Use string version to avoid namespace issues
builder.AddAzureFunctionsProject<Projects.Aspire_Worker_Queue_Cache_Functions>("function")
    .WithReference(queue)
    .WithReference(cache)
    .WithReference(cosmos)
    .WithReference(db)
    .WithReference(container)
    .WaitFor(queue)
    .WaitFor(cache)
    .WaitFor(cosmos)
    .WaitFor(db);

builder.Build().Run();
