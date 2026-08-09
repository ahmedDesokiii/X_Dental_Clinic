using Dental.Models.Common;
using Dental.Models.Patient;

namespace Dental.Models.Clinic
{
    public class VisitDiagnosis : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public int DiagnosisCatalogId { get; set; }
        public DiagnosisCatalog Diagnosis { get; set; } = null!;
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; } // M, D, B, L, O
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; } = false;
    }
}
