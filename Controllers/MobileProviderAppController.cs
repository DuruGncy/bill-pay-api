using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Services.Interfaces;

namespace MobileProviderBillPaymentSystem.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MobileProviderAppController : ControllerBase
{
    private readonly IBillingService _billingService;

    public MobileProviderAppController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    // ---------------------------------------------------------
    // QUERY BILL (summary)
    // ---------------------------------------------------------
    [HttpGet("query-bill")]
    [Authorize]
    public async Task<IActionResult> QueryBill(
        [FromQuery] string subscriberNo,
        [FromQuery] string month)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");

        if (!DateTime.TryParseExact(month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var billMonth))
        {
            return BadRequest("Invalid month format. Use yyyy-MM.");
        }

        try
        {
            var bill = await _billingService.QueryBillAsync(subscriberNo, billMonth);
            if (bill == null)
                return NotFound("No bill found for this subscriber and month.");

            return Ok(bill);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving bill: {ex.Message}");
        }
    }

    // ---------------------------------------------------------
    // QUERY BILL DETAILED (paged, list items)
    // ---------------------------------------------------------
    [HttpGet("query-bill-detailed")]
    [Authorize]
    public async Task<IActionResult> QueryBillDetailed(
        [FromQuery] string subscriberNo,
        [FromQuery] string month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");

        if (string.IsNullOrWhiteSpace(month))
            return BadRequest("Month is required.");
        if (!DateTime.TryParseExact(month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var billMonth))
        {
            return BadRequest("Invalid month format. Use yyyy-MM.");
        }

        if (page < 1)
            return BadRequest("Page must be >= 1.");

        if (pageSize < 1 || pageSize > 100)
            return BadRequest("PageSize must be between 1 and 100.");

        try
        {
            var result = await _billingService.QueryBillDetailedAsync(
                subscriberNo, billMonth, page, pageSize);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving bill details: {ex.Message}");
        }
    }
}
