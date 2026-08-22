using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Clinical.DTOs
{

    // ============================================================
    // الطلب الموحّد: بيوصف كل حاجة حصلت "للسنة" (السن) الواحدة
    // خالل السيشن قبل الحفظ النهائي
    // ============================================================
    public class SaveClinicalRecordRequest
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public List<ToothRecordRequest> ToothRecords { get; set; } = new();
    }

    public class ToothRecordRequest
    {
        // ---- بيانات السن / الدينتال شارت ----
        public int ToothNumber { get; set; }
        public Dental.Models.Patient.ToothStatus Status { get; set; }
        public List<ToothSurfaceRequest> Surfaces { get; set; } = new();
        public string? ProcedureCode { get; set; }
        public string? Notes { get; set; }

        // ---- التشخيص (اختياري) ----
        public int? DiagnosisCatalogId { get; set; }
        public string? DiagnosisSurface { get; set; }   // M,D,B,L,O
        public string? DiagnosisNotes { get; set; }
        public bool DiagnosisIsPrimary { get; set; } = true;

        // ---- الأشعة (اختياري) ----
        public string? XRayFileName { get; set; }
        public string? XRayBase64 { get; set; }         // بيانات الصورة Base64
        public string? XRayType { get; set; }
        public string? XRayNotes { get; set; }

        // ---- الروشتة الخاصة بالسن ده (اختياري) ----
        public List<PrescriptionItemRequest> PrescriptionItems { get; set; } = new();
    }



    // ── Diagnosis Request ──
    public class AddDiagnosisRequest
    {
        public int VisitId { get; set; }
        public int DiagnosisId { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; } = false;
    }

    // ── Procedure Request ──
    public class AddProcedureRequest
    {
        public int VisitId { get; set; }
        public int ProcedureId { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? Price { get; set; } // null = use catalog default
        public string? Notes { get; set; }
    }

    // ── Payment Request ──
    public class AddPaymentRequest
    {
        public int VisitId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
    }

    // ── Dental Chart Request ──
    public class SaveChartRequest
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public List<ToothChartRequest> Teeth { get; set; } = new();
    }

    public class ToothChartRequest
    {
        public int Number { get; set; }
        public ToothStatus Status { get; set; }
        public List<ToothSurfaceRequest> Surfaces { get; set; } = new();
        public string? ProcedureCode { get; set; }
        public string? Notes { get; set; }
    }

    public class ToothSurfaceRequest
    {
        public string Surface { get; set; } = string.Empty; // M, D, B, L, O
        public string Condition { get; set; } = string.Empty; // Healthy, Caries...
    }

    public class CreateAppointmentRequest
    {
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int Duration { get; set; } = 30; // minutes
        public string? Reason { get; set; }
    }

    public class AddPrescriptionRequest
    {
        public int VisitId { get; set; }
        public string? Diagnosis { get; set; }
        public string? Notes { get; set; }
        public string? PrescribedBy { get; set; }
        public List<PrescriptionItemRequest> Items { get; set; } = new();
    }

    public class PrescriptionItemRequest
    {
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }
}