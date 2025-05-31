using Logistiq.Domain.Common;

namespace Logistiq.Domain.Entities
{
    public class Organization : BaseEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; } 
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Settings { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual Subscription? Subscription { get; set; }

        public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    }
}