    using Dental.Models.Common;
    using Dental.Models.Patient;
    using Dental.Modules.Patients.DTOs;

    namespace Dental.Models.Patient
    {

        public enum ToothStatus
        {
            Healthy = 1, Decayed = 2, Treated = 3, Missing = 4,
            Extracted = 5, Impacted = 6, Crown = 7, Bridge = 8,
            Implant = 9, Fractured = 10, RootCanal = 11, Filling = 12
        }

        public class DentalChartEntry : BaseEntity
        {
            public int PatientId { get; set; }
            public Patient Patient { get; set; } = null!;

            public int? VisitId { get; set; }
            public Visit? Visit { get; set; }

            public int ToothNumber { get; set; }              // FDI: 11-18, 21-28...
            public ToothStatus Status { get; set; } = ToothStatus.Healthy;

            // JSON: [{"surface":"M","condition":"Caries"},{"surface":"O","condition":"Filling"}]
            public string? SurfacesJson { get; set; }

            public string? ProcedureCode { get; set; }        // CRN, RCT...
            public string? Notes { get; set; }
            public DateTime Date { get; set; } = DateTime.Now;
        }
    }
