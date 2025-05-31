using Logistiq.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Entities
{
    public class Category : BaseEntity, IOrganizationEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }

        // Navigation Properties
        public virtual Category? ParentCategory { get; set; }
        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
