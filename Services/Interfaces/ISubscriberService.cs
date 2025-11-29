using MobileProviderBillPaymentSystem.Models;

namespace MobileProviderBillPaymentSystem.Services.Interfaces;

public interface ISubscriberService
{
    Task<IEnumerable<Subscriber>> GetAllAsync();
    Task<Subscriber?> GetByIdAsync(int id);
    Task<Subscriber?> GetBySubscriberNoAsync(string subscriberNo);
    Task<Subscriber> AddSubscriberAsync(Subscriber subscriber);
    Task<Subscriber?> UpdateSubscriberAsync(int id, Subscriber subscriber);
}
