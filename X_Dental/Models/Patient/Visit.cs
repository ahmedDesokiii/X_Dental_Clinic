using Dental.Models.Common;
using Dental.Modules.Patients.Enums;

namespace Dental.Models.Patient
{
    public class Visit : BaseEntity
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public VisitCategory Category { get; set; }
        public DateTime VisitDate { get; set; }
        public DateTime? NextVisitDate { get; set; }
        public int? TreatmentCatalogId { get; set; }
        public TreatmentCatalog? TreatmentCatalog { get; set; }
        public string? TreatmentDetails { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal RemainingAmount => RequiredAmount - (PaidAmount + Discount);
        public FollowUpType FollowUpType { get; set; } = FollowUpType.Normal;
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}