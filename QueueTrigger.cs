using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using invoiceAPI.Services;
using invoiceAPI.Models;

namespace invoiceAPI;

public class QueueTrigger
{
    private readonly ILogger<QueueTrigger> _logger;
    private readonly InvoiceService _invoiceService;
    private readonly PdfGenerationService _pdfGenerationService;

    public QueueTrigger(ILogger<QueueTrigger> logger, InvoiceService invoiceService, PdfGenerationService pdfGenerationService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
        _pdfGenerationService = pdfGenerationService;
    }

    [Function("PdfGenerationQueueTrigger")]
    public async Task ProcessPdfGeneration([QueueTrigger("pdf-generation", Connection = "")] QueueMessage message)
    {
        _logger.LogInformation("Processing PDF generation request: {messageText}", message.MessageText);

        try
        {
            var request = JsonSerializer.Deserialize<PdfGenerationRequest>(message.MessageText);
            if (request == null)
            {
                _logger.LogError("Failed to deserialize PDF generation request");
                return;
            }

            await GenerateInvoicePdf(request.CustomerName, request.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF generation request");
            throw; // This will cause the message to be retried or moved to poison queue
        }
    }

    private async Task GenerateInvoicePdf(string customerName, string invoiceNumber)
    {
        _logger.LogInformation($"Generating PDF for invoice {invoiceNumber} for customer {customerName}");

        // Get invoice data - try specific customer first, then search by invoice number
        Invoice? invoice = null;

        if (!string.IsNullOrEmpty(customerName))
        {
            invoice = await _invoiceService.GetInvoiceAsync(customerName, invoiceNumber);
        }

        // If not found with customer name, try searching by invoice number only
        if (invoice == null)
        {
            _logger.LogInformation($"Invoice not found with customer name, searching by invoice number only: {invoiceNumber}");
            invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
        }

        if (invoice == null)
        {
            _logger.LogError($"Invoice {invoiceNumber} not found");
            return;
        }

        var items = await _invoiceService.GetInvoiceItemsAsync(invoiceNumber);

        // Generate actual PDF using the PDF generation service
        var pdfFileName = await _pdfGenerationService.GenerateInvoicePdfAsync(invoice, items);

        _logger.LogInformation($"PDF generated and stored as {pdfFileName}");
    }


}

public class PdfGenerationRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
}
