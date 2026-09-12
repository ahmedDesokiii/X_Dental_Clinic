using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Clinical.DTOs
{
    public class TreatmentPlanDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? VisitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public TreatmentPlanStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColorClass { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int ProgressPercent { get; set; }
        public List<TreatmentPlanStepDto> Steps { get; set; } = new();
    }

    public class TreatmentPlanStepDto
    {
        public int Id { get; set; }
        public int TreatmentPlanId { get; set; }
        public int StepOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? ProcedureCatalogId { get; set; }
        public string? ProcedureName { get; set; }
        public int? ToothNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime? TargetDate { get; set; }
        public TreatmentStepStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColorClass { get; set; } = string.Empty;
        public int? CompletedVisitId { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Notes { get; set; }
    }

    // ── Requests ──
    public class CreateTreatmentPlanRequest
    {
        public int PatientId { get; set; }
        public int? VisitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<CreateTreatmentPlanStepRequest> Steps { get; set; } = new();
    }

    public class CreateTreatmentPlanStepRequest
    {
        public string Description { get; set; } = string.Empty;
        public int? ProcedureCatalogId { get; set; }
        public int? ToothNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? Notes { get; set; }
    }

    public class AddStepRequest
    {
        public int TreatmentPlanId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? ProcedureCatalogId { get; set; }
        public int? ToothNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateStepStatusRequest
    {
        public int StepId { get; set; }
        public TreatmentStepStatus Status { get; set; }
        public int? CompletedVisitId { get; set; }
    }

    public class UpdatePlanStatusRequest
    {
        public int PlanId { get; set; }
        public TreatmentPlanStatus Status { get; set; }
    }

    public class ReorderStepsRequest
    {
        public int PlanId { get; set; }
        public List<int> OrderedStepIds { get; set; } = new();
    }
}