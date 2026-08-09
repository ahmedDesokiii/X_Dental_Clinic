using Dental.Models.Common;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;

namespace Dental.Models.Clinic
{
    public class Prescription : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;

        public DateTime PrescriptionDate { get; set; } = DateTime.Now;
        public string? Diagnosis { get; set; }
        public string? Notes { get; set; }
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;

        public string? PrescribedBy { get; set; }         // Doctor Name
        public int? DoctorId { get; set; }

        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }

    public class PrescriptionItem : BaseEntity
    {
        public int PrescriptionId { get; set; }
        public Prescription Prescription { get; set; } = null!;

        public string MedicationName { get; set; } = null!;
        public string? Dosage { get; set; }              // 500mg
        public string? Frequency { get; set; }             // 3 times daily
        public string? Duration { get; set; }              // 7 days
        public string? Instructions { get; set; }          // After meals
        public int? Quantity { get; set; }
    }
}
