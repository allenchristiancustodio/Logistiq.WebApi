using Logistiq.Domain.Common;

namespace Logistiq.Domain.Entities
{
    public class Organization : BaseEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;

        // Business-specific settings (not handled by Clerk)
        public string? Settings { get; set; } 

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual Subscription? Subscription { get; set; }
    }
}