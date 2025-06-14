﻿using Logistiq.Domain.Common;
using Logistiq.Domain.Enums;

namespace Logistiq.Domain.Entities
{
    public class Product : BaseEntity, IOrganizationEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int? MinStockLevel { get; set; }
        public int? MaxStockLevel { get; set; }
        public string? Unit { get; set; } = "pcs"; // Default unit
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? ImageUrl { get; set; }
        public string? Tags { get; set; } // JSON or comma-separated
        public bool TrackInventory { get; set; } = true;
        public bool AllowBackorder { get; set; } = false;

        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!; 
        public virtual Category? Category { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }
}
