using Microsoft.EntityFrameworkCore;
using MobileProviderBillPaymentSystem.Context;
using MobileProviderBillPaymentSystem.Models;
using MobileProviderBillPaymentSystem.Services.Interfaces;

namespace MobileProviderBillPaymentSystem.Services;

public class SubscriberService : ISubscriberService
{
    private readonly BillingDbContext _db;

    public SubscriberService(BillingDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Subscriber>> GetAllAsync()
    {
        return await _db.Subscribers
            .Include(s => s.Bills)
            .ToListAsync();
    }

    public async Task<Subscriber?> GetByIdAsync(int id)
    {
        return await _db.Subscribers
            .Include(s => s.Bills)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subscriber?> GetBySubscriberNoAsync(string subscriberNo)
    {
        return await _db.Subscribers
            .Include(s => s.Bills)
            .FirstOrDefaultAsync(s => s.SubscriberNo == subscriberNo);
    }

    public async Task<Subscriber> AddSubscriberAsync(SubscriberDto subscriber)
    {
        // Map DTO -> entity
        var entity = new Subscriber
        {
            SubscriberNo = subscriber.SubscriberNo,
            FullName = subscriber.FullName,
            Bills = new List<Bill>()
        };

        _db.Subscribers.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<Subscriber?> UpdateSubscriberAsync(int id, Subscriber subscriber)
    {
        var existing = await _db.Subscribers.FindAsync(id);
        if (existing == null) return null;

        existing.FullName = subscriber.FullName;
        
        await _db.SaveChangesAsync();
        return existing;
    }
}
