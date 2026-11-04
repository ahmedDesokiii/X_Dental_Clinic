// ============================================================
// 📁 File: Modules/Appointments/DTOs/AppointmentDtos.cs
// ============================================================
using Dental.Models.Patient;
using Dental.Modules.Patients.Enums;

namespace Dental.Modules.Appointments.DTOs
{
    // ── يُستخدم لتغذية الكالندر (Weekly/Monthly view) ──
    public class AppointmentCalendarDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientMobile { get; set; } = null!;
        public PatientType PatientType { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int DurationMinutes { get; set; }

        public AppointmentStatus Status { get; set; }
        public string StatusText { get; set; } = null!;
        public string StatusColorClass { get; set; } = null!; // للاستخدام في الـ badge/border

        public string? Reason { get; set; }
    }

    // ── تفاصيل موعد واحد (للـ Modal / التعديل) ──
    public class AppointmentDetailsDto : AppointmentCalendarDto
    {
        public string? Notes { get; set; }
        public string? DoctorName { get; set; }
        public int? VisitId { get; set; }
        public bool IsReminderSent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── قائمة مبسطة (مواعيد اليوم / مواعيد مريض معين) ──
    public class AppointmentListDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientMobile { get; set; } = null!;
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; }
        public AppointmentStatus Status { get; set; }
        public string StatusText { get; set; } = null!;
        public string? Reason { get; set; }
    }

    // ── إنشاء موعد جديد ──
    public class CreateAppointmentDto
    {
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public string? Reason { get; set; }
        public string? Notes { get; set; }

        // لو المريض عنده موعد في نفس اليوم، السيرفر بيرجع تحذير؛ الفرونت يبعت true بعد تأكيد المستخدم
        public bool AllowSameDay { get; set; }
    }

    // ── تعديل بيانات الموعد (بدون تغيير الميعاد نفسه) ──
    public class UpdateAppointmentDto
    {
        public int Id { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    // ── تأجيل/تحريك الموعد لتاريخ أو مدة تانية ──
    public class RescheduleAppointmentDto
    {
        public int Id { get; set; }
        public DateTime NewAppointmentDate { get; set; }
        public int DurationMinutes { get; set; } = 30;
    }

    // ── فحص التعارض (يُستخدم أثناء الكتابة في الفورم قبل الحفظ) ──
    public class ConflictCheckRequestDto
    {
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public int? ExcludeId { get; set; }
    }

    // ── نتيجة تحويل الموعد إلى زيارة (محتاجين الاتنين عشان نوجّه الفرونت للصفحة الصح) ──
    public class ConvertToVisitResultDto
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
    }
}
