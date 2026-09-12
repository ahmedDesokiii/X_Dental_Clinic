using Dental.Models.Common;
using static Dental.Constants.EnumHelper;

namespace Dental.Models.Clinic
{
    // ========== Master Data: Diagnosis Catalog ==========
    public class DiagnosisCatalog : BaseEntity
    {
        public string Code { get; set; } = null!; // مثال: D01
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public DiagnosisCategory Category { get; set; }
        public string? Description { get; set; }

        public decimal Price { get; set; } = 0;
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        // ⭐ NEW — Feature 2: Laboratory
        public bool IsLaboratoryRelated { get; set; } = false;
    }
}
