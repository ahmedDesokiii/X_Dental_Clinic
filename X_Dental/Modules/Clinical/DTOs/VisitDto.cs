using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Clinical.DTOs
{
    public class VisitDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime VisitDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? Notes { get; set; }
        public FollowUpType FollowUpType { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public VisitStatus Status { get; set; }

        public VisitCategory Category { get; set; }
        // Financial
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining { get; set; }

        // Related Data
        public List<VisitDiagnosisDto> Diagnoses { get; set; } = new();
        public List<VisitProcedureDto> Procedures { get; set; } = new();
        public List<VisitPaymentDto> Payments { get; set; } = new();
        public List<XRayDto> XRays { get; set; } = new();
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
    }

    public class VisitDiagnosisDto
    {
        public int Id { get; set; }
        public int DiagnosisCatalogId { get; set; }
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisCode { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class VisitProcedureDto
    {
        public int Id { get; set; }
        public int ProcedureCatalogId { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string? ProcedureCode { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public ProcedureStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class VisitPaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? ReceiptNo { get; set; }
    }
}