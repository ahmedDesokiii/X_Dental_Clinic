// ============================================================
// 📁 File: Models/Clinic/OrthoModels.cs  (ملف جديد)
// موديلز التقويم — جداول جديدة بس، مفيش تعديل على أي جدول موجود
// ============================================================
namespace Dental.Models.Clinic
{
    /// <summary>الفك — Flags عشان الحالة تقدر تبقى Upper / Lower / Both</summary>
    [Flags]
    public enum OrthoArch
    {
        None = 0,
        Upper = 1,
        Lower = 2,
        Both = Upper | Lower
    }

    public enum OrthoCaseStatus
    {
        Active = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>حالة تقويم واحدة للمريض (اتفاق إجمالي واحد على فك أو الاتنين)</summary>
    public class OrthoCase
    {
        public int Id { get; set; }
        public int PatientId { get; set; }

        public OrthoArch ArchType { get; set; }

        /// <summary>الإجمالي المتفق عليه — بيتخزن كمان في Anchor Visit.TotalAmount</summary>
        public decimal TotalRequired { get; set; }

        /// <summary>الزيارة اللي بتشيل المبلغ المطلوب وكل المدفوعات (عشان التقارير الحالية تفضل شغالة)</summary>
        public int AnchorVisitId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public OrthoCaseStatus Status { get; set; } = OrthoCaseStatus.Active;
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public List<OrthoVisitEntry> Entries { get; set; } = new();
        public List<OrthoTotalChange> TotalChanges { get; set; } = new();
    }

    /// <summary>تسجيل فك واحد في زيارة واحدة (تشخيص تقويم + Comment)</summary>
    public class OrthoVisitEntry
    {
        public int Id { get; set; }
        public int OrthoCaseId { get; set; }
        public OrthoCase? OrthoCase { get; set; }

        public int VisitId { get; set; }

        /// <summary>Upper أو Lower بس (مش Both)</summary>
        public OrthoArch Arch { get; set; }

        public int? OrthoProcedureId { get; set; }
        public string? Comment { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; }
    }

    /// <summary>كتالوج تشخيصات/إجراءات التقويم — من غير أسعار (الاتفاق شامل)</summary>
    public class OrthoProcedureCatalog
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    /// <summary>سجل تعديلات الإجمالي (Audit)</summary>
    public class OrthoTotalChange
    {
        public int Id { get; set; }
        public int OrthoCaseId { get; set; }
        public OrthoCase? OrthoCase { get; set; }

        public decimal OldTotal { get; set; }
        public decimal NewTotal { get; set; }
        public string? Reason { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}