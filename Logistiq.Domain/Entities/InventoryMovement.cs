using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class InventoryMovement : BaseEntity, IOrganizationEntity
    {   
        //For future upgrade
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public Guid? WarehouseId { get; set; }
        public MovementType Type { get; set; }
        public int Quantity { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Organization? Organization { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse? Warehouse { get; set; } 
    }
}
