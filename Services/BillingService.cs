using Microsoft.EntityFrameworkCore;
using MobileProviderBillPaymentSystem.Context;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using MobileProviderBillPaymentSystem.Models;

namespace MobileProviderBillPaymentSystem.Services;

public class BillingService : IBillingService
{
    private readonly BillingDbContext _db;

    public BillingService(BillingDbContext db)
    {
        _db = db;
    }

    // --------------------------------------------------------
    // QUERY BILL (SUMMARY)
    // --------------------------------------------------------
    public async Task<object?> QueryBillAsync(string subscriberNo, DateTime month)
    {
        var bill = await _db.Bills
            .Include(b => b.Subscriber)
            .Where(b => b.Subscriber.SubscriberNo == subscriberNo &&
                        b.BillMonth == month)
            .Select(b => new
            {
                b.Subscriber.SubscriberNo,
                Month = b.BillMonth,
                BillTotal = b.BillTotal,
                AmountPaid = b.AmountPaid,
                PaidStatus = b.IsPaid ? "Paid" : "NotPaid"
            })
            .FirstOrDefaultAsync();

        return bill;
    }

    // --------------------------------------------------------
    // QUERY BILL (DETAILED)
    // --------------------------------------------------------
    public async Task<object> QueryBillDetailedAsync(
    string subscriberNo, DateTime month, int page, int pageSize)
    {
        // Get query filtered from database
        var query = _db.Payments
            .Include(p => p.Bill)
            .ThenInclude(b => b.Subscriber)
            .Where(p => p.Bill.Subscriber.SubscriberNo == subscriberNo &&
                        p.Bill.BillMonth == month)
            .OrderBy(p => p.Id);

        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Pull page into memory to allow client-side projection
        var pageData = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Now do the projection in memory
        var items = pageData
            .Select((p, index) => new
            {
                LineNumber = index + 1 + ((page - 1) * pageSize),
                Date = p.PaymentDate.ToString("yyyy-MM-dd"),
                Description = "Payment",
                Amount = p.Amount,
                p.Status
            })
            .ToList();

        return new
        {
            SubscriberNo = subscriberNo,
            Month = month.ToString("yyyy-MM"),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Items = items
        };
    }


    // --------------------------------------------------------
    // QUERY ALL UNPAID BILLS
    // --------------------------------------------------------
    public async Task<IEnumerable<object>> QueryUnpaidBillsAsync(string subscriberNo)
    {
        var bills = await _db.Bills
            .Include(b => b.Subscriber)
            .Where(b => b.Subscriber.SubscriberNo == subscriberNo && !b.IsPaid)
            .OrderBy(b => b.BillMonth)
            .Select(b => new
            {
                SubscriberNo = b.Subscriber.SubscriberNo,
                Month = b.BillMonth.ToString(),
                BillTotal = b.BillTotal,
                AmountPaid = b.AmountPaid,
                PaidStatus = "NotPaid"
            })
            .ToListAsync();

        // Return empty list if no bills found
        return bills;
    }

    // --------------------------------------------------------
    // PAY BILL (SUPPORTS PARTIAL)
    // --------------------------------------------------------
    public async Task<bool> PayBillAsync(string subscriberNo, DateTime month, decimal amount)
    {
        var bill = await _db.Bills
            .Include(b => b.Subscriber)
            .FirstOrDefaultAsync(b =>
                b.Subscriber.SubscriberNo == subscriberNo &&
                b.BillMonth == month);

        if (bill == null)
            throw new Exception("Bill not found.");

        // Create payment record
        var payment = new Payment
        {
            BillId = bill.Id,
            Amount = amount,
            Status = "Successful"
        };

        _db.Payments.Add(payment);

        // Update bill totals
        bill.AmountPaid += amount;

        if (bill.AmountPaid >= bill.BillTotal)
            bill.IsPaid = true;

        await _db.SaveChangesAsync();
        return true;
    }

    // --------------------------------------------------------
    // ADMIN: ADD BILL
    // --------------------------------------------------------
    public async Task AddBillAsync(string subscriberNo, DateTime month, decimal amount, string? detailsJson)
    {
        var subscriber = await _db.Subscribers
            .FirstOrDefaultAsync(s => s.SubscriberNo == subscriberNo);

        if (subscriber == null)
            throw new Exception("Subscriber does not exist.");

        bool exists = await _db.Bills.AnyAsync(b =>
            b.SubscriberId == subscriber.Id &&
            b.BillMonth == month);

        if (exists)
            throw new Exception("Bill already exists.");

        var bill = new Bill
        {
            SubscriberId = subscriber.Id,
            BillMonth = month,
            BillTotal = amount,
            BillDetails = detailsJson,
            IsPaid = false,
            AmountPaid = 0
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync();
    }

    // --------------------------------------------------------
    // ADMIN: BATCH ADD BILLS FROM CSV
    // --------------------------------------------------------
    public async Task AddBillBatchAsync(List<Bill> billList)
    {
        foreach (var bill in billList)
        {
            var subscriber = await _db.Subscribers
                .FirstOrDefaultAsync(s => s.Id == bill.SubscriberId);

            if (subscriber == null)
            {
                // Skip or throw
                Console.WriteLine($"Skipping bill for unknown subscriber {bill.SubscriberId}");
                continue;
            }

            bool exists = await _db.Bills.AnyAsync(b =>
                b.SubscriberId == bill.SubscriberId &&
                b.BillMonth == bill.BillMonth);

            if (!exists)
                _db.Bills.Add(bill);
        }

        await _db.SaveChangesAsync();
    }

}
