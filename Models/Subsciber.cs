namespace MobileProviderBillPaymentSystem.Models;

public class Subscriber
{
    public int Id { get; set; }

    public string SubscriberNo { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}

public class SubscriberDto
{
    public string SubscriberNo { get; set; }
    public string FullName { get; set; }
}
