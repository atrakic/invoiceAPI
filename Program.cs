using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

using invoiceAPI.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure OpenAPI
builder.Services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
{
    return new OpenApiConfigurationOptions()
    {
        Info = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "Invoice API",
            Description = "A minimal Azure Functions API for managing invoices, customers, and invoice items with PDF generation capabilities."
        },
        Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
        IncludeRequestingHostName = true,
        ForceHttps = false,
        ForceHttp = false
    };
});

// Register Azure Storage services
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:AzureWebJobsStorage")
    ?? builder.Configuration.GetValue<string>("Values:AzureWebJobsStorage")
    ?? "UseDevelopmentStorage=true"; // fallback to Azurite

builder.Services.AddSingleton(provider => new BlobServiceClient(connectionString));
builder.Services.AddSingleton(provider => new QueueServiceClient(connectionString));
builder.Services.AddSingleton(provider => new TableServiceClient(connectionString));

// Register services
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PdfGenerationService>();
builder.Services.AddScoped<DataSeedingService>();

var app = builder.Build();

// Initialize storage resources
try
{
    var blobServiceClient = app.Services.GetRequiredService<BlobServiceClient>();
    var queueServiceClient = app.Services.GetRequiredService<QueueServiceClient>();
    var tableServiceClient = app.Services.GetRequiredService<TableServiceClient>();

    // Create containers
    await blobServiceClient.GetBlobContainerClient("invoices").CreateIfNotExistsAsync();
    await blobServiceClient.GetBlobContainerClient("invoice-pdfs").CreateIfNotExistsAsync();
    Console.WriteLine("✅ Created blob containers: invoices, invoice-pdfs");

    // Create queues
    await queueServiceClient.GetQueueClient("pdf-generation").CreateIfNotExistsAsync();
    Console.WriteLine("✅ Created queue: pdf-generation");

    // Create tables
    await tableServiceClient.CreateTableIfNotExistsAsync("invoices");
    await tableServiceClient.CreateTableIfNotExistsAsync("invoiceitems");
    await tableServiceClient.CreateTableIfNotExistsAsync("customers");
    Console.WriteLine("✅ Created tables: invoices, invoiceitems, customers");

    // Auto-seed data when using development storage
    if (connectionString.Contains("UseDevelopmentStorage=true"))
    {
        Console.WriteLine("🌱 Development storage detected - auto-seeding sample data...");
        var dataSeedingService = app.Services.GetRequiredService<DataSeedingService>();
        await dataSeedingService.SeedDevelopmentDataAsync();
    }

    Console.WriteLine("🎉 Invoice App storage resources initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Warning: Could not initialize storage resources: {ex.Message}");
    Console.WriteLine("Make sure Azurite is running before starting the Functions app.");
}

app.Run();
