using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Inventory.DTOs
{
    public class MaterialFormDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public MaterialCategory Category { get; set; }
        public string? Description { get; set; }

        public MaterialUnit Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinimumStockLevel { get; set; }
        public string? StorageLocation { get; set; }

        public decimal PurchasePrice { get; set; }
        public decimal CurrentPrice { get; set; }

        public int? SupplierId { get; set; }
        public string? Manufacturer { get; set; }

        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class MaterialListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string UnitName { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal MinimumStockLevel { get; set; }
        public decimal CurrentPrice { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsOutOfStock { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpired { get; set; }
        public bool IsNearExpiry { get; set; }
    }

    public class MaterialDetailsDto : MaterialFormDto
    {
        public string CategoryName { get; set; } = null!;
        public string UnitName { get; set; } = null!;
        public string? SupplierName { get; set; }
        public bool IsOutOfStock { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpired { get; set; }
        public bool IsNearExpiry { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class StockInRequest
    {
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? SupplierId { get; set; }
        public string? Notes { get; set; }
    }

    public class StockOutRequest
    {
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public string? DestinationOrReason { get; set; }
        public int? PatientId { get; set; }
        public int? VisitId { get; set; }
        public string? Notes { get; set; }
    }

    public class MaterialHistoryDto
    {
        public string ActionText { get; set; } = null!;
        public string Icon { get; set; } = null!;          // ⭐ NEW
        public string ColorClass { get; set; } = null!;     // ⭐ NEW
        public DateTime ActionDate { get; set; }
        public string? UserName { get; set; }
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public decimal? QuantityBefore { get; set; }
        public decimal? QuantityAfter { get; set; }
        public decimal? PriceBefore { get; set; }
        public decimal? PriceAfter { get; set; }
        public string? Notes { get; set; }
    }

    public class InventoryDashboardDto
    {
        public int TotalMaterials { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<CategoryStockDto> StockByCategory { get; set; } = new();
    }

    public class CategoryStockDto
    {
        public string CategoryName { get; set; } = null!;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class MaterialFilterDto
    {
        public string? Search { get; set; }
        public MaterialCategory? Category { get; set; }
        public int? SupplierId { get; set; }
        public bool? LowStock { get; set; }
        public bool? OutOfStock { get; set; }
        public bool? Expired { get; set; }
        public bool? NearExpiry { get; set; }
        public bool? Available { get; set; }
    }
}