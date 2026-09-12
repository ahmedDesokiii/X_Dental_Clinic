using Dental.Models.Common;
using static Dental.Constants.EnumHelper;

namespace Dental.Models.Inventory
{
    public class StockTransaction : BaseEntity
    {
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        public StockTransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }

        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? UserName { get; set; }

        // Stock Out بس
        public string? DestinationOrReason { get; set; }
        public int? PatientId { get; set; }
        public int? VisitId { get; set; }

        public string? Notes { get; set; }
    }
}