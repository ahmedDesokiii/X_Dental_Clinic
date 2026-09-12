using static Dental.Constants.EnumHelper;

namespace Dental.Models.Clinic
{
    public class TreatmentPlanStep
    {
        public int Id { get; set; }
        public int TreatmentPlanId { get; set; }
        public TreatmentPlan? TreatmentPlan { get; set; }

        public int StepOrder { get; set; }
        public string Description { get; set; } = string.Empty;

        public int? ProcedureCatalogId { get; set; }
        public ProcedureCatalog? ProcedureCatalog { get; set; }

        public int? ToothNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime? TargetDate { get; set; }

        public TreatmentStepStatus Status { get; set; } = TreatmentStepStatus.Planned;

        public int? CompletedVisitId { get; set; }   // ربط بالزيارة اللي اتنفذت فيها فعلياً
        public DateTime? CompletedDate { get; set; }

        public string? Notes { get; set; }
    }
}