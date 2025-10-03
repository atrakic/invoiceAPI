using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using invoiceAPI.Models;
using invoiceAPI.Services;
using System.Text.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;

namespace invoiceAPI;

public class InvoiceController
{
    private readonly ILogger<InvoiceController> _logger;
    private readonly InvoiceService _invoiceService;

    public InvoiceController(ILogger<InvoiceController> logger, InvoiceService invoiceService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
    }

    // GET /api/invoices - List all invoices
    [Function("GetInvoices")]
    [OpenApiOperation(operationId: "GetInvoices", tags: new[] { "Invoices" }, Summary = "Get all invoices", Description = "Retrieve all invoices, optionally filtered by customer name")]
    [OpenApiParameter(name: "customer", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by customer name")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Invoice[]), Description = "List of invoices")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> GetInvoices([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices")] HttpRequest req)
    {
        try
        {
            var customerName = req.Query["customer"].FirstOrDefault();
            var invoices = await _invoiceService.GetInvoicesAsync(customerName);
            return new OkObjectResult(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // GET /api/invoices/{invoiceNumber} - Get specific invoice
    [Function("GetInvoice")]
    [OpenApiOperation(operationId: "GetInvoice", tags: new[] { "Invoices" }, Summary = "Get invoice by number", Description = "Retrieve a specific invoice by invoice number. Customer name is optional for faster lookup.")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiParameter(name: "customer", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Customer name (optional - if provided, enables faster lookup)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Invoice), Description = "Invoice details")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Invoice not found")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> GetInvoice([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{invoiceNumber}")] HttpRequest req, string invoiceNumber)
    {
        try
        {
            var customerName = req.Query["customer"].FirstOrDefault();
            Invoice? invoice;

            if (!string.IsNullOrEmpty(customerName))
            {
                // Use faster lookup with both customer name and invoice number
                invoice = await _invoiceService.GetInvoiceAsync(customerName, invoiceNumber);
            }
            else
            {
                // Search by invoice number only (slower but more flexible)
                invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
            }

            if (invoice == null)
                return new NotFoundObjectResult("Invoice not found");

            return new OkObjectResult(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // POST /api/invoices - Create new invoice
    [Function("CreateInvoice")]
    [OpenApiOperation(operationId: "CreateInvoice", tags: new[] { "Invoices" }, Summary = "Create new invoice", Description = "Create a new invoice")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Invoice), Required = true, Description = "Invoice data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Invoice), Description = "Created invoice")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid invoice data")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> CreateInvoice([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices")] HttpRequest req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var invoice = JsonSerializer.Deserialize<Invoice>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (invoice == null)
                return new BadRequestObjectResult("Invalid invoice data");

            var createdInvoice = await _invoiceService.CreateInvoiceAsync(invoice);
            return new CreatedResult($"/api/invoices/{createdInvoice.InvoiceNumber}", createdInvoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // PUT /api/invoices/{invoiceNumber} - Update invoice
    [Function("UpdateInvoice")]
    [OpenApiOperation(operationId: "UpdateInvoice", tags: new[] { "Invoices" }, Summary = "Update invoice", Description = "Update an existing invoice")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiParameter(name: "customer", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Customer name")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Invoice), Required = true, Description = "Updated invoice data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Invoice), Description = "Updated invoice")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid invoice data or customer name required")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> UpdateInvoice([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "invoices/{invoiceNumber}")] HttpRequest req, string invoiceNumber)
    {
        try
        {
            var customerName = req.Query["customer"].FirstOrDefault();
            if (string.IsNullOrEmpty(customerName))
                return new BadRequestObjectResult("Customer name is required");

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var invoice = JsonSerializer.Deserialize<Invoice>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (invoice == null)
                return new BadRequestObjectResult("Invalid invoice data");

            invoice.PartitionKey = customerName;
            invoice.RowKey = invoiceNumber;

            var updatedInvoice = await _invoiceService.UpdateInvoiceAsync(invoice);
            return new OkObjectResult(updatedInvoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // DELETE /api/invoices/{invoiceNumber} - Delete invoice
    [Function("DeleteInvoice")]
    [OpenApiOperation(operationId: "DeleteInvoice", tags: new[] { "Invoices" }, Summary = "Delete invoice", Description = "Delete an existing invoice")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiParameter(name: "customer", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Customer name")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Invoice deleted successfully")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Customer name is required")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> DeleteInvoice([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "invoices/{invoiceNumber}")] HttpRequest req, string invoiceNumber)
    {
        try
        {
            var customerName = req.Query["customer"].FirstOrDefault();
            if (string.IsNullOrEmpty(customerName))
                return new BadRequestObjectResult("Customer name is required");

            await _invoiceService.DeleteInvoiceAsync(customerName, invoiceNumber);
            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // GET /api/invoices/{invoiceNumber}/items - Get invoice items
    [Function("GetInvoiceItems")]
    [OpenApiOperation(operationId: "GetInvoiceItems", tags: new[] { "Invoice Items" }, Summary = "Get invoice items", Description = "Retrieve all items for a specific invoice")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(InvoiceItem[]), Description = "List of invoice items")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> GetInvoiceItems([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{invoiceNumber}/items")] HttpRequest req, string invoiceNumber)
    {
        try
        {
            var items = await _invoiceService.GetInvoiceItemsAsync(invoiceNumber);
            return new OkObjectResult(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice items");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // POST /api/invoices/{invoiceNumber}/items - Add invoice item
    [Function("AddInvoiceItem")]
    [OpenApiOperation(operationId: "AddInvoiceItem", tags: new[] { "Invoice Items" }, Summary = "Add invoice item", Description = "Add a new item to an invoice")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(InvoiceItem), Required = true, Description = "Invoice item data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(InvoiceItem), Description = "Created invoice item")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid item data")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> AddInvoiceItem([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{invoiceNumber}/items")] HttpRequest req, string invoiceNumber)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var item = JsonSerializer.Deserialize<InvoiceItem>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (item == null)
                return new BadRequestObjectResult("Invalid item data");

            var createdItem = await _invoiceService.AddInvoiceItemAsync(invoiceNumber, item);
            return new CreatedResult($"/api/invoices/{invoiceNumber}/items/{createdItem.RowKey}", createdItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding invoice item");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // DELETE /api/invoices/{invoiceNumber}/items/{itemId} - Delete invoice item
    [Function("DeleteInvoiceItem")]
    [OpenApiOperation(operationId: "DeleteInvoiceItem", tags: new[] { "Invoice Items" }, Summary = "Delete invoice item", Description = "Delete an item from an invoice")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiParameter(name: "itemId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Item ID")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Item deleted successfully")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> DeleteInvoiceItem([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "invoices/{invoiceNumber}/items/{itemId}")] HttpRequest req, string invoiceNumber, string itemId)
    {
        try
        {
            await _invoiceService.DeleteInvoiceItemAsync(invoiceNumber, itemId);
            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice item");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // GET /api/customers - List all customers
    [Function("GetCustomers")]
    [OpenApiOperation(operationId: "GetCustomers", tags: new[] { "Customers" }, Summary = "Get all customers", Description = "Retrieve all customers")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Customer[]), Description = "List of customers")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> GetCustomers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequest req)
    {
        try
        {
            var customers = await _invoiceService.GetCustomersAsync();
            return new OkObjectResult(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // POST /api/customers - Create new customer
    [Function("CreateCustomer")]
    [OpenApiOperation(operationId: "CreateCustomer", tags: new[] { "Customers" }, Summary = "Create new customer", Description = "Create a new customer")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Customer), Required = true, Description = "Customer data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Customer), Description = "Created customer")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid customer data")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> CreateCustomer([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequest req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonSerializer.Deserialize<Customer>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (customer == null)
                return new BadRequestObjectResult("Invalid customer data");

            var createdCustomer = await _invoiceService.CreateCustomerAsync(customer);
            return new CreatedResult($"/api/customers/{createdCustomer.RowKey}", createdCustomer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // POST /api/invoices/{invoiceNumber}/pdf - Queue PDF generation
    [Function("QueuePdfGeneration")]
    [OpenApiOperation(operationId: "QueuePdfGeneration", tags: new[] { "PDF Generation" }, Summary = "Queue PDF generation", Description = "Queue an invoice for PDF generation by invoice number only")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Invoice number")]
    [OpenApiParameter(name: "customer", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Customer name (optional - if provided, enables faster lookup)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object), Description = "PDF generation queued")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Invoice not found")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> QueuePdfGeneration([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{invoiceNumber}/pdf")] HttpRequest req, string invoiceNumber)
    {
        try
        {
            var customerName = req.Query["customer"].FirstOrDefault();

            if (!string.IsNullOrEmpty(customerName))
            {
                // Use faster lookup with both customer name and invoice number
                await _invoiceService.QueueInvoiceForPdfGenerationAsync(customerName, invoiceNumber);
            }
            else
            {
                // Search by invoice number only and queue for PDF generation
                var success = await _invoiceService.QueueInvoiceForPdfGenerationByNumberAsync(invoiceNumber);
                if (!success)
                {
                    return new NotFoundObjectResult("Invoice not found");
                }
            }

            return new AcceptedResult($"/api/invoices/{invoiceNumber}/pdf", new { Message = "PDF generation queued", InvoiceNumber = invoiceNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing PDF generation");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }


}
