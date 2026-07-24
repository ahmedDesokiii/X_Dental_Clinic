using Dental.Models.Common;
using System.Reflection;
namespace Dental.Models.Patient
{
    public class Patient : BaseEntity
    {
        public string PatientCode { get; set; } = null!;
        public string FullNameAr { get; set; } = null!;
        public string? FullNameEn {  get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public string Mobile { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;



        // Navigation

        public int? GenderId { get; set; }
        public virtual Gender? Gender { get; set; } = null!;
        
        public int? BloodGroupId { get; set; }
        public virtual BloodGroup? BloodGroup { get; set; }

        public virtual ICollection<PatientContact> Contacts { get; set; } = new HashSet<PatientContact>();
        public virtual PatientMedicalInfo? MedicalInfo { get; set; }
        public virtual ICollection<PatientDocument> Documents { get; set; } = new HashSet<PatientDocument>();
        public virtual ICollection<PatientTag> PatientTags { get; set; } = new HashSet<PatientTag>();

        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<PatientXRay> XRays { get; set; } = new List<PatientXRay>();
    }

}