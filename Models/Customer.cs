using Azure;
using Azure.Data.Tables;

namespace invoiceAPI.Models;

public class Customer : ITableEntity
{
    public string PartitionKey { get; set; } = "Customer"; // Always "Customer" for all customers
    public string RowKey { get; set; } = string.Empty; // Customer ID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Customer properties
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
