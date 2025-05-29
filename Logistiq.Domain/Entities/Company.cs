using Logistiq.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class Company : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual Subscription? Subscription { get; set; }

        //public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>(); for future use
        //public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>(); for future use

    }
}
