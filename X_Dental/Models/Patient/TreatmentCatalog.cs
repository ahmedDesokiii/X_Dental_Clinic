using Dental.Models.Common;
using Dental.Modules.Patients.Enums;

namespace Dental.Models.Patient
{
    public class TreatmentCatalog : BaseEntity
    {
        public string Name { get; set; } = null!;
        public decimal DefaultCost { get; set; }
        public VisitCategory? Category { get; set; } // null = shared between both
        public bool IsActive { get; set; } = true;
    }
}