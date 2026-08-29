//==================================================
// Modules / Clinical / DTOs
//==================================================

using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Clinical.DTOs
{
    // ============================================================
    // طلب الحفظ (القديم — لسه مستخدم في أماكن تانية، سيبناه زي ما هو)
    // ============================================================
    public class SaveClinicalRecordRequest
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public List<ToothRecordRequest> ToothRecords { get; set; } = new();
    }

    // ⭐ عدّلنا فيها: استبدلنا حقول الأشعة المفردة بقائمة تدعم أكتر من ملف
    public class ToothRecordRequest
    {
        public int ToothNumber { get; set; }
        public Dental.Models.Patient.ToothStatus Status { get; set; }
        public List<ToothSurfaceRequest> Surfaces { get; set; } = new();
        public string? ProcedureCode { get; set; }
        public string? Notes { get; set; }

        public int? DiagnosisCatalogId { get; set; }
        public string? DiagnosisSurface { get; set; }
        public string? DiagnosisNotes { get; set; }
        public bool DiagnosisIsPrimary { get; set; } = true;

        // كانت: XRayFileName / XRayBase64 / XRayType / XRayNotes (حقول مفردة)
        public List<XRayItemRequest> XRays { get; set; } = new();

        public List<PrescriptionItemRequest> PrescriptionItems { get; set; } = new();
    }

    // ⭐ جديد
    public class XRayItemRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string Base64 { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    // ⭐ جديد: طلب الحفظ النهائي الموحّد
    public class FinishSessionRequest
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public List<ToothRecordRequest> ToothRecords { get; set; } = new();
        public List<PendingPaymentRequest> Payments { get; set; } = new();
    }

    // ⭐ جديد
    public class PendingPaymentRequest
    {
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
    }

    // ── Diagnosis Request (قديم) ──
    public class AddDiagnosisRequest
    {
        public int VisitId { get; set; }
        public int DiagnosisId { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; } = false;
    }

    // ── Procedure Request (قديم) ──
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

    // ── Payment Request (قديم) ──
    public class AddPaymentRequest
    {
        public int VisitId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
    }

    // ── Dental Chart Request (قديم) ──
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

    public class PreviousSessionRecordDto
    {
        public int ToothNumber { get; set; }
        public DateTime Date { get; set; }
        public string StatusText { get; set; } = "";
        public string? DiagnosisName { get; set; }
        public string? DiagnosisNotes { get; set; }
        public decimal DiagnosisPrice { get; set; }
        public decimal VisitTotalAmount { get; set; }
        public decimal VisitPaidAmount { get; set; }
        public decimal VisitDiscount { get; set; }
        public decimal VisitRemaining { get; set; }
    }
}