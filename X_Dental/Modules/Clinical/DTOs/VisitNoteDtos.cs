// ============================================================
// 📁 File: Modules/Clinical/DTOs/VisitNoteDtos.cs  (ملف جديد — المرحلة 2)
// ============================================================
namespace Dental.Modules.Clinical.DTOs
{
    public class VisitNoteDto
    {
        public string Id { get; set; } = string.Empty;   // معرّف الملاحظة جوه الزيارة
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string Text { get; set; } = string.Empty;

        // التشخيص المرتبط (لو موجود)
        public int? LinkedVisitDiagnosisId { get; set; }
        public int LinkedToothNumber { get; set; }          // 0 = بدون سن
        public string? LinkedDiagnosisName { get; set; }
        public DateTime? LinkedDiagnosisDate { get; set; }
    }

    // تشخيص سابق ممكن نربط عليه ملاحظة
    public class LinkableDiagnosisDto
    {
        public int Id { get; set; }                 // VisitDiagnosisId
        public int VisitId { get; set; }
        public DateTime Date { get; set; }
        public int ToothNumber { get; set; }        // 0 = بدون سن
        public string DiagnosisName { get; set; } = string.Empty;
        public int NotesCount { get; set; }         // عدد المتابعات اللي اتسجلت عليه قبل كده
    }

    public class SaveVisitNoteRequest
    {
        public string? Id { get; set; }             // null = إضافة، قيمة = تعديل
        public int VisitId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int? LinkedVisitDiagnosisId { get; set; }
    }
}
