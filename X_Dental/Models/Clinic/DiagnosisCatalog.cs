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
        public bool IsActive { get; set; } = true;
    }
}
