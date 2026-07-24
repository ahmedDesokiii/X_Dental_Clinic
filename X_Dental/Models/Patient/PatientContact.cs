using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class PatientContact : BaseEntity
    {
        public int PatientId { get; set; }


        public int ContactTypeId { get; set; }


        public string? Name { get; set; }


        public string Phone { get; set; } = null!;


        public string? Email { get; set; }


        public string? Address { get; set; }


        public bool IsPrimary { get; set; } = false;


        public bool IsEmergency { get; set; } = false;



        // Navigation

        public virtual Patient Patient { get; set; } = null!;
    }
}