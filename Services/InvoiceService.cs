using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using invoiceAPI.Models;

namespace invoiceAPI.Services;

public class InvoiceService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(TableServiceClient tableServiceClient, QueueServiceClient queueServiceClient, ILogger<InvoiceService> logger)
    {
        _tableServiceClient = tableServiceClient;
        _queueServiceClient = queueServiceClient;
        _logger = logger;
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoices");
        await tableClient.CreateIfNotExistsAsync();

        // Generate invoice number if not provided
        if (string.IsNullOrEmpty(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        // Set IDs
        invoice.PartitionKey = invoice.CustomerName; // Group by customer
        invoice.RowKey = invoice.InvoiceNumber;
        invoice.CreatedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await tableClient.UpsertEntityAsync(invoice);
        _logger.LogInformation($"Created invoice: {invoice.InvoiceNumber}");

        return invoice;
    }

    public async Task<Invoice?> GetInvoiceAsync(string customerName, string invoiceNumber)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoices");

        try
        {
            var response = await tableClient.GetEntityAsync<Invoice>(customerName, invoiceNumber);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoices");

        try
        {
            var filter = $"InvoiceNumber eq '{invoiceNumber}'";

            await foreach (var invoice in tableClient.QueryAsync<Invoice>(filter))
            {
                return invoice; // Return the first (and should be only) match
            }

            return null; // No invoice found with this number
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving invoice by number: {invoiceNumber}");
            return null;
        }
    }

    public async Task<List<Invoice>> GetInvoicesAsync(string? customerName = null)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoices");
        var invoices = new List<Invoice>();

        var filter = string.IsNullOrEmpty(customerName) ? null : $"PartitionKey eq '{customerName}'";

        await foreach (var invoice in tableClient.QueryAsync<Invoice>(filter))
        {
            invoices.Add(invoice);
        }

        return invoices.OrderByDescending(i => i.CreatedAt).ToList();
    }

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoices");
        invoice.UpdatedAt = DateTime.UtcNow;

        await tableClient.UpsertEntityAsync(invoice);
        _logger.LogInformation($"Updated invoice: {invoice.InvoiceNumber}");

        return invoice;
    }

    public async Task DeleteInvoiceAsync(string customerName, string invoiceNumber)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoices");
        await tableClient.DeleteEntityAsync(customerName, invoiceNumber);
        _logger.LogInformation($"Deleted invoice: {invoiceNumber}");
    }

    public async Task<List<InvoiceItem>> GetInvoiceItemsAsync(string invoiceNumber)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoiceitems");
        var items = new List<InvoiceItem>();

        var filter = $"PartitionKey eq '{invoiceNumber}'";

        await foreach (var item in tableClient.QueryAsync<InvoiceItem>(filter))
        {
            items.Add(item);
        }

        return items.OrderBy(i => i.CreatedAt).ToList();
    }

    public async Task<InvoiceItem> AddInvoiceItemAsync(string invoiceNumber, InvoiceItem item)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoiceitems");
        await tableClient.CreateIfNotExistsAsync();

        item.PartitionKey = invoiceNumber;
        item.RowKey = Guid.NewGuid().ToString();
        item.TotalPrice = item.Quantity * item.UnitPrice;
        item.CreatedAt = DateTime.UtcNow;

        await tableClient.UpsertEntityAsync(item);
        _logger.LogInformation($"Added item to invoice {invoiceNumber}: {item.Description}");

        // Recalculate invoice total
        await RecalculateInvoiceTotalAsync(invoiceNumber);

        return item;
    }

    public async Task DeleteInvoiceItemAsync(string invoiceNumber, string itemId)
    {
        var tableClient = _tableServiceClient.GetTableClient("invoiceitems");
        await tableClient.DeleteEntityAsync(invoiceNumber, itemId);
        _logger.LogInformation($"Deleted item {itemId} from invoice {invoiceNumber}");

        // Recalculate invoice total
        await RecalculateInvoiceTotalAsync(invoiceNumber);
    }

    private async Task RecalculateInvoiceTotalAsync(string invoiceNumber)
    {
        var items = await GetInvoiceItemsAsync(invoiceNumber);
        var total = items.Sum(i => i.TotalPrice);

        // Find the invoice to update
        var invoicesTableClient = _tableServiceClient.GetTableClient("invoices");

        await foreach (var invoice in invoicesTableClient.QueryAsync<Invoice>($"RowKey eq '{invoiceNumber}'"))
        {
            invoice.TotalAmount = total;
            invoice.UpdatedAt = DateTime.UtcNow;
            await invoicesTableClient.UpsertEntityAsync(invoice);
            break;
        }
    }

    public async Task QueueInvoiceForPdfGenerationAsync(string customerName, string invoiceNumber)
    {
        var queueClient = _queueServiceClient.GetQueueClient("pdf-generation");
        await queueClient.CreateIfNotExistsAsync();

        var message = new { CustomerName = customerName, InvoiceNumber = invoiceNumber };
        var messageText = JsonSerializer.Serialize(message);

        await queueClient.SendMessageAsync(messageText);
        _logger.LogInformation($"Queued invoice {invoiceNumber} for PDF generation");
    }

    public async Task<bool> QueueInvoiceForPdfGenerationByNumberAsync(string invoiceNumber)
    {
        // First, find the invoice to get the customer name
        var invoice = await GetInvoiceByNumberAsync(invoiceNumber);
        if (invoice == null)
        {
            _logger.LogWarning($"Invoice {invoiceNumber} not found for PDF generation");
            return false;
        }

        var queueClient = _queueServiceClient.GetQueueClient("pdf-generation");
        await queueClient.CreateIfNotExistsAsync();

        var message = new { CustomerName = invoice.CustomerName, InvoiceNumber = invoiceNumber };
        var messageText = JsonSerializer.Serialize(message);

        await queueClient.SendMessageAsync(messageText);
        _logger.LogInformation($"Queued invoice {invoiceNumber} for PDF generation (customer: {invoice.CustomerName})");

        return true;
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        var tableClient = _tableServiceClient.GetTableClient("customers");
        await tableClient.CreateIfNotExistsAsync();

        customer.PartitionKey = "Customer";
        customer.RowKey = Guid.NewGuid().ToString();
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;

        await tableClient.UpsertEntityAsync(customer);
        _logger.LogInformation($"Created customer: {customer.Name}");

        return customer;
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        var tableClient = _tableServiceClient.GetTableClient("customers");
        var customers = new List<Customer>();

        await foreach (var customer in tableClient.QueryAsync<Customer>())
        {
            customers.Add(customer);
        }

        return customers.OrderBy(c => c.Name).ToList();
    }
}
