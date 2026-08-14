// ============================================================
// 📁 File: PatientProfileViewModel.cs
// ============================================================

using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Patients.ViewModels
{
    public class PatientProfileViewModel
    {
        public PatientDetailsDto Patient { get; set; } = null!;
        public int VisitsCount { get; set; }
        public int AppointmentsCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public DentalChartDto? DentalChart { get; set; }
        public List<SessionDto> Sessions { get; set; } = new();
        public List<XRayDto> XRays { get; set; } = new();
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
        public List<PhotoDto> Photos { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
        public List<DocumentDto> Documents { get; set; } = new();
        public decimal TotalCost => Payments?.Sum(p => p.Amount) ?? 0;
        public decimal BalanceDue => TotalCost - TotalPaid;
        public bool HasAllergies => !string.IsNullOrEmpty(Patient?.Allergies);
        public bool HasChronicDiseases => !string.IsNullOrEmpty(Patient?.ChronicDiseases);
        public bool HasPendingPayments => Payments?.Any(p => p.Status == PaymentStatus.Pending) ?? false;
        public int CompletedSessions => Sessions?.Count(s => s.Status == SessionStatus.Completed) ?? 0;
        public int UpcomingAppointments => AppointmentsCount;
        public bool HasUpcomingAppointment => Patient?.NextAppointment.HasValue ?? false;

    }
}