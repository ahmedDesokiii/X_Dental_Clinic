using System.ComponentModel.DataAnnotations.Schema;
using Dental.Models.Common;
using Dental.Models.Clinic;
using Dental.Modules.Patients.Enums;

namespace Dental.Models.Patient
{
    public class Visit : BaseEntity
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public DateTime VisitDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? Notes { get; set; }

        public FollowUpType FollowUpType { get; set; } = FollowUpType.Normal;
        public DateTime? FollowUpDate { get; set; }

        public VisitStatus Status { get; set; } = VisitStatus.InProgress;
        public VisitCategory Category { get; set; } = VisitCategory.General;

        public bool IsActive { get; set; } = true;

        // Financial
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        [NotMapped]
        public int Remaining => (int)(TotalAmount - Discount - PaidAmount);

        // Navigation
        public ICollection<VisitDiagnosis> Diagnoses { get; set; } = new List<VisitDiagnosis>();
        public ICollection<VisitProcedure> Procedures { get; set; } = new List<VisitProcedure>();
        public ICollection<VisitPayment> Payments { get; set; } = new List<VisitPayment>();
        public ICollection<PatientXRay> XRays { get; set; } = new List<PatientXRay>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        //public ICollection<TreatmentSession> TreatmentSessions { get; set; } = new List<TreatmentSession>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}