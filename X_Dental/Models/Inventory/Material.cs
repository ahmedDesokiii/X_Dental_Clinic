using System.ComponentModel.DataAnnotations.Schema;
using Dental.Models.Common;
using static Dental.Constants.EnumHelper;

namespace Dental.Models.Inventory
{
    public class Material : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public MaterialCategory Category { get; set; }
        public string? Description { get; set; }

        public MaterialUnit Unit { get; set; }
        public decimal Quantity { get; set; } = 0;
        public decimal MinimumStockLevel { get; set; } = 0;
        public string? StorageLocation { get; set; }

        public decimal PurchasePrice { get; set; } = 0;
        public decimal CurrentPrice { get; set; } = 0;

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public string? Manufacturer { get; set; }

        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }

        // Navigation
        public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
        public ICollection<MaterialHistory> History { get; set; } = new List<MaterialHistory>();

        // ⭐ Computed — مش مخزّن، دايمًا بيتحسب Live
        [NotMapped]
        public bool IsOutOfStock => Quantity <= 0;
        [NotMapped]
        public bool IsLowStock => Quantity > 0 && Quantity <= MinimumStockLevel;
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;
        [NotMapped]
        public bool IsNearExpiry => ExpiryDate.HasValue
            && !IsExpired
            && (ExpiryDate.Value.Date - DateTime.Today).TotalDays <= 30;
    }
}