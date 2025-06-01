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
        public string? Settings { get; set; } // JSON settings

        // business-specific fields
        public string? TaxId { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public string? DefaultCurrency { get; set; } = "USD";
        public string? TimeZone { get; set; } = "UTC";
        public string? DateFormat { get; set; } = "MM/dd/yyyy";
        public bool MultiLocationEnabled { get; set; } = false;
        public int MaxUsers { get; set; } = 5;
        public int MaxProducts { get; set; } = 100;

        //check if the organization has completed onboarding
        public bool HasCompletedSetup { get; set; } = false;
        public DateTime? SetupCompletedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public virtual Subscription? Subscription { get; set; }
    }
}