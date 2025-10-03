using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using invoiceAPI.Models;
using System.Text;

namespace invoiceAPI.Services;

public class PdfGenerationService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<PdfGenerationService> _logger;
    private const string PDF_CONTAINER_NAME = "invoice-pdfs";

    public PdfGenerationService(BlobServiceClient blobServiceClient, ILogger<PdfGenerationService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> GenerateInvoicePdfAsync(Invoice invoice, List<InvoiceItem> items)
    {
        _logger.LogInformation($"Generating PDF for invoice {invoice.InvoiceNumber}");

        try
        {
            // Create PDF document
            var document = new PdfDocument();
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);

            // Generate PDF content using template
            DrawInvoiceTemplate(graphics, page, invoice, items);

            // Save PDF to memory stream
            using var stream = new MemoryStream();
            document.Save(stream);
            document.Close();

            // Upload to blob storage
            var blobName = $"{invoice.InvoiceNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var blobClient = await GetBlobClientAsync(blobName);

            stream.Position = 0;
            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation($"PDF generated and stored: {blobName}");
            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating PDF for invoice {invoice.InvoiceNumber}");
            throw;
        }
    }

    private void DrawInvoiceTemplate(XGraphics graphics, PdfPage page, Invoice invoice, List<InvoiceItem> items)
    {
        // Define fonts
        var titleFont = new XFont("Arial", 24, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
        var normalFont = new XFont("Arial", 11, XFontStyle.Regular);
        var smallFont = new XFont("Arial", 9, XFontStyle.Regular);

        // Define colors
        var blackBrush = XBrushes.Black;
        var grayBrush = XBrushes.Gray;
        var lightGrayPen = new XPen(XColors.LightGray, 1);

        // Page dimensions
        var pageWidth = page.Width.Point;
        var pageHeight = page.Height.Point;
        var margin = 50;
        var contentWidth = pageWidth - (2 * margin);

        var yPosition = margin;

        // Header - Company Info & Invoice Title
        graphics.DrawString("INVOICE", titleFont, blackBrush, margin, yPosition);
        graphics.DrawString($"#{invoice.InvoiceNumber}", headerFont, blackBrush, (int)(pageWidth - margin - 150), yPosition);
        yPosition += 40;

        // Invoice Info Box
        var infoBoxY = yPosition;
        graphics.DrawRectangle(lightGrayPen, XBrushes.LightGray, margin, infoBoxY, (int)contentWidth, 80);

        // Left side - Invoice details
        yPosition += 20;
        graphics.DrawString($"Invoice Date: {invoice.InvoiceDate:yyyy-MM-dd}", normalFont, blackBrush, margin + 10, yPosition);
        yPosition += 20;
        graphics.DrawString($"Due Date: {invoice.DueDate:yyyy-MM-dd}", normalFont, blackBrush, margin + 10, yPosition);
        yPosition += 20;
        graphics.DrawString($"Status: {invoice.Status}", normalFont, blackBrush, margin + 10, yPosition);

        // Right side - Customer info
        yPosition = infoBoxY + 20;
        var rightColumn = pageWidth - margin - 200;
        graphics.DrawString("Bill To:", headerFont, blackBrush, (int)rightColumn, yPosition);
        yPosition += 20;
        graphics.DrawString(invoice.CustomerName, normalFont, blackBrush, (int)rightColumn, yPosition);
        yPosition += 15;
        graphics.DrawString(invoice.CustomerEmail, normalFont, blackBrush, (int)rightColumn, yPosition);
        yPosition += 15;

        // Handle multi-line address
        var addressLines = SplitAddress(invoice.CustomerAddress, 30);
        foreach (var line in addressLines)
        {
            graphics.DrawString(line, normalFont, blackBrush, (int)rightColumn, yPosition);
            yPosition += 15;
        }

        yPosition = infoBoxY + 100;

        // Description section (if exists)
        if (!string.IsNullOrEmpty(invoice.Description))
        {
            yPosition += 20;
            graphics.DrawString("Description:", headerFont, blackBrush, margin, yPosition);
            yPosition += 20;
            graphics.DrawString(invoice.Description, normalFont, blackBrush, margin, yPosition);
            yPosition += 30;
        }

        // Items table
        yPosition += 20;
        var tableY = yPosition;
        var rowHeight = 25;
        var colWidths = new[] { 200, 80, 100, 100 }; // Description, Qty, Unit Price, Total
        var colPositions = new double[4];
        colPositions[0] = margin;
        for (int i = 1; i < 4; i++)
        {
            colPositions[i] = colPositions[i - 1] + colWidths[i - 1];
        }

        // Table header
        graphics.DrawRectangle(lightGrayPen, XBrushes.LightGray, margin, tableY, (int)contentWidth, (int)rowHeight);
        graphics.DrawString("Description", headerFont, blackBrush, (int)(colPositions[0] + 5), tableY + 15);
        graphics.DrawString("Qty", headerFont, blackBrush, (int)(colPositions[1] + 5), tableY + 15);
        graphics.DrawString("Unit Price", headerFont, blackBrush, (int)(colPositions[2] + 5), tableY + 15);
        graphics.DrawString("Total", headerFont, blackBrush, (int)(colPositions[3] + 5), tableY + 15);

        yPosition += rowHeight;

        // Table rows
        foreach (var item in items)
        {
            // Alternate row background
            if ((items.IndexOf(item) % 2) == 0)
            {
                graphics.DrawRectangle(XBrushes.WhiteSmoke, margin, yPosition, (int)contentWidth, (int)rowHeight);
            }

            graphics.DrawRectangle(lightGrayPen, margin, yPosition, (int)contentWidth, (int)rowHeight);

            graphics.DrawString(TruncateText(item.Description, 35), normalFont, blackBrush, (int)(colPositions[0] + 5), yPosition + 15);
            graphics.DrawString(item.Quantity.ToString(), normalFont, blackBrush, (int)(colPositions[1] + 5), yPosition + 15);
            graphics.DrawString($"${item.UnitPrice:F2}", normalFont, blackBrush, (int)(colPositions[2] + 5), yPosition + 15);
            graphics.DrawString($"${item.TotalPrice:F2}", normalFont, blackBrush, (int)(colPositions[3] + 5), yPosition + 15);

            yPosition += rowHeight;
        }

        // Total section
        yPosition += 20;
        var totalBoxY = yPosition;
        var totalBoxWidth = 200;
        var totalBoxX = pageWidth - margin - totalBoxWidth;

        graphics.DrawRectangle(lightGrayPen, XBrushes.LightYellow, (int)totalBoxX, totalBoxY, (int)totalBoxWidth, 40);
        graphics.DrawString("Total Amount:", headerFont, blackBrush, (int)(totalBoxX + 10), totalBoxY + 15);
        graphics.DrawString($"${invoice.TotalAmount:F2}", headerFont, blackBrush, (int)(totalBoxX + 10), totalBoxY + 30);

        // Footer
        yPosition = (int)(pageHeight - margin - 30);
        graphics.DrawString($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC", smallFont, grayBrush, margin, yPosition);
        graphics.DrawString("Thank you for your business!", smallFont, grayBrush, (int)(pageWidth - margin - 150), yPosition);
    }

    private List<string> SplitAddress(string address, int maxLength)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(address)) return lines;

        var words = address.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            if ((currentLine + " " + word).Length <= maxLength)
            {
                currentLine += (currentLine.Length > 0 ? " " : "") + word;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }

    private async Task<BlobClient> GetBlobClientAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(PDF_CONTAINER_NAME);
        await containerClient.CreateIfNotExistsAsync();
        return containerClient.GetBlobClient(blobName);
    }

    public async Task<Stream?> GetPdfAsync(string blobName)
    {
        try
        {
            var blobClient = await GetBlobClientAsync(blobName);

            if (await blobClient.ExistsAsync())
            {
                return await blobClient.OpenReadAsync();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving PDF: {blobName}");
            return null;
        }
    }

    public async Task<List<string>> ListPdfsAsync(string? invoiceNumberFilter = null)
    {
        var pdfs = new List<string>();

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(PDF_CONTAINER_NAME);

            if (!await containerClient.ExistsAsync())
                return pdfs;

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (string.IsNullOrEmpty(invoiceNumberFilter) ||
                    blobItem.Name.StartsWith(invoiceNumberFilter))
                {
                    pdfs.Add(blobItem.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing PDFs");
        }

        return pdfs.OrderByDescending(x => x).ToList();
    }
}
