using Dental.Models.Clinic;
using Dental.Models.Common;
using Dental.Modules.Patients.Enums;
using static Dental.Constants.EnumHelper;

namespace Dental.Models.Patient
{// ========== Visit (السيشن الجديد) ==========
    public class Visit : BaseEntity
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public DateTime VisitDate { get; set; } = DateTime.Now;
        public string? ChiefComplaint { get; set; } // الشكوى الرئيسية
        public string? Examination { get; set; } // الفحص
        public string? Notes { get; set; }

        public FollowUpType FollowUpType { get; set; } = FollowUpType.Normal;
        public DateTime? FollowUpDate { get; set; }
        public VisitStatus Status { get; set; } = VisitStatus.InProgress;

        public bool IsActive { get; set; } = true;
        public VisitCategory Category { get; set; } = VisitCategory.General;


        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining => TotalAmount - Discount - PaidAmount;

        // Navigation
        public ICollection<VisitDiagnosis> Diagnoses { get; set; } = new List<VisitDiagnosis>();
        public ICollection<VisitProcedure> Procedures { get; set; } = new List<VisitProcedure>();
        public ICollection<VisitPayment> Payments { get; set; } = new List<VisitPayment>();
        public ICollection<PatientXRay> XRays { get; set; } = new List<PatientXRay>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}