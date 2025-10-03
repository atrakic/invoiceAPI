using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using invoiceAPI.Services;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;

namespace invoiceAPI;

public class PdfController
{
    private readonly ILogger<PdfController> _logger;
    private readonly PdfGenerationService _pdfGenerationService;

    public PdfController(ILogger<PdfController> logger, PdfGenerationService pdfGenerationService)
    {
        _logger = logger;
        _pdfGenerationService = pdfGenerationService;
    }

    // GET /api/pdfs - List all generated PDF files
    [Function("GetPdfs")]
    [OpenApiOperation(operationId: "GetPdfs", tags: new[] { "PDF Management" }, Summary = "List generated PDFs", Description = "Retrieve a list of all generated PDF files, optionally filtered by invoice number")]
    [OpenApiParameter(name: "invoiceNumber", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter PDFs by invoice number")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PdfListResponse), Description = "List of PDF files")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> GetPdfs([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pdfs")] HttpRequest req)
    {
        try
        {
            var invoiceNumberFilter = req.Query["invoiceNumber"].FirstOrDefault();
            var pdfs = await _pdfGenerationService.ListPdfsAsync(invoiceNumberFilter);

            var response = new PdfListResponse
            {
                PDFs = pdfs.Select(pdf => new PdfInfo
                {
                    FileName = pdf,
                    InvoiceNumber = ExtractInvoiceNumberFromFileName(pdf),
                    GeneratedAt = ExtractDateFromFileName(pdf),
                    ViewUrl = $"{GetBaseUrl(req)}/api/pdfs/{Uri.EscapeDataString(pdf)}/view",
                    DownloadUrl = $"{GetBaseUrl(req)}/api/pdfs/{Uri.EscapeDataString(pdf)}/download"
                }).ToList(),
                Count = pdfs.Count
            };

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF list");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // GET /api/pdfs/{fileName}/view - View PDF in browser
    [Function("ViewPdf")]
    [OpenApiOperation(operationId: "ViewPdf", tags: new[] { "PDF Management" }, Summary = "View PDF in browser", Description = "Display a PDF file directly in the browser")]
    [OpenApiParameter(name: "fileName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "PDF file name")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(FileResult), Description = "PDF file content for browser viewing")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "PDF file not found")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> ViewPdf([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pdfs/{fileName}/view")] HttpRequest req, string fileName)
    {
        try
        {
            var decodedFileName = Uri.UnescapeDataString(fileName);
            var pdfStream = await _pdfGenerationService.GetPdfAsync(decodedFileName);

            if (pdfStream == null)
            {
                return new NotFoundObjectResult("PDF file not found");
            }

            // Copy stream to memory for browser viewing
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            await pdfStream.DisposeAsync();

            var pdfBytes = memoryStream.ToArray();

            // Return PDF with inline content disposition for browser viewing
            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = decodedFileName,
                EnableRangeProcessing = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error viewing PDF: {fileName}");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    // GET /api/pdfs/{fileName}/download - Download PDF file
    [Function("DownloadPdf")]
    [OpenApiOperation(operationId: "DownloadPdf", tags: new[] { "PDF Management" }, Summary = "Download PDF file", Description = "Download a PDF file as an attachment")]
    [OpenApiParameter(name: "fileName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "PDF file name")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/pdf", bodyType: typeof(FileResult), Description = "PDF file content for download")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "PDF file not found")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error")]
    public async Task<IActionResult> DownloadPdf([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pdfs/{fileName}/download")] HttpRequest req, string fileName)
    {
        try
        {
            var decodedFileName = Uri.UnescapeDataString(fileName);
            var pdfStream = await _pdfGenerationService.GetPdfAsync(decodedFileName);

            if (pdfStream == null)
            {
                return new NotFoundObjectResult("PDF file not found");
            }

            // Copy stream to memory for download
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            await pdfStream.DisposeAsync();

            var pdfBytes = memoryStream.ToArray();

            // Return PDF with attachment content disposition for download
            var result = new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = decodedFileName,
                EnableRangeProcessing = true
            };

            // Force download by setting Content-Disposition header
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading PDF: {fileName}");
            return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
        }
    }

    private string ExtractInvoiceNumberFromFileName(string fileName)
    {
        try
        {
            // Expected format: INV-001_20231003123456.pdf
            var parts = fileName.Split('_');
            return parts.Length > 0 ? parts[0] : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private DateTime? ExtractDateFromFileName(string fileName)
    {
        try
        {
            // Expected format: INV-001_20231003123456.pdf
            var parts = fileName.Split('_');
            if (parts.Length > 1)
            {
                var dateTimeString = parts[1].Replace(".pdf", "");
                if (DateTime.TryParseExact(dateTimeString, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    return parsedDate;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return null;
    }

    private string GetBaseUrl(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}";
    }
}

// Response models for API documentation
public class PdfListResponse
{
    public List<PdfInfo> PDFs { get; set; } = new();
    public int Count { get; set; }
}

public class PdfInfo
{
    public string FileName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? GeneratedAt { get; set; }
    public string ViewUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}
