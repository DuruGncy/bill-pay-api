using Microsoft.EntityFrameworkCore;
using MobileProviderBillPaymentSystem.Context;
using MobileProviderBillPaymentSystem.Models;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MobileProviderBillPaymentSystem.Services;

public class UserService : IUserService
{
    private readonly BillingDbContext _context;

    public UserService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RegisterUser(string username, string password)
    {
        if (await _context.Users.AnyAsync(x => x.Username == username))
            return false;

        var hash = HashPassword(password);

        var user = new User
        {
            Username = username,
            PasswordHash = hash
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User?> Authenticate(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (user == null)
            return null;

        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
}
