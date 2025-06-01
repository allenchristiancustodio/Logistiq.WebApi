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
    public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
    {
        public void Configure(EntityTypeBuilder<InventoryMovement> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ClerkOrganizationId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.Reference)
                .HasMaxLength(100);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.Property(x => x.MovementDate)
                .IsRequired();

            // Indexes
            builder.HasIndex(x => x.ClerkOrganizationId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.MovementDate);
            builder.HasIndex(x => x.Type);

        }
    }
}
