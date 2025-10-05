#!/usr/bin/env dotnet fsi

// Simple F# script to test the Invoice API models
// Works both locally and in CI/CD pipelines with relative paths
#r "bin/Debug/net8.0/invoiceAPI.dll"

open invoiceAPI.Models
open System.IO

// Validate that we can find the assembly
let assemblyPath = "bin/Debug/net8.0/invoiceAPI.dll"
if not (File.Exists(assemblyPath)) then
    printfn "❌ Error: Could not find %s" assemblyPath
    printfn "   Make sure to run 'dotnet build' first"
    exit 1

printfn "Running Invoice API Basic Tests..."

// Test Customer Creation
let customer = Customer(
    Name = "John Doe",
    Email = "john@example.com",
    Phone = "+1234567890",
    Address = "123 Main St",
    City = "New York",
    PostalCode = "10001",
    Country = "USA"
)

assert (customer.Name = "John Doe")
assert (customer.Email = "john@example.com")
assert (customer.PartitionKey = "Customer")
printfn "✓ Customer creation test passed"

// Test Invoice Creation
let invoice = Invoice(
    InvoiceNumber = "INV-001",
    CustomerName = "John Doe",
    CustomerEmail = "john@example.com",
    Description = "Web Development Services",
    Status = "Draft",
    TotalAmount = 2500.00m
)

assert (invoice.InvoiceNumber = "INV-001")
assert (invoice.Status = "Draft")
assert (invoice.TotalAmount = 2500.00m)
printfn "✓ Invoice creation test passed"

// Test InvoiceItem Creation
let item = InvoiceItem(
    Description = "Frontend Development",
    Quantity = 50,
    UnitPrice = 50.00m,
    TotalPrice = 2500.00m
)

assert (item.Description = "Frontend Development")
assert (item.Quantity = 50)
assert (item.UnitPrice = 50.00m)
printfn "✓ Invoice item creation test passed"

// Test calculation
let calculatedTotal = decimal item.Quantity * item.UnitPrice
assert (calculatedTotal = 2500.00m)
printfn "✓ Invoice item calculation test passed"

// Test defaults
let defaultCustomer = Customer()
assert (defaultCustomer.PartitionKey = "Customer")
printfn "✓ Customer defaults test passed"

let defaultInvoice = Invoice()
assert (defaultInvoice.Status = "Draft")
printfn "✓ Invoice defaults test passed"

let defaultItem = InvoiceItem()
assert (defaultItem.Quantity = 1)
printfn "✓ Invoice item defaults test passed"

printfn "All tests completed successfully! ✅"
