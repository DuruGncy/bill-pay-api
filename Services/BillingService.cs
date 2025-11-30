using Microsoft.EntityFrameworkCore;
using MobileProviderBillPaymentSystem.Context;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using MobileProviderBillPaymentSystem.Models;
using System.Text.Json;

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
    public async Task<BillSummaryDto?> QueryBillAsync(string subscriberNo, DateTime month)
    {
        var bill = await _db.Bills
            .Include(b => b.Subscriber)
            .Where(b => b.Subscriber.SubscriberNo == subscriberNo &&
                        b.BillMonth == month)
            .Select(b => new BillSummaryDto
            {
                SubscriberNo = b.Subscriber.SubscriberNo,
                Month = b.BillMonth.ToString("yyyy-MM"),
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
    public async Task<BillDetailsDto?> QueryBillDetailedAsync(
        string subscriberNo,
        DateTime month,
        int page,
        int pageSize)
    {
        // Resolve the subscriber
        Subscriber? subscriber = null;
        if (int.TryParse(subscriberNo, out var numericId))
        {
            subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.Id == numericId);
        }

        subscriber ??= await _db.Subscribers
            .FirstOrDefaultAsync(s => s.SubscriberNo == subscriberNo);

        if (subscriber == null)
        {
            return new BillDetailsDto
            {
                SubscriberNo = subscriberNo,
                Month = month.ToString("yyyy-MM"),
                TotalItems = 0,
                TotalPages = 0,
                Items = new List<BillDetailItemDto>(),
            };
        }

        // Get bill
        var bill = await _db.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.SubscriberId == subscriber.Id &&
                                      b.BillMonth == month);

        if (bill == null)
        {
            return new BillDetailsDto
            {
                SubscriberNo = subscriber.SubscriberNo,
                Month = month.ToString("yyyy-MM"),
                TotalItems = 0,
                TotalPages = 0,
                Items = new List<BillDetailItemDto>(),
            };
        }

        // Deserialize bill details (JSON array) into JsonElements to avoid JsonElement -> IConvertible casts
        var allItems = new List<Dictionary<string, JsonElement>>();
        if (!string.IsNullOrWhiteSpace(bill.BillDetails))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(bill.BillDetails);
                if (parsed != null)
                    allItems = parsed;
            }
            catch (JsonException)
            {
                // invalid JSON — leave allItems empty and return empty paging below
                allItems = new List<Dictionary<string, JsonElement>>();
            }
        }

        var totalItems = allItems.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var pageData = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Convert to DTO items
        var items = pageData.Select((p, index) =>
        {
            // Safely read properties from JsonElement dictionary
            string description = "Unknown";
            if (p.TryGetValue("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                description = typeEl.GetString() ?? "Unknown";

            decimal amount = 0m;
            if (p.TryGetValue("cost", out var costEl))
            {
                if (costEl.ValueKind == JsonValueKind.Number && costEl.TryGetDecimal(out var d))
                    amount = d;
                else if (costEl.ValueKind == JsonValueKind.String && decimal.TryParse(costEl.GetString(), out var d2))
                    amount = d2;
            }

            int? mb = null;
            if (p.TryGetValue("mb", out var mbEl))
            {
                if (mbEl.ValueKind == JsonValueKind.Number && mbEl.TryGetInt32(out var mi))
                    mb = mi;
                else if (mbEl.ValueKind == JsonValueKind.String && int.TryParse(mbEl.GetString(), out var mi2))
                    mb = mi2;
            }

            int? duration = null;
            if (p.TryGetValue("duration", out var durEl))
            {
                if (durEl.ValueKind == JsonValueKind.Number && durEl.TryGetInt32(out var di))
                    duration = di;
                else if (durEl.ValueKind == JsonValueKind.String && int.TryParse(durEl.GetString(), out var di2))
                    duration = di2;
            }

            return new BillDetailItemDto
            {
                LineNumber = index + 1 + ((page - 1) * pageSize),
                Description = duration.HasValue ? $"{description} ({duration.Value}s)" : description,
                Amount = amount,
                DataMb = mb ?? 0,
                DurationSeconds = duration ?? 0
            };
        }).ToList();

        return new BillDetailsDto
        {
            SubscriberNo = subscriber.SubscriberNo,
            Month = month.ToString("yyyy-MM"),
            BillTotal = bill.BillTotal,
            AmountPaid = bill.AmountPaid,
            IsPaid = bill.IsPaid,
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
