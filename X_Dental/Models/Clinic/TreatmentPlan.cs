using Dental.Models.Clinic;
using Dental.Models.Common;
using Dental.Models.Patient;

namespace Dental.Models.Patient
{
    public enum PlanStatus { Draft = 1, Active = 2, Completed = 3, Cancelled = 4 }

    public class TreatmentPlan : BaseEntity
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public string? PlanCode { get; set; }
        public DateTime PlanDate { get; set; } = DateTime.Now;
        public PlanStatus Status { get; set; } = PlanStatus.Draft;
        public string? Notes { get; set; }
        public decimal TotalEstimatedCost { get; set; }

        public ICollection<TreatmentPlanItem> Items { get; set; } = new List<TreatmentPlanItem>();
        public ICollection<TreatmentSession> Sessions { get; set; } = new List<TreatmentSession>();
    }

    public class TreatmentPlanItem : BaseEntity
    {
        public int TreatmentPlanId { get; set; }
        public TreatmentPlan TreatmentPlan { get; set; } = null!;

        public int? ProcedureCatalogId { get; set; }
        public ProcedureCatalog? Procedure { get; set; }

        public int? ToothNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal? Discount { get; set; }
        public decimal FinalCost => EstimatedCost - (Discount ?? 0);
        public ProcedureStatus Status { get; set; } = ProcedureStatus.Planned;
        public int? Priority { get; set; }                // 1 = Highest
        public string? Notes { get; set; }
    }

    public class TreatmentSession : BaseEntity
    {
        public int TreatmentPlanId { get; set; }
        public TreatmentPlan TreatmentPlan { get; set; } = null!;

        public int? VisitId { get; set; }
        public Visit? Visit { get; set; }

        public DateTime SessionDate { get; set; }
        public string? SessionName { get; set; }          // "Session 1: Access"
        public string? Notes { get; set; }
        public bool IsCompleted { get; set; } = false;
    }

}
