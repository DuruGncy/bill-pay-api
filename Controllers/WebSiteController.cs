using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace MobileProviderBillPaymentSystem.Controllers;


[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebSiteController : ControllerBase
{
    [HttpPost("pay-bill")]
    public IActionResult PayBill([FromQuery] string subscriberNo, [FromQuery] string month)
    {
        return Ok();
    }

    [HttpPost("admin/add-bill")]
    [Authorize]
    public IActionResult AddBill([FromQuery] string subscriberNo, [FromQuery] string month)
    {
        return Ok();
    }

    [HttpPost("admin/add-bill/batch")]
    [Authorize]
    public IActionResult AddBillBatch([FromQuery] string subscriberNo, [FromQuery] string month)
    {
        return Ok();
    }
}
