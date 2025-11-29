using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileProviderBillPaymentSystem.Models;
using MobileProviderBillPaymentSystem.Services.Interfaces;

namespace MobileProviderBillPaymentSystem.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SubscriberController : ControllerBase
{
    private readonly ISubscriberService _subscriberService;

    public SubscriberController(ISubscriberService subscriberService)
    {
        _subscriberService = subscriberService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subscribers = await _subscriberService.GetAllAsync();
        return Ok(subscribers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var subscriber = await _subscriberService.GetByIdAsync(id);
        if (subscriber == null) return NotFound();
        return Ok(subscriber);
    }

    [HttpGet("by-number/{subscriberNo}")]
    public async Task<IActionResult> GetBySubscriberNo(string subscriberNo)
    {
        var subscriber = await _subscriberService.GetBySubscriberNoAsync(subscriberNo);
        if (subscriber == null) return NotFound();
        return Ok(subscriber);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddSubscriber([FromBody] Subscriber subscriber)
    {
        var created = await _subscriberService.AddSubscriberAsync(subscriber);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateSubscriber(int id, [FromBody] Subscriber subscriber)
    {
        var updated = await _subscriberService.UpdateSubscriberAsync(id, subscriber);
        if (updated == null) return NotFound();
        return Ok(updated);
    }
}
