using Dental.Models.Common;

namespace Dental.Models.Clinic
{
    // ========== Master Data: Procedure Catalog ==========
    public class ProcedureCatalog : BaseEntity
    {
        public string Code { get; set; } = null!; // مثال: RCT, CRN
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public decimal DefaultPrice { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
