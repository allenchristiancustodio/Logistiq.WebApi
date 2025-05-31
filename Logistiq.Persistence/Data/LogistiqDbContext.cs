using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Common;
using Logistiq.Domain.Entities;
using System.Reflection;
using System.Linq.Expressions;

namespace Logistiq.Persistence.Data;

public class LogistiqDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public LogistiqDbContext(DbContextOptions<LogistiqDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;

    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<InventoryMovement> InventoryMovements { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(LogistiqDbContext)
                    .GetMethod(nameof(GetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                var filter = method.Invoke(null, Array.Empty<object>());
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)filter!);
            }
        }

        var currentOrgId = _currentUserService.OrganizationId;
        if (!string.IsNullOrEmpty(currentOrgId))
        {
            // Add organization filters for all organization entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IOrganizationEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(IOrganizationEntity.ClerkOrganizationId));
                    var constant = Expression.Constant(currentOrgId);
                    var equals = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda(equals, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }
        if (!string.IsNullOrEmpty(currentOrgId))
        {
            // Products
            modelBuilder.Entity<Product>().HasQueryFilter(p =>
                p.ClerkOrganizationId == currentOrgId && !p.IsDeleted);

            // Categories
            modelBuilder.Entity<Category>().HasQueryFilter(c =>
                c.ClerkOrganizationId == currentOrgId && !c.IsDeleted);

            // Add more entities as needed...
        }


        base.OnModelCreating(modelBuilder);
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>() where TEntity : class, ISoftDeletable
    {
        Expression<Func<TEntity, bool>> filter = x => !x.IsDeleted;
        return filter;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Keep your existing audit logic
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IOrganizationEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.ClerkOrganizationId))
            {
                entry.Entity.ClerkOrganizationId = _currentUserService.OrganizationId ?? "";
            }
        }

        // Keep your existing soft delete logic
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
                entry.Entity.DeletedBy = _currentUserService.UserId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}