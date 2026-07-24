using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class BloodGroup : BaseEntity
    {
        public string Code { get; set; } = null!;


        public string Name { get; set; } = null!;


        public bool IsActive { get; set; } = true;
    }
}