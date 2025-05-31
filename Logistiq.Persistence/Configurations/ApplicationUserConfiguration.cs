using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Logistiq.Domain.Entities;

namespace Logistiq.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClerkUserId) 
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(x => x.ClerkUserId)
            .IsUnique();

        builder.HasIndex(x => x.Email);

    }
}