using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Models;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using System.Globalization;
using Swashbuckle.AspNetCore.Annotations;


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

        var bills = new List<Bill>();

        using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        {
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Skip header row
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                var columns = line.Split(',');

                if (columns.Length < 3)
                    continue; // skip invalid rows

                if (!int.TryParse(columns[0].Trim(), out int subscriberId))
                    continue; // skip invalid subscriber id

                var billMonth = DateTime.ParseExact(columns[1].Trim(), "yyyy-MM", CultureInfo.InvariantCulture);
      ;

                if (!decimal.TryParse(columns[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal billTotal))
                    continue; // skip invalid total

                string? billDetails = null;

                if (columns.Length > 3)
                {
                    var raw = columns[3].Trim();

                    if (!string.IsNullOrEmpty(raw))
                    {
                        try
                        {
                            // Try parsing it as JSON to validate
                            using var doc = System.Text.Json.JsonDocument.Parse(raw);

                            // Store it as raw JSON string
                            billDetails = raw;
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Invalid JSON, skip this row or set to null
                            billDetails = null;
                        }
                    }
                }

                bills.Add(new Bill
                {
                    SubscriberId = subscriberId,
                    BillMonth = billMonth,
                    BillTotal = billTotal,
                    BillDetails = billDetails
                });
            }
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
