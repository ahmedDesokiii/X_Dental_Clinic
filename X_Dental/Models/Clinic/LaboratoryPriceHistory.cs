using Dental.Models.Common;

namespace Dental.Models.Clinic
{
    // ⭐ NEW — سجل تاريخي لأسعار المعمل، بيحافظ على قيمة الجلسات القديمة زي ما هي
    public class LaboratoryPriceHistory : BaseEntity
    {
        public int DiagnosisCatalogId { get; set; }
        public DiagnosisCatalog DiagnosisCatalog { get; set; } = null!;

        public decimal Price { get; set; }
        public DateTime EffectiveDate { get; set; } = DateTime.Now;
        public string? ChangedBy { get; set; }
        public string? Notes { get; set; }
    }
}