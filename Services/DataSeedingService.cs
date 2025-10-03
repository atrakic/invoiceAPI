using Azure.Data.Tables;

using invoiceAPI.Models;

using Microsoft.Extensions.Logging;

namespace invoiceAPI.Services;

/// <summary>
/// Service responsible for seeding development data when using local storage emulator
/// </summary>
public class DataSeedingService
{
    private readonly ILogger<DataSeedingService> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public DataSeedingService(ILogger<DataSeedingService> logger, TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _tableServiceClient = tableServiceClient;
    }

    /// <summary>
    /// Seeds development data including customers, invoices, and invoice items
    /// </summary>
    public async Task SeedDevelopmentDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting development data seeding...");

            await SeedCustomersAsync();
            await SeedInvoicesAsync();
            await SeedInvoiceItemsAsync();

            _logger.LogInformation("🎉 Development data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not seed development data: {Message}", ex.Message);
        }
    }

    private async Task SeedCustomersAsync()
    {
        var customersTableClient = _tableServiceClient.GetTableClient("customers");
        var customerEntities = new[]
        {
            new Customer
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                Name = "John Doe",
                Email = "john@example.com",
                Phone = "+1234567890",
                Address = "123 Main St, New York, NY 10001",
                City = "New York",
                PostalCode = "10001",
                Country = "USA"
            },
            new Customer
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                Name = "Jane Smith",
                Email = "jane@example.com",
                Phone = "+1987654321",
                Address = "456 Oak Ave, Los Angeles, CA 90210",
                City = "Los Angeles",
                PostalCode = "90210",
                Country = "USA"
            },
            new Customer
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                Name = "Bob Johnson",
                Email = "bob@example.com",
                Phone = "+1555123456",
                Address = "789 Pine Rd, Chicago, IL 60601",
                City = "Chicago",
                PostalCode = "60601",
                Country = "USA"
            }
        };

        foreach (var customer in customerEntities)
        {
            await customersTableClient.UpsertEntityAsync(customer);
        }

        _logger.LogInformation("✅ Seeded {Count} customers", customerEntities.Length);
    }

    private async Task SeedInvoicesAsync()
    {
        var invoicesTableClient = _tableServiceClient.GetTableClient("invoices");
        var invoiceEntities = new[]
        {
            new Invoice
            {
                PartitionKey = "John Doe",
                RowKey = "INV-001",
                InvoiceNumber = "INV-001",
                CustomerName = "John Doe",
                CustomerEmail = "john@example.com",
                CustomerAddress = "123 Main St, New York, NY 10001",
                Description = "Web Development Services",
                Status = "Sent",
                TotalAmount = 2500.00m,
                InvoiceDate = DateTime.UtcNow.AddDays(-10),
                DueDate = DateTime.UtcNow.AddDays(20)
            },
            new Invoice
            {
                PartitionKey = "Jane Smith",
                RowKey = "INV-002",
                InvoiceNumber = "INV-002",
                CustomerName = "Jane Smith",
                CustomerEmail = "jane@example.com",
                CustomerAddress = "456 Oak Ave, Los Angeles, CA 90210",
                Description = "Mobile App Development",
                Status = "Draft",
                TotalAmount = 4800.00m,
                InvoiceDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(25)
            },
            new Invoice
            {
                PartitionKey = "Bob Johnson",
                RowKey = "INV-003",
                InvoiceNumber = "INV-003",
                CustomerName = "Bob Johnson",
                CustomerEmail = "bob@example.com",
                CustomerAddress = "789 Pine Rd, Chicago, IL 60601",
                Description = "Consulting Services",
                Status = "Paid",
                TotalAmount = 1800.00m,
                InvoiceDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(-5)
            },
            new Invoice
            {
                PartitionKey = "John Doe",
                RowKey = "INV-004",
                InvoiceNumber = "INV-004",
                CustomerName = "John Doe",
                CustomerEmail = "john@example.com",
                CustomerAddress = "123 Main St, New York, NY 10001",
                Description = "Database Design & Implementation",
                Status = "Overdue",
                TotalAmount = 3200.00m,
                InvoiceDate = DateTime.UtcNow.AddDays(-45),
                DueDate = DateTime.UtcNow.AddDays(-15)
            }
        };

        foreach (var invoice in invoiceEntities)
        {
            await invoicesTableClient.UpsertEntityAsync(invoice);
        }

        _logger.LogInformation("✅ Seeded {Count} invoices", invoiceEntities.Length);
    }

    private async Task SeedInvoiceItemsAsync()
    {
        var invoiceItemsTableClient = _tableServiceClient.GetTableClient("invoiceitems");
        var invoiceItemEntities = new[]
        {
            // Items for INV-001 (Web Development Services)
            new InvoiceItem
            {
                PartitionKey = "INV-001",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Frontend Development",
                Quantity = 80,
                UnitPrice = 25.00m,
                TotalPrice = 2000.00m
            },
            new InvoiceItem
            {
                PartitionKey = "INV-001",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Backend API Development",
                Quantity = 20,
                UnitPrice = 25.00m,
                TotalPrice = 500.00m
            },
            // Items for INV-002 (Mobile App Development)
            new InvoiceItem
            {
                PartitionKey = "INV-002",
                RowKey = Guid.NewGuid().ToString(),
                Description = "iOS App Development",
                Quantity = 60,
                UnitPrice = 40.00m,
                TotalPrice = 2400.00m
            },
            new InvoiceItem
            {
                PartitionKey = "INV-002",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Android App Development",
                Quantity = 60,
                UnitPrice = 40.00m,
                TotalPrice = 2400.00m
            },
            // Items for INV-003 (Consulting Services)
            new InvoiceItem
            {
                PartitionKey = "INV-003",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Architecture Consultation",
                Quantity = 12,
                UnitPrice = 100.00m,
                TotalPrice = 1200.00m
            },
            new InvoiceItem
            {
                PartitionKey = "INV-003",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Code Review Sessions",
                Quantity = 6,
                UnitPrice = 100.00m,
                TotalPrice = 600.00m
            },
            // Items for INV-004 (Database Design & Implementation)
            new InvoiceItem
            {
                PartitionKey = "INV-004",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Database Schema Design",
                Quantity = 24,
                UnitPrice = 75.00m,
                TotalPrice = 1800.00m
            },
            new InvoiceItem
            {
                PartitionKey = "INV-004",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Data Migration Scripts",
                Quantity = 16,
                UnitPrice = 75.00m,
                TotalPrice = 1200.00m
            },
            new InvoiceItem
            {
                PartitionKey = "INV-004",
                RowKey = Guid.NewGuid().ToString(),
                Description = "Performance Optimization",
                Quantity = 8,
                UnitPrice = 75.00m,
                TotalPrice = 600.00m
            }
        };

        foreach (var invoiceItem in invoiceItemEntities)
        {
            await invoiceItemsTableClient.UpsertEntityAsync(invoiceItem);
        }

        _logger.LogInformation("✅ Seeded {Count} invoice items", invoiceItemEntities.Length);
    }
}
