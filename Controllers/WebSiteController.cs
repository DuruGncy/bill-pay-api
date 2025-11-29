using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Models;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using System.Globalization;

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
    [Consumes("multipart/form-data")]
    [HttpPost("admin/add-bill/batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddBillBatch([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var bills = new List<Bill>();

        using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var columns = line.Split(','); // naive CSV parsing
                bills.Add(new Bill
                {
                    SubscriberId = int.Parse(columns[0]),
                    BillMonth = DateTime.ParseExact(columns[1], "yyyy-MM", CultureInfo.InvariantCulture),
                    BillTotal = decimal.Parse(columns[2]),
                    BillDetails = columns.Length > 3 ? columns[3] : null
                });
            }
        }

        await _billingService.AddBillBatchAsync(bills);

        return Ok(new
        {
            Message = "Batch bill creation successful.",
            bills.Count
        });
    }

}
