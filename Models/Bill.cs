using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MobileProviderBillPaymentSystem.Models;

public class Bill
{
    public int Id { get; set; }

    public int SubscriberId { get; set; }

    [JsonIgnore]
    public Subscriber Subscriber { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime BillMonth { get; set; }

    public decimal BillTotal { get; set; }

    // JSON stored in PostgreSQL → use `jsonb` in migration
    public string? BillDetails { get; set; }

    // Payment tracking
    public decimal AmountPaid { get; set; } = 0;

    public bool IsPaid { get; set; } = false;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class BillSummaryDto
{
    public string SubscriberNo { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public decimal BillTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaidStatus { get; set; } = string.Empty;
}

public class BillDetailItemDto
{
    public int LineNumber { get; set; }

    // Type of item: "sms", "call", "data", or "payment"
    public string Description { get; set; } = string.Empty;

    // Cost of this item
    public decimal Amount { get; set; }

    // Number of MB used for data items
    public int? DataMb { get; set; }

    // Duration in seconds for calls
    public int? DurationSeconds { get; set; }
}


public class BillDetailsDto
{
    public string SubscriberNo { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;

    public decimal BillTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsPaid { get; set; }

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public List<BillDetailItemDto> Items { get; set; } = new();
}

public class ErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string SubscriberNo { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
}


