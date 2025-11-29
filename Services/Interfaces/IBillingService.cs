using MobileProviderBillPaymentSystem.Models;

namespace MobileProviderBillPaymentSystem.Services.Interfaces;

public interface IBillingService
{

    Task<object?> QueryBillAsync(string subscriberNo, DateTime month);

    Task<object> QueryBillDetailedAsync(
        string subscriberNo, DateTime month,
        int page, int pageSize);

    Task<IEnumerable<object>> QueryUnpaidBillsAsync(string subscriberNo);

    Task<bool> PayBillAsync(string subscriberNo, DateTime month, decimal amount);
    Task AddBillAsync(string subscriberNo, DateTime month, decimal amount, string? detailsJson);
    Task AddBillBatchAsync(List<Bill> billList);


}
