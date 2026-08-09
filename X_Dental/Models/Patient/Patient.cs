using Dental.Models.Clinic;
using Dental.Models.Common;
using System.Reflection;
using static Dental.Constants.EnumHelper;
namespace Dental.Models.Patient
{
    public enum PatientType { General = 1, Orthodontics = 2, Both = 3 }
    //public enum VisitStatus { InProgress = 1, Completed = 2, Cancelled = 3, NoShow = 4 }
    public enum ProcedureStatus { Planned = 1, InProgress = 2, Completed = 3 }
    public enum DiagnosisCategory { Caries = 1, Periodontal = 2, Endodontic = 3, Prosthodontic = 4, Orthodontic = 5, Other = 6 }
    public enum AppointmentStatus { Scheduled = 1, Confirmed = 2, CheckedIn = 3, InProgress = 4, Completed = 5, Cancelled = 6, NoShow = 7 }
    //public enum PaymentMethod { Cash = 1, CreditCard = 2, DebitCard = 3, BankTransfer = 4, Insurance = 5, Installment = 6 }

        public class Patient : BaseEntity
        {
            public string PatientCode { get; set; } = null!;
            public string FullNameAr { get; set; } = null!;
            public string? FullNameEn { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public string? NationalId { get; set; }
            public string Mobile { get; set; } = null!;
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? Occupation { get; set; }
            public string? Notes { get; set; }
            public bool IsActive { get; set; } = true;

            // ⭐ NEW: نوع المريض (عام / تقويم)
            public PatientType PatientType { get; set; } = PatientType.General;
            public string? FileNo { get; set; }

            // Navigation
            public int? GenderId { get; set; }
            public virtual Gender? Gender { get; set; }

            public int? BloodGroupId { get; set; }
            public virtual BloodGroup? BloodGroup { get; set; }

            public virtual ICollection<PatientContact> Contacts { get; set; } = new HashSet<PatientContact>();
            public virtual PatientMedicalInfo? MedicalInfo { get; set; }
            public virtual ICollection<PatientDocument> Documents { get; set; } = new HashSet<PatientDocument>();
            public virtual ICollection<PatientTag> PatientTags { get; set; } = new HashSet<PatientTag>();

            // ⭐ Clinical & Financial
            public ICollection<Visit> Visits { get; set; } = new List<Visit>();
            public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
            public ICollection<PatientXRay> XRays { get; set; } = new List<PatientXRay>();
            public ICollection<DentalChartEntry> DentalHistory { get; set; } = new List<DentalChartEntry>();
            public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
        }
    }