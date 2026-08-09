using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Clinical.DTOs
{
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
}