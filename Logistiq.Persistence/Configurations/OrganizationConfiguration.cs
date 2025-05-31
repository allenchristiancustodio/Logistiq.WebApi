using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Logistiq.Domain.Entities;

namespace Logistiq.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Slug)
            .HasMaxLength(100);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Industry)
            .HasMaxLength(100);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasMaxLength(255);

        builder.Property(x => x.Website)
            .HasMaxLength(500);

        // Relationships
        builder.HasMany(x => x.Products)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.ClerkOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Orders)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.ClerkOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Customers)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.ClerkOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Suppliers)
            .WithOne(x => x.Organization)
            .HasForeignKey(x => x.ClerkOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Subscription)
            .WithOne(x => x.Organization)
            .HasForeignKey<Subscription>(x => x.ClerkOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}