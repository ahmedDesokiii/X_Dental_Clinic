// ============================================================
// 📁 File: PatientIndexViewModel.cs
// ============================================================

using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Patients.ViewModels
{
    public class PatientIndexViewModel
    {
        public List<PatientDto> Patients { get; set; } = new();
        public string? Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public int TotalPatients { get; set; }
        public int NewThisMonth { get; set; }
        public int ActiveToday { get; set; }
    }
}