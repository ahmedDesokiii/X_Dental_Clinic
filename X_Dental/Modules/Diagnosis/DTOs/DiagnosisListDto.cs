// ============================================================
// 📁 File: Modules/Diagnosis/DTOs/DiagnosisDtos.cs
// ============================================================
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Diagnosis.DTOs
{
    // ── لعرض القائمة (Index + DataTable) ──
    public class DiagnosisListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public DiagnosisCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public bool IsLaboratoryRelated { get; set; }
    }

    // ── فورم موحّد للإضافة والتعديل (Id = 0 => إضافة، Id > 0 => تعديل) ──
    public class DiagnosisFormDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public DiagnosisCategory Category { get; set; } = DiagnosisCategory.Caries;
        public decimal Price { get; set; } = 0;
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsLaboratoryRelated { get; set; }
    }

    // ── نسخة خفيفة تتبعت للـ JS في صفحة السيشن (تحتوي على السعر) ──
    public class DiagnosisLookupDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}