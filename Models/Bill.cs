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
