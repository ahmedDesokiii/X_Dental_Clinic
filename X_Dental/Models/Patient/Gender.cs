using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class Gender : BaseEntity
    {
        public string Code { get; set; } = null!;


        public string NameAr { get; set; } = null!;


        public string? NameEn { get; set; }


        public bool IsActive { get; set; } = true;
    }
}