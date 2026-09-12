using static Dental.Constants.EnumHelper;

namespace Dental.Models.Clinic
{
    public class MedicationCatalog
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public MedicationCategory Category { get; set; }
        public string? DefaultDosage { get; set; }      // "500mg"
        public string? DefaultFrequency { get; set; }   // "3 مرات يوميًا"
        public string? DefaultDuration { get; set; }    // "5 أيام"
        public bool IsActive { get; set; } = true;
    }
}