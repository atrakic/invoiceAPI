using invoiceAPI.Models;

namespace invoiceAPI.Tests;

/// <summary>
/// Basic integration tests for the Invoice API models and core functionality
/// This is a simplified test approach using console assertions
/// </summary>
public class BasicTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("Running Invoice API Basic Tests...");

        TestCustomerCreation();
        TestInvoiceCreation();
        TestInvoiceItemCreation();
        TestInvoiceItemCalculation();
        TestInvoiceStatusValues();
        TestCustomerDefaults();
        TestInvoiceDefaults();
        TestInvoiceItemDefaults();
        TestMultipleItemsCalculation();
        TestCustomerEmailValidation();
        TestInvoiceDateLogic();
        TestInvoiceNumberFormat();

        Console.WriteLine("All tests completed successfully! ✅");
    }

    private static void TestCustomerCreation()
    {
        var customer = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "+1234567890",
            Address = "123 Main St",
            City = "New York",
            PostalCode = "10001",
            Country = "USA"
        };

        Assert(customer.Name == "John Doe", "Customer name should match");
        Assert(customer.Email == "john@example.com", "Customer email should match");
        Assert(customer.Phone == "+1234567890", "Customer phone should match");
        Assert(customer.Address == "123 Main St", "Customer address should match");
        Assert(customer.City == "New York", "Customer city should match");
        Assert(customer.PostalCode == "10001", "Customer postal code should match");
        Assert(customer.Country == "USA", "Customer country should match");
        Assert(customer.PartitionKey == "Customer", "Customer partition key should be 'Customer'");
        Console.WriteLine("✓ Customer creation test passed");
    }

    private static void TestInvoiceCreation()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            CustomerName = "John Doe",
            CustomerEmail = "john@example.com",
            CustomerAddress = "123 Main St, New York, NY",
            Description = "Web Development Services",
            Status = "Draft",
            TotalAmount = 2500.00m
        };

        Assert(invoice.InvoiceNumber == "INV-001", "Invoice number should match");
        Assert(invoice.CustomerName == "John Doe", "Customer name should match");
        Assert(invoice.CustomerEmail == "john@example.com", "Customer email should match");
        Assert(invoice.CustomerAddress == "123 Main St, New York, NY", "Customer address should match");
        Assert(invoice.Description == "Web Development Services", "Description should match");
        Assert(invoice.Status == "Draft", "Status should match");
        Assert(invoice.TotalAmount == 2500.00m, "Total amount should match");
        Console.WriteLine("✓ Invoice creation test passed");
    }

    private static void TestInvoiceItemCreation()
    {
        var item = new InvoiceItem
        {
            Description = "Frontend Development",
            Quantity = 50,
            UnitPrice = 50.00m,
            TotalPrice = 2500.00m
        };

        Assert(item.Description == "Frontend Development", "Item description should match");
        Assert(item.Quantity == 50, "Item quantity should match");
        Assert(item.UnitPrice == 50.00m, "Unit price should match");
        Assert(item.TotalPrice == 2500.00m, "Total price should match");
        Console.WriteLine("✓ Invoice item creation test passed");
    }

    private static void TestInvoiceItemCalculation()
    {
        var item = new InvoiceItem
        {
            Description = "Consulting Hours",
            Quantity = 10,
            UnitPrice = 150.00m
        };

        var calculatedTotal = item.Quantity * item.UnitPrice;
        Assert(calculatedTotal == 1500.00m, "Calculated total should be correct");
        Console.WriteLine("✓ Invoice item calculation test passed");
    }

    private static void TestInvoiceStatusValues()
    {
        var statuses = new[] { "Draft", "Sent", "Paid", "Overdue" };

        foreach (var status in statuses)
        {
            var invoice = new Invoice { Status = status };
            Assert(invoice.Status == status, $"Status '{status}' should be accepted");
        }
        Console.WriteLine("✓ Invoice status values test passed");
    }

    private static void TestCustomerDefaults()
    {
        var customer = new Customer();
        Assert(customer.PartitionKey == "Customer", "Customer should have default partition key");
        Console.WriteLine("✓ Customer defaults test passed");
    }

    private static void TestInvoiceDefaults()
    {
        var invoice = new Invoice();
        Assert(invoice.Status == "Draft", "Invoice should have default status 'Draft'");
        Console.WriteLine("✓ Invoice defaults test passed");
    }

    private static void TestInvoiceItemDefaults()
    {
        var item = new InvoiceItem();
        Assert(item.Quantity == 1, "Invoice item should have default quantity of 1");
        Console.WriteLine("✓ Invoice item defaults test passed");
    }

    private static void TestMultipleItemsCalculation()
    {
        var items = new List<InvoiceItem>
        {
            new() { Description = "Item 1", Quantity = 2, UnitPrice = 100.00m },
            new() { Description = "Item 2", Quantity = 1, UnitPrice = 50.00m },
            new() { Description = "Item 3", Quantity = 3, UnitPrice = 75.00m }
        };

        var totalAmount = items.Sum(item => item.Quantity * item.UnitPrice);
        Assert(totalAmount == 475.00m, "Multiple items total should be calculated correctly");
        Console.WriteLine("✓ Multiple items calculation test passed");
    }

    private static void TestCustomerEmailValidation()
    {
        var customer = new Customer { Email = "test@example.com" };
        Assert(customer.Email.Contains("@"), "Customer email should contain @ symbol");
        Console.WriteLine("✓ Customer email validation test passed");
    }

    private static void TestInvoiceDateLogic()
    {
        var invoiceDate = DateTime.UtcNow;
        var dueDate = invoiceDate.AddDays(30);

        var invoice = new Invoice
        {
            InvoiceDate = invoiceDate,
            DueDate = dueDate
        };

        Assert(invoice.DueDate > invoice.InvoiceDate, "Due date should be after invoice date");
        Console.WriteLine("✓ Invoice date logic test passed");
    }

    private static void TestInvoiceNumberFormat()
    {
        var expectedFormat = "INV-001";
        var invoice = new Invoice { InvoiceNumber = expectedFormat };

        Assert(invoice.InvoiceNumber.StartsWith("INV-"), "Invoice number should start with 'INV-'");
        Assert(invoice.InvoiceNumber.Length == 7, "Invoice number should be 7 characters long");
        Console.WriteLine("✓ Invoice number format test passed");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Test failed: {message}");
        }
    }
}
