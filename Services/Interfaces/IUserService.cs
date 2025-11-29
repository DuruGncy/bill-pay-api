using MobileProviderBillPaymentSystem.Models;

namespace MobileProviderBillPaymentSystem.Services.Interfaces;

public interface IUserService
{
    Task<bool> RegisterUser(string username, string password);
    Task<User?> Authenticate(string username, string password);
}
