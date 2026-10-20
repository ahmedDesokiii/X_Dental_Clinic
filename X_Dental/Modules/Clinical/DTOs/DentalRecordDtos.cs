// ============================================================
// 📁 File: Modules/Clinical/DTOs/DentalRecordDtos.cs  (ملف جديد)
// تعديل الدينتال شارت من صفحة المريض (Details) — حفظ مباشر بدون Session
// ============================================================
using Dental.Modules.Patients.Enums;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Clinical.DTOs
{
    // ── Requests ──

    /// <summary>إضافة تشخيص/حالة على سن أو أكتر (Multi-Unit) → بتتعمل زيارة سريعة تلقائيًا.</summary>
    public class ChartAddRecordsRequest
    {
        public int PatientId { get; set; }
        /// <summary>للمريض "عام + تقويم" بس — غير كده بتتحدد تلقائيًا من نوع المريض.</summary>
        public VisitCategory? Category { get; set; }
        public List<int> Teeth { get; set; } = new();
        public int Status { get; set; } = 2;
        public int? DiagnosisCatalogId { get; set; }
        public string? Notes { get; set; }
        public List<ChartXRayInput> XRays { get; set; } = new();
    }

    public class ChartUpdateRecordRequest
    {
        public int VisitDiagnosisId { get; set; }
        public int DiagnosisCatalogId { get; set; }
        public string? Notes { get; set; }
        /// <summary>اختياري — تعديل حالة السن (Status) لنفس الزيارة.</summary>
        public int? Status { get; set; }
    }

    public class ChartDeleteRecordRequest
    {
        /// <summary>حذف تشخيص (لو جزء من مجموعة → المجموعة كلها).</summary>
        public int VisitDiagnosisId { get; set; }
        /// <summary>حذف سجل حالة سن بدون تشخيص (لو VisitDiagnosisId = 0).</summary>
        public int ChartEntryId { get; set; }
        public string? Reason { get; set; }
    }

    public class ChartAddXRaysRequest
    {
        public int PatientId { get; set; }
        public int ToothNumber { get; set; }
        public List<ChartXRayInput> XRays { get; set; } = new();
    }

    public class ChartXRayInput
    {
        public string FileName { get; set; } = string.Empty;
        public string Base64 { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    // ── Responses ──

    /// <summary>كل سجل السن (لمودال السن في صفحة المريض).</summary>
    public class ChartToothHistoryDto
    {
        public int ToothNumber { get; set; }
        public List<ChartToothRecordDto> Records { get; set; } = new();
        public List<ChartXRayDto> XRays { get; set; } = new();
    }

    public class ChartToothRecordDto
    {
        public int VisitDiagnosisId { get; set; }   // 0 = سجل حالة بدون تشخيص
        public int ChartEntryId { get; set; }
        public int VisitId { get; set; }
        public DateTime Date { get; set; }
        public int? DiagnosisCatalogId { get; set; }
        public string? DiagnosisName { get; set; }
        /// <summary>سعر السن الواحد (PriceSnapshot).</summary>
        public decimal Price { get; set; }
        public int Units { get; set; } = 1;
        public Guid? GroupId { get; set; }
        public List<int> GroupTeeth { get; set; } = new();
        public int? Status { get; set; }
        public string? StatusText { get; set; }
        public string? Notes { get; set; }
    }

    public class ChartXRayDto
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime TakenDate { get; set; }
    }
}