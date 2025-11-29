using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using MobileProviderBillPaymentSystem.Models;

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
    [HttpPost("admin/add-bill/batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddBillBatch([FromBody] List<Bill> bills)
    {
        if (bills == null || !bills.Any())
            return BadRequest("No bills provided for batch creation.");

        await _billingService.AddBillBatchAsync(bills);

        return Ok(new
        {
            Message = "Batch bill creation successful.",
            Count = bills.Count
        });
    }
}
