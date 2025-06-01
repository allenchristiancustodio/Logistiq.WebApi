using Logistiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Logistiq.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(x => x.Id);

            // Properties
            builder.Property(x => x.ClerkOrganizationId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SubTotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TaxAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.DiscountAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.ShippingAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.Property(x => x.ShippingAddress)
                .HasMaxLength(500);

            builder.Property(x => x.BillingAddress)
                .HasMaxLength(500);

            builder.Property(x => x.TrackingNumber)
                .HasMaxLength(100);

            builder.Property(x => x.CreatedByUserId)
                .HasMaxLength(100);

            builder.HasIndex(x => new { x.ClerkOrganizationId, x.OrderNumber }).IsUnique();
            builder.HasIndex(x => x.OrderDate);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Type);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Supplier)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.OrderItems)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}