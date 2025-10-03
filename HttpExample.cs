using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Data.Tables;
using azure_function_azurite.Models;

namespace azure_function_azurite;

public class HttpExample
{
    private readonly ILogger<HttpExample> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public HttpExample(ILogger<HttpExample> logger, TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _tableServiceClient = tableServiceClient;
    }

    [Function("HttpExample")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var action = req.Query["action"].FirstOrDefault()?.ToLower() ?? "list";

        try
        {
            switch (action)
            {
                case "list":
                    return await ListAllEntities();
                case "seed":
                    return await SeedSampleData();
                case "tables":
                    return await ListAllTables();
                default:
                    return new BadRequestObjectResult("Invalid action. Use 'list', 'seed', or 'tables'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    private async Task<IActionResult> ListAllTables()
    {
        _logger.LogInformation("Listing all tables");

        var tables = new List<string>();
        await foreach (var tableItem in _tableServiceClient.QueryAsync())
        {
            tables.Add(tableItem.Name);
        }

        return new OkObjectResult(new { Tables = tables, Count = tables.Count });
    }

    private async Task<IActionResult> ListAllEntities()
    {
        _logger.LogInformation("Listing all entities from all tables");

        var result = new Dictionary<string, object>();
        var totalEntities = 0;

        await foreach (var tableItem in _tableServiceClient.QueryAsync())
        {
            var tableName = tableItem.Name;
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            var entities = new List<object>();
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                // Convert TableEntity to a more readable format
                var entityData = new Dictionary<string, object?>
                {
                    { "PartitionKey", entity.PartitionKey },
                    { "RowKey", entity.RowKey },
                    { "Timestamp", entity.Timestamp }
                };

                // Add all other properties
                foreach (var kvp in entity)
                {
                    if (kvp.Key != "PartitionKey" && kvp.Key != "RowKey" && kvp.Key != "Timestamp" && kvp.Key != "odata.etag")
                    {
                        entityData[kvp.Key] = kvp.Value;
                    }
                }

                entities.Add(entityData);
                totalEntities++;
            }

            result[tableName] = new { Entities = entities, Count = entities.Count };
        }

        return new OkObjectResult(new
        {
            Tables = result,
            TotalEntities = totalEntities,
            TableCount = result.Count
        });
    }

    private async Task<IActionResult> SeedSampleData()
    {
        _logger.LogInformation("Seeding sample data");

        const string tableName = "demo";
        var tableClient = _tableServiceClient.GetTableClient(tableName);

        // Ensure table exists
        await tableClient.CreateIfNotExistsAsync();

        var entities = new[]
        {
            new SampleEntity
            {
                PartitionKey = "sample",
                RowKey = "001",
                Name = "Sample Item 1",
                Description = "First sample item",
                Value = 100
            },
            new SampleEntity
            {
                PartitionKey = "sample",
                RowKey = "002",
                Name = "Sample Item 2",
                Description = "Second sample item",
                Value = 200
            },
            new SampleEntity
            {
                PartitionKey = "demo",
                RowKey = "001",
                Name = "Demo Item",
                Description = "Demo item for testing",
                Value = 300
            }
        };

        var insertedCount = 0;
        foreach (var entity in entities)
        {
            try
            {
                await tableClient.UpsertEntityAsync(entity);
                insertedCount++;
                _logger.LogInformation($"Inserted entity: {entity.PartitionKey}/{entity.RowKey}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to insert entity: {entity.PartitionKey}/{entity.RowKey}");
            }
        }

        return new OkObjectResult(new
        {
            Message = "Sample data seeded successfully",
            InsertedEntities = insertedCount,
            TableName = tableName
        });
    }
}
