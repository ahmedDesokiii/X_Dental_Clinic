
    // Models/Patient/Appointment.cs
    using global::Dental.Models.Common;
    using global::Dental.Models.Patient;

    namespace Dental.Models.Patient
    {
        public class Appointment : BaseEntity
        {
            public int PatientId { get; set; }
            public Patient Patient { get; set; } = null!;

            public DateTime AppointmentDate { get; set; }
            public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);

            public string? Reason { get; set; }               // سبب الزيارة
            public string? Notes { get; set; }
            public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

            // If converted to Visit
            public int? VisitId { get; set; }
            public Visit? Visit { get; set; }

            // Doctor/Clinic
            public int? DoctorId { get; set; }
            public string? DoctorName { get; set; }
            public int? ClinicId { get; set; }

            // Reminder
            public bool IsReminderSent { get; set; } = false;
            public DateTime? ReminderDate { get; set; }
        }
    }
