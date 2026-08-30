
using Dental.Models.Common;

namespace Dental.Models.Patient
    {
        public class PatientXRay : BaseEntity
    {

            public int PatientId { get; set; }
            public Patient Patient { get; set; } = null!;

            public int? VisitId { get; set; }
            public Visit? Visit { get; set; }

            public string FileName { get; set; } = null!;
            public string FilePath { get; set; } = null!; // path on disk/blob, NOT binary
            public DateTime TakenDate { get; set; }
            public string? Notes { get; set; }

        public int? ToothNumber { get; set; }
    }
    }