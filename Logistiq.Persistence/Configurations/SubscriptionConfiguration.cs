using Logistiq.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Logistiq.Persistence.Configurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StripeCustomerId)
                .HasMaxLength(100);

            builder.Property(x => x.StripeSubscriptionId)
                .HasMaxLength(100);

            builder.Property(x => x.StripePriceId)
                .HasMaxLength(100);

            builder.Property(x => x.PlanName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.MonthlyPrice)
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.StartDate)
                .IsRequired();

            builder.Property(x => x.EndDate)
                .IsRequired();

            builder.HasIndex(x => x.StripeCustomerId);
            builder.HasIndex(x => x.StripeSubscriptionId);
            builder.HasIndex(x => x.CompanyId)
                .IsUnique();
        }
    }
}
