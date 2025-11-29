using Microsoft.EntityFrameworkCore;
using MobileProviderBillPaymentSystem.Models;

namespace MobileProviderBillPaymentSystem.Context;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    { }

    public DbSet<Subscriber> Subscribers { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Payment> Payments { get; set; }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.ToTable("bills");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.Id).HasColumnName("id");
            entity.Property(b => b.SubscriberId).HasColumnName("subscriber_id");
            entity.Property(b => b.BillMonth).HasColumnName("bill_month");
            entity.Property(b => b.BillTotal).HasColumnName("bill_total");
            entity.Property(b => b.BillDetails).HasColumnName("bill_details").HasColumnType("jsonb");
            entity.Property(b => b.IsPaid).HasColumnName("is_paid");
            entity.Property(b => b.AmountPaid).HasColumnName("amount_paid");

            // Relationships
            entity.HasOne(b => b.Subscriber)
                  .WithMany(s => s.Bills)
                  .HasForeignKey(b => b.SubscriberId);
        });

        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.ToTable("subscribers");
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.SubscriberNo).HasColumnName("subscriber_no");
            entity.Property(s => s.FullName).HasColumnName("full_name");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.BillId).HasColumnName("bill_id");
            entity.Property(p => p.Amount).HasColumnName("amount");
            entity.Property(p => p.Status).HasColumnName("status");
            entity.Property(p => p.PaymentDate).HasColumnName("payment_date");

            entity.HasOne(p => p.Bill)
                  .WithMany(b => b.Payments)
                  .HasForeignKey(p => p.BillId);
        });

    }

}
