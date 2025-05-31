using Logistiq.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Entities
{   
    public class Warehouse : BaseEntity, IOrganizationEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }
}
