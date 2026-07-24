using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class Tag : BaseEntity
    {
        public string Code { get; set; } = null!;


        public string NameAr { get; set; } = null!;


        public string? NameEn { get; set; }


        public string? Color { get; set; }


        public bool IsActive { get; set; } = true;


        public virtual ICollection<PatientTag> PatientTags { get; set; }
            = new HashSet<PatientTag>();
    }
}