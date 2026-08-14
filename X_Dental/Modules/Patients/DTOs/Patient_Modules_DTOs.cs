using Dental.Models.Clinic;
using Dental.Models.Patient;

namespace Dental.Modules.Patients.DTOs
{

    // ============================================================
    // 📁 File: Dental.Modules.Patients.DTOs
    // ============================================================

    // ── Base DTOs ──

    public class SessionDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime SessionDate { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public SessionStatus Status { get; set; } = SessionStatus.Completed;
        public decimal Cost { get; set; }
        public int? ToothNumber { get; set; }
        public string DentistName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class XRayDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public XRayType Type { get; set; } = XRayType.Panoramic;
        public string Description { get; set; } = string.Empty;
        public DateTime TakenDate { get; set; }
        public string TakenBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PrescriptionDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public List<PrescriptionItemDto> Items { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public string PrescribedBy { get; set; } = string.Empty;
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;
        public DateTime CreatedAt { get; set; }
    }

    public class PrescriptionItemDto
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;       // e.g. "500mg"
        public string Frequency { get; set; } = string.Empty;    // e.g. "3 times daily"
        public string Duration { get; set; } = string.Empty;       // e.g. "7 days"
        public string Instructions { get; set; } = string.Empty;   // e.g. "After meals"
    }

    public class PhotoDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public PhotoType Type { get; set; } = PhotoType.Intraoral;
        public string Category { get; set; } = string.Empty;      // e.g. "Before", "After"
        public string Description { get; set; } = string.Empty;
        public DateTime TakenDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? SessionId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;
        public string Description { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DocumentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FileExtension { get; set; }
        public long? FileSize { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    // ── Enums ──

    public enum SessionStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Postponed
    }

    public enum XRayType
    {
        Panoramic,        // OPG
        Periapical,       // Single tooth
        Bitewing,         // Crown check
        Occlusal,         // Floor of mouth
        Cephalometric,    // Orthodontic
        CBCT              // 3D scan
    }

    public enum PrescriptionStatus
    {
        Active,
        Completed,
        Cancelled
    }

    public enum PhotoType
    {
        Portrait,         // Face photo
        Intraoral,        // Inside mouth
        Extraoral,        // Outside mouth
        XRay,             // Radiograph (duplicate for gallery)
        Smile,            // Smile line
        Profile           // Side view
    }

    public enum PaymentMethod
    {
        Cash,
        CreditCard,
        DebitCard,
        BankTransfer,
        Insurance,
        Installment
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        PartiallyPaid,
        Refunded,
        Cancelled
    }

    // ── Dental Chart DTOs ──

    public class DentalChartDto
    {
        public int PatientId { get; set; }
        public List<ToothDto> Teeth { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class ToothDto
    {
        public int ToothNumber { get; set; }          // FDI notation: 11-18, 21-28, 31-38, 41-48
        public string ToothName { get; set; } = string.Empty;
        public ToothStatus Status { get; set; } = ToothStatus.Healthy;
        public List<ToothSurfaceDto> Surfaces { get; set; } = new();
        public List<ToothTreatmentDto> Treatments { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }

    public class ToothSurfaceDto
    {
        public ToothSurface Surface { get; set; }
        public SurfaceCondition Condition { get; set; } = SurfaceCondition.Healthy;
        public string Notes { get; set; } = string.Empty;
    }

    public class ToothTreatmentDto
    {
        public int Id { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public DateTime TreatmentDate { get; set; }
        public string DentistName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    //public enum ToothStatus
    //{
    //    Healthy,
    //    Decayed,          // Cavity
    //    Treated,          // Filled/Root canal
    //    Missing,
    //    Extracted,
    //    Impacted,
    //    Crown,
    //    Bridge,
    //    Implant,
    //    Fractured
    //}

    public enum ToothSurface
    {
        Mesial,           // Front side
        Distal,           // Back side
        Buccal,           // Cheek side
        Lingual,          // Tongue side
        Occlusal,         // Biting surface
        Incisal           // Cutting edge
    }

    public enum SurfaceCondition
    {
        Healthy,
        Caries,
        Filling,
        Crown,
        RootCanal,
        ExtractionNeeded
    }
}
