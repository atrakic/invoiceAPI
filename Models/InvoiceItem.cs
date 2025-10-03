using Azure;
using Azure.Data.Tables;

namespace invoiceAPI.Models;

public class InvoiceItem : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // Invoice ID
    public string RowKey { get; set; } = string.Empty; // Item ID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Invoice item properties
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
