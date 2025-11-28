using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MobileProviderBillPaymentSystem.Controllers;

[Route("v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class BankingAppController : ControllerBase
{
    [HttpGet("query-bill")]
    [Authorize]
    public IActionResult QueryBill([FromQuery] string subscriberNo)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");


        // TODO: call your service layer here

        return Ok(new
        {
            SubscriberNo = subscriberNo,
            BillTotal = 120.50,
            PaidStatus = "NotPaid"
        });
    }
}
