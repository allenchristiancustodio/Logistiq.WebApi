using Logistiq.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Persistence.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            // Composite index for performance
            builder.HasIndex(x => new { x.OrderId, x.ProductId });


            builder.HasOne(x => x.Company)
              .WithMany() // No navigation property from Company
              .HasForeignKey(x => x.CompanyId)
              .OnDelete(DeleteBehavior.NoAction);
                }
    }
}
