using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using invoiceAPI.Tests;

namespace invoiceAPI;

/// <summary>
/// HTTP endpoint for running basic tests
/// </summary>
public class TestController
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [Function("RunTests")]
    public IActionResult RunTests([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tests")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("Running basic tests for Invoice API");

            // Capture console output
            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Run the tests
            BasicTests.RunAllTests();

            // Restore console output
            Console.SetOut(originalOut);

            var testOutput = sw.ToString();
            _logger.LogInformation("Tests completed successfully");

            return new OkObjectResult(new
            {
                Status = "Success",
                Message = "All basic tests passed successfully",
                TestOutput = testOutput,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tests failed");
            return new BadRequestObjectResult(new
            {
                Status = "Failed",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
