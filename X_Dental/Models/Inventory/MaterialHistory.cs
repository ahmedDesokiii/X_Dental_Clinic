using Dental.Models.Common;
using static Dental.Constants.EnumHelper;

namespace Dental.Models.Inventory
{
    public class MaterialHistory : BaseEntity
    {
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        public MaterialHistoryAction Action { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.Now;
        public string? UserName { get; set; }

        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public decimal? QuantityBefore { get; set; }
        public decimal? QuantityAfter { get; set; }
        public decimal? PriceBefore { get; set; }
        public decimal? PriceAfter { get; set; }

        public string? BatchNumber { get; set; }
        public int? ReferenceTransactionId { get; set; }
        public string? Notes { get; set; }
    }
}