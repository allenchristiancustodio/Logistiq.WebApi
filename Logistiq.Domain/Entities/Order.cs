﻿using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class Order : BaseEntity, IOrganizationEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? SupplierId { get; set; }

        // Financial
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Additional fields
        public string? Notes { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CreatedByUserId { get; set; }

        public string? PurchaseOrderNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; } = "Pending";
        public string? Currency { get; set; } = "USD";
        public decimal ExchangeRate { get; set; } = 1.0m;

        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual Customer? Customer { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
