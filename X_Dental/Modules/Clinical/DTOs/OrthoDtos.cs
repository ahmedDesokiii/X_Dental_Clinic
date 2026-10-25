// ============================================================
// 📁 File: Modules/Clinical/DTOs/OrthoDtos.cs  (ملف جديد)
// ============================================================
using Dental.Models.Clinic;

namespace Dental.Modules.Clinical.DTOs
{
    // ── Requests ──
    public class OrthoCreateCaseRequest
    {
        public int PatientId { get; set; }
        /// <summary>الزيارة الحالية (السيشن المفتوح) — هتبقى الـ Anchor</summary>
        public int VisitId { get; set; }
        public OrthoArch Arch { get; set; }
        public decimal TotalRequired { get; set; }
        public string? Notes { get; set; }
    }

    public class OrthoUpdateCaseRequest
    {
        public int OrthoCaseId { get; set; }
        /// <summary>null = من غير تغيير</summary>
        public OrthoArch? Arch { get; set; }
        /// <summary>null = من غير تغيير</summary>
        public decimal? TotalRequired { get; set; }
        /// <summary>سبب تعديل الإجمالي (بيتسجل)</summary>
        public string? Reason { get; set; }
    }

    public class OrthoSessionEntryInput
    {
        public OrthoArch Arch { get; set; }          // Upper أو Lower
        public int? OrthoProcedureId { get; set; }
        public string? Comment { get; set; }
    }

    public class OrthoSaveSessionRequest
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public List<OrthoSessionEntryInput> Entries { get; set; } = new();
    }

    // ── Responses ──
    public class OrthoEntryDto
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public DateTime Date { get; set; }
        public OrthoArch Arch { get; set; }
        public string ArchText { get; set; } = string.Empty;
        public int? OrthoProcedureId { get; set; }
        public string? ProcedureName { get; set; }
        public string? Comment { get; set; }
    }

    public class OrthoTotalChangeDto
    {
        public DateTime ChangedAt { get; set; }
        public decimal OldTotal { get; set; }
        public decimal NewTotal { get; set; }
        public string? Reason { get; set; }
    }

    public class OrthoCaseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public OrthoArch ArchType { get; set; }
        public string ArchText { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int AnchorVisitId { get; set; }

        public decimal TotalRequired { get; set; }
        public decimal Paid { get; set; }
        public decimal Discount { get; set; }
        public decimal Remaining { get; set; }

        public List<OrthoEntryDto> Entries { get; set; } = new();
        public List<OrthoTotalChangeDto> TotalChanges { get; set; } = new();
    }

    public class OrthoProcedureDto
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
    }
}