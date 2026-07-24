using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class PatientTag : BaseEntity
    {
        public int PatientId { get; set; }


        public int TagId { get; set; }



        // Navigation

        public virtual Patient Patient { get; set; } = null!;


        public virtual Tag Tag { get; set; } = null!;
    }
}