using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register Azure Storage services
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:AzureWebJobsStorage")
    ?? builder.Configuration.GetValue<string>("Values:AzureWebJobsStorage")
    ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton(provider => new BlobServiceClient(connectionString));
builder.Services.AddSingleton(provider => new QueueServiceClient(connectionString));
builder.Services.AddSingleton(provider => new TableServiceClient(connectionString));

var app = builder.Build();

// Initialize storage resources inline
try
{
    const string name = "demo";
    await app.Services.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(name).CreateIfNotExistsAsync();
    await app.Services.GetRequiredService<QueueServiceClient>().GetQueueClient(name).CreateIfNotExistsAsync();
    await app.Services.GetRequiredService<TableServiceClient>().CreateTableIfNotExistsAsync(name);
    Console.WriteLine("✅ All Azurite resources initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Warning: Could not initialize Azurite resources: {ex.Message}");
    Console.WriteLine("Make sure Azurite is running before starting the Functions app.");
}

app.Run();
