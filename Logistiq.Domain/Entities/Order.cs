using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class Order : BaseEntity, ITenantEntity
    {
        public Guid CompanyId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public Guid? CustomerId { get; set; }
        //public Guid? SupplierId { get; set; } for future
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; } = null!;
        public virtual Customer? Customer { get; set; }
        //public virtual Supplier? Supplier { get; set; } for future
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
