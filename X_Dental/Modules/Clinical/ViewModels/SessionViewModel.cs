using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Clinical.ViewModels
{
    public class SessionViewModel
    {
        public PatientDetailsDto Patient { get; set; } = null!;
        public VisitDto Visit { get; set; } = null!;
        public List<DentalChartEntry> DentalHistory { get; set; } = new();
        public List<DiagnosisCatalog> AvailableDiagnoses { get; set; } = new();
        public List<ProcedureCatalog> AvailableProcedures { get; set; } = new();
    }
}