using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Services.Interfaces;

namespace MobileProviderBillPaymentSystem.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class BankingAppController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BankingAppController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    /// <summary>
    /// Returns all unpaid bills for the subscriber, grouped by month.
    /// </summary>
    [HttpGet("query-bill")]
    [Authorize]
    public async Task<IActionResult> QueryBill([FromQuery] string subscriberNo)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");

        var result = await _billingService.QueryUnpaidBillsAsync(subscriberNo);

        if (!result.Any())
            return NotFound("No unpaid bills found for this subscriber.");

        return Ok(result);
    }
}
