using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace MobileProviderBillPaymentSystem.Controllers;


[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class MobileProviderAppController : ControllerBase
{
    [HttpGet("query-bill")]
    [Authorize]
    public IActionResult QueryBill([FromQuery] string subscriberNo, [FromQuery] string month)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");

        if (string.IsNullOrWhiteSpace(month))
            return BadRequest("Month is required.");

        // TODO: call your service layer here

        return Ok(new
        {
            SubscriberNo = subscriberNo,
            Month = month,
            BillTotal = 120.50,
            PaidStatus = "NotPaid"
        });
    }



    [HttpGet("query-bill-detailed")]
    [Authorize]
    public IActionResult QueryBillDetailed(
        [FromQuery] string subscriberNo,
        [FromQuery] string month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(subscriberNo))
            return BadRequest("Subscriber number is required.");

        if (string.IsNullOrWhiteSpace(month))
            return BadRequest("Month is required.");

        if (page < 1)
            return BadRequest("Page must be greater than or equal to 1.");

        if (pageSize < 1 || pageSize > 100)
            return BadRequest("PageSize must be between 1 and 100.");

        // Mock data generation (replace with real service call)
        var items = new List<object>();
        int totalItems = 25;
        for (int i = 1; i <= totalItems; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            items.Add(new
            {
                LineNumber = i,
                Date = date.ToString("yyyy-MM-dd"),
                Description = (i % 2 == 0) ? $"Call to +1-555-010{i:00}" : $"Data usage session #{i}",
                Amount = Math.Round(1.25m * i, 2),
                Tax = Math.Round(0.10m * (1.25m * i), 2),
                Total = Math.Round((1.25m * i) * 1.10m, 2),
                Paid = (i % 3 == 0) // every 3rd item marked paid
            });
        }

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        int startIndex = (page - 1) * pageSize;
        List<object> pagedItems;

        if (startIndex >= totalItems)
        {
            pagedItems = new List<object>();
        }
        else
        {
            int take = Math.Min(pageSize, totalItems - startIndex);
            pagedItems = items.GetRange(startIndex, take);
        }

        var response = new
        {
            SubscriberNo = subscriberNo,
            Month = month,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Items = pagedItems
        };

        return Ok(response);
    }
}

