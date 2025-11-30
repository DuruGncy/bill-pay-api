using Asp.Versioning;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Models;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Globalization;
using System.Linq; // added for Skip/Join
using System.Text.Json;

namespace MobileProviderBillPaymentSystem.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebSiteController : ControllerBase
{
    private readonly IBillingService _billingService;

    public WebSiteController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    /// <summary>
    /// Pay a bill for a subscriber for a given month.
    /// Supports partial payment via 'amount'.
    /// </summary>
    [HttpPost("pay-bill")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PayBill(
        [FromQuery] string subscriberNo,
        [FromQuery] string month,
        [FromQuery] decimal amount)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");
        if (string.IsNullOrWhiteSpace(month))
            return BadRequest("Month is required.");
        if (!DateTime.TryParseExact(month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var billMonth))
        {
            return BadRequest("Invalid month format. Use yyyy-MM.");
        }
        if (amount <= 0)
            return BadRequest("Amount must be greater than 0.");

        var result = await _billingService.PayBillAsync(subscriberNo, billMonth, amount);

        return Ok(new
        {
            Message = "Bill payment processed successfully.",
            SubscriberNo = subscriberNo,
            Month = month,
            AmountPaid = amount,
            Status = result ? "Successful" : "AlreadyPaid"
        });
    }

    /// <summary>
    /// Admin: Add a single bill.
    /// </summary>
    [HttpPost("admin/add-bill")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddBill(
        [FromQuery] string subscriberNo,
        [FromQuery] string month,
        [FromQuery] decimal amount,
        [FromQuery] string? detailsJson = null)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");
        if (string.IsNullOrWhiteSpace(month))
            return BadRequest("Month is required.");
        if (!DateTime.TryParseExact(month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var billMonth))
        {
            return BadRequest("Invalid month format. Use yyyy-MM.");
        }
        if (amount <= 0)
            return BadRequest("Amount must be greater than 0.");

        await _billingService.AddBillAsync(subscriberNo, billMonth, amount, detailsJson);

        return Ok(new
        {
            Message = "Bill added successfully.",
            SubscriberNo = subscriberNo,
            Month = month,
            Amount = amount
        });
    }

    /// <summary>
    /// Admin: Add bills in batch (list of bills).
    /// </summary>
    [HttpPost("admin/add-bill/batch")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Upload a CSV file containing bills")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddBillBatch(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            
        });

        var bills = new List<Bill>();
        while (await csv.ReadAsync())
        {
            if (!csv.TryGetField(0, out int subscriberId)) continue;
            if (!csv.TryGetField(1, out string monthStr)) continue;
            if (!DateTime.TryParseExact(monthStr.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var billMonth)) continue;
            if (!csv.TryGetField(2, out decimal billTotal)) continue;

            string? billDetails = null;
            if (csv.TryGetField(3, out string detailsRaw) && !string.IsNullOrWhiteSpace(detailsRaw))
            {
                string cleaned = detailsRaw;

                // Remove wrapping quotes (CsvHelper keeps them)
                if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
                    cleaned = cleaned.Substring(1, cleaned.Length - 2);

                // Convert doubled quotes back to normal JSON quotes
                cleaned = cleaned.Replace("\"\"", "\"");

                try
                {
                    using var _ = JsonDocument.Parse(cleaned);
                    billDetails = cleaned; // store valid JSON
                }
                catch (JsonException)
                {
                    billDetails = null;
                }
            }

            bills.Add(new Bill { SubscriberId = subscriberId, BillMonth = billMonth, BillTotal = billTotal, BillDetails = billDetails });
        }

        if (!bills.Any())
            return BadRequest("No valid bills found in the CSV file.");

        await _billingService.AddBillBatchAsync(bills);

        return Ok(new
        {
            Message = "Batch bill creation successful.",
            Count = bills.Count
        });
    }

}
