using Dental.Models.Common;
using Dental.Models.Patient;

namespace Dental.Models.Clinic
{
    public class VisitDiagnosis : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public int DiagnosisCatalogId { get; set; }
        public DiagnosisCatalog Diagnosis { get; set; } = null!;
        // null = سن واحد (الوضع الحالي). نفس القيمة = نفس المجموعة (Multi-Unit)
        public Guid? GroupId { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; } // M, D, B, L, O
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; } = false;
        // ⭐ NEW — snapshot لسعر التشخيص وقت تسجيله فعليًا في السيشن
        // (بيتسجل مرة واحدة وقت الإنشاء ومبيتغيرش لو سعر الكتالوج اتغير بعدين)
        public decimal? LabPriceSnapshot { get; set; }
    }
}
