# Invoice API

[![.NET](https://github.com/atrakic/invoiceAPI/actions/workflows/dotnet.yml/badge.svg)](https://github.com/atrakic/invoiceAPI/actions/workflows/dotnet.yml)

> A minimal Azure Functions-based invoice management API built with C#/.NET 8. This application provides RESTful endpoints for managing invoices, customers, and PDF generation with Azure Storage integration.

## Features

- 📋 **Invoice Management**: Create, read, update, and delete invoices
- 👥 **Customer Management**: Manage customer information
- 📄 **PDF Generation**: Automatic PDF invoice generation with professional templates
- 🔄 **Background Processing**: Queue-based PDF generation for better performance
- 💾 **Azure Storage Integration**: Uses Tables, Blobs, and Queues for data persistence
- 📚 **OpenAPI Documentation**: Built-in Swagger UI for API exploration
- 🌱 **Sample Data Seeding**: Quick setup with realistic test data

## Architecture

- **Azure Functions**: Serverless HTTP triggers for API endpoints
- **Azure Table Storage**: Customer, invoice, and invoice item data
- **Azure Blob Storage**: PDF file storage
- **Azure Queue Storage**: Background PDF generation processing
- **PdfSharpCore**: Professional PDF generation with custom templates

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Docker](https://www.docker.com/) (for Azurite storage emulator)

### 1. Start Storage Emulator

```bash
# Start Azurite (Azure Storage Emulator)
docker compose up -d
```

### 2. Build and Run

```bash
# Build the project
dotnet build

# Start the Functions host
func host start
```

The API will be available at `http://localhost:7071`

**Note**: When using development storage (Azurite), sample data is automatically seeded on startup, including customers, invoices, and invoice items.

### 3. Explore the API

Visit `http://localhost:7071/api/swagger/ui` for interactive API documentation.

## API Endpoints

### Core Entities

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/customers` | List all customers |
| `POST` | `/api/customers` | Create a new customer |
| `GET` | `/api/invoices` | List all invoices |
| `GET` | `/api/invoices/{number}` | Get specific invoice |
| `POST` | `/api/invoices` | Create a new invoice |
| `PUT` | `/api/invoices/{number}` | Update an invoice |
| `DELETE` | `/api/invoices/{number}` | Delete an invoice |

### Invoice Items

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/invoices/{number}/items` | Get invoice line items |
| `POST` | `/api/invoices/{number}/items` | Add item to invoice |
| `DELETE` | `/api/invoices/{number}/items/{itemId}` | Remove invoice item |

### PDF Generation

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/invoices/{number}/pdf` | Queue PDF generation |
| `GET` | `/api/pdfs` | List generated PDFs |
| `GET` | `/api/pdfs/{fileName}/view` | View PDF in browser |
| `GET` | `/api/pdfs/{fileName}/download` | Download PDF file |

### Utilities

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/HttpAPI` | List all storage entities |
| `GET` | `/api/HttpAPI?action=tables` | List storage tables |

## Usage Examples

### Create a Customer

```bash
curl -X POST "http://localhost:7071/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corp",
    "email": "billing@acme.com",
    "phone": "+1-555-0123",
    "address": "123 Business St",
    "city": "New York",
    "postalCode": "10001",
    "country": "USA"
  }'
```

### Create an Invoice

```bash
curl -X POST "http://localhost:7071/api/invoices" \
  -H "Content-Type: application/json" \
  -d '{
    "invoiceNumber": "INV-2025-001",
    "customerName": "Acme Corp",
    "customerEmail": "billing@acme.com",
    "customerAddress": "123 Business St, New York, NY 10001",
    "description": "Web Development Services",
    "status": "Draft",
    "totalAmount": 2500.00,
    "dueDate": "2025-11-15T00:00:00Z"
  }'
```

### Add Invoice Items

```bash
curl -X POST "http://localhost:7071/api/invoices/INV-2025-001/items" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Frontend Development",
    "quantity": 50,
    "unitPrice": 50.00
  }'
```

### Generate PDF

```bash
# Queue PDF generation (asynchronous)
curl -X POST "http://localhost:7071/api/invoices/INV-2025-001/pdf"

# Check available PDFs
curl "http://localhost:7071/api/pdfs"

# View PDF in browser
open "http://localhost:7071/api/pdfs/INV-2025-001.pdf/view"
```

## Data Models

### Invoice
```json
{
  "invoiceNumber": "INV-001",
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "customerAddress": "123 Main St, New York, NY",
  "description": "Services rendered",
  "status": "Sent", // Draft, Sent, Paid, Overdue
  "totalAmount": 1500.00,
  "invoiceDate": "2025-10-01T00:00:00Z",
  "dueDate": "2025-10-31T00:00:00Z"
}
```

### Customer
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "address": "123 Main St",
  "city": "New York",
  "postalCode": "10001",
  "country": "USA"
}
```

### Invoice Item
```json
{
  "description": "Consulting Hours",
  "quantity": 10,
  "unitPrice": 150.00,
  "totalPrice": 1500.00
}
```

## Storage Structure

### Azure Tables
- **`customers`**: Customer records (PartitionKey: "Customer", RowKey: CustomerId)
- **`invoices`**: Invoice records (PartitionKey: CustomerName, RowKey: InvoiceNumber)
- **`invoiceitems`**: Line items (PartitionKey: InvoiceNumber, RowKey: ItemId)

### Azure Blobs
- **`invoices`**: General invoice files
- **`invoice-pdfs`**: Generated PDF invoices

### Azure Queues
- **`pdf-generation`**: Background PDF generation requests

## Development

### Build Commands
```bash
# Clean build
make clean

# Build project
make build

# Run locally
make run
```

### Configuration

Edit `local.settings.json` for local development:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

## PDF Generation

The application includes a sophisticated PDF generation system:

- **Template-based**: Professional invoice layout with company branding
- **Asynchronous**: Uses queue processing to avoid HTTP timeouts
- **Storage integration**: PDFs automatically stored in blob storage
- **Multiple formats**: View in browser or download as file

## Testing

The project includes comprehensive tests for the core models and business logic.

### Run Tests

```bash
# Run basic model tests using F# Interactive
dotnet fsi test_runner.fsx

# Expected output:
# Running Invoice API Basic Tests...
# ✓ Customer creation test passed
# ✓ Invoice creation test passed
# ✓ Invoice item creation test passed
# ✓ Invoice item calculation test passed
# ✓ Customer defaults test passed
# ✓ Invoice defaults test passed
# ✓ Invoice item defaults test passed
# All tests completed successfully! ✅
```

### Test Coverage

The test suite covers:
- ✅ Model instantiation and property assignment
- ✅ Default value behavior
- ✅ Business logic calculations
- ✅ Data validation rules
- ✅ Multiple record operations

### Test Results

See `TEST_RESULTS.md` for detailed test reports including:
- Test execution summary
- Individual test details
- Coverage analysis
- Recommendations for future testing

### Runtime Testing

When the API is running, you can also test via HTTP endpoint:

```bash
curl "http://localhost:7071/api/tests"
```

## Error Handling

The API includes comprehensive error handling:
- Validation errors return `400 Bad Request`
- Missing resources return `404 Not Found`
- Server errors return `500 Internal Server Error`
- All errors include descriptive messages

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable (see Testing section)
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
