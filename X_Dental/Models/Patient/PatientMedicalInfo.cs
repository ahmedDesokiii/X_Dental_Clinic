using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class PatientMedicalInfo : BaseEntity
    {
        public int PatientId { get; set; }


        public string? Allergies { get; set; }


        public string? ChronicDiseases { get; set; }


        public string? PreviousSurgeries { get; set; }


        public string? CurrentMedications { get; set; }


        public string? MedicalNotes { get; set; }


        public string? SmokingStatus { get; set; }


        public bool IsPregnant { get; set; }


        public string? EmergencyNotes { get; set; }



        // Navigation

        public virtual Patient Patient { get; set; } = null!;
    }
}