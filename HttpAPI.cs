using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;

namespace invoiceAPI;

public class HttpAPI
{
    private readonly ILogger<HttpAPI> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public HttpAPI(ILogger<HttpAPI> logger, TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _tableServiceClient = tableServiceClient;
    }

    [Function(nameof(HttpAPI))]
    [OpenApiOperation(operationId: "HttpAPI", tags: new[] { "Invoice API" }, Summary = "Invoice API Demo and Documentation", Description = "Get API information or list storage entities")]
    [OpenApiParameter(name: "action", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Action to perform: list (default), tables")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "API information or requested data")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid action parameter")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
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
                case "tables":
                    return await ListAllTables();
                default:
                    return new BadRequestObjectResult("Invalid action. Use 'list' or 'tables'");
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
}
