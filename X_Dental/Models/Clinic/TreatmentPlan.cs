using static Dental.Constants.EnumHelper;

namespace Dental.Models.Clinic
{
    public class TreatmentPlan
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? VisitId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ApprovedDate { get; set; }

        public string? CreatedBy { get; set; }

        public List<TreatmentPlanStep> Steps { get; set; } = new();

        // ⭐ Computed helpers (not mapped)
        public decimal TotalEstimatedCost => Steps?.Sum(s => s.EstimatedCost) ?? 0;
        public int TotalSteps => Steps?.Count ?? 0;
        public int CompletedSteps => Steps?.Count(s => s.Status == TreatmentStepStatus.Done) ?? 0;
    }
}