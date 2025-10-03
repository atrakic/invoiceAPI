using Azure;
using Azure.Data.Tables;

namespace invoiceAPI.Models;

public class Invoice : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // Customer ID
    public string RowKey { get; set; } = string.Empty; // Invoice ID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Invoice properties
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Paid, Overdue
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
