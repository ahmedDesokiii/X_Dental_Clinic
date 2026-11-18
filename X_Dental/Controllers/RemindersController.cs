using Dental.Core.Settings;
using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Dental.Controllers
{
    /// <summary>
    /// 💬 D3 — تذكير المواعيد على واتساب.
    /// بيستخدم روابط wa.me (مجانية، من غير WhatsApp Business API):
    /// الضغطة بتفتح واتساب على رقم المريض والرسالة جاهزة، والموظف يدوس إرسال.
    /// الصلاحيات: نفس صلاحيات المواعيد (عرض = Appointments.View، تسجيل الإرسال = Appointments.Manage).
    /// </summary>
    public class RemindersController : Controller
    {
        private static readonly CultureInfo Ar = new("ar-EG");

        private readonly ApplicationDbContext _context;
        private readonly IClinicSettingsService _settings;

        public RemindersController(ApplicationDbContext context, IClinicSettingsService settings)
        {
            _context = context;
            _settings = settings;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetList(DateTime? date)
        {
            var day = (date ?? DateTime.Today.AddDays(1)).Date;
            var next = day.AddDays(1);
            var s = _settings.Get();

            var rows = await _context.Appointments
                .AsNoTracking()
                .Where(a => !a.IsDeleted
                         && a.AppointmentDate >= day && a.AppointmentDate < next
                         && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed))
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.Id,
                    a.PatientId,
                    a.AppointmentDate,
                    a.Status,
                    a.Reason,
                    a.Notes,
                    a.IsReminderSent,
                    a.ReminderDate,
                    PatientName = a.Patient.FullNameAr,
                    a.Patient.PatientCode,
                    Mobile = a.Patient.Mobile ?? a.Patient.Phone
                })
                .ToListAsync();

            var data = rows.Select(a =>
            {
                var phone = WhatsAppNumber(a.Mobile, s.WhatsAppCountryCode);
                var message = BuildMessage(s, a.PatientName, a.AppointmentDate);
                return new
                {
                    a.Id,
                    a.PatientId,
                    a.PatientName,
                    a.PatientCode,
                    a.Mobile,
                    Time = HasRealTime(a.AppointmentDate) ? a.AppointmentDate.ToString("h:mm tt", Ar) : null,
                    Status = a.Status.ToString(),
                    a.Reason,
                    a.Notes,
                    a.IsReminderSent,
                    ReminderDate = a.ReminderDate?.ToString("dd/MM HH:mm"),
                    Message = message,
                    WhatsAppUrl = phone == null ? null : $"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}"
                };
            }).ToList();

            return Json(new
            {
                success = true,
                date = day.ToString("yyyy-MM-dd"),
                title = day.ToString("dddd d MMMM", Ar),
                total = data.Count,
                sent = data.Count(x => x.IsReminderSent),
                data
            });
        }

        [HttpPost]
        public async Task<IActionResult> MarkSent(int id)
        {
            var a = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return Json(new { success = false, message = "الموعد غير موجود." });

            a.IsReminderSent = true;
            a.ReminderDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true, reminderDate = a.ReminderDate?.ToString("dd/MM HH:mm") });
        }

        // ── Helpers ────────────────────────────────────────────

        /// <summary>المواعيد اللي اتسجلت من الكالندر من غير وقت بتتحفظ الساعة 12 الضهر → مش هنكتب وقت في الرسالة.</summary>
        private static bool HasRealTime(DateTime d) => d.TimeOfDay != TimeSpan.Zero && d.TimeOfDay != TimeSpan.FromHours(12);

        public static string? WhatsAppNumber(string? mobile, string countryCode)
        {
            var digits = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length < 8) return null;
            if (digits.StartsWith("00")) return digits[2..];
            if (digits.StartsWith(countryCode) && digits.Length > 10) return digits;
            return countryCode + digits.TrimStart('0');
        }

        public static string BuildMessage(ClinicSettings s, string? patient, DateTime when)
        {
            var text = (s.ReminderTemplate ?? string.Empty)
                .Replace("{patient}", (patient ?? string.Empty).Trim())
                .Replace("{day}", when.ToString("dddd", Ar))
                .Replace("{date}", when.ToString("d MMMM", Ar))
                .Replace("{time}", HasRealTime(when) ? " الساعة " + when.ToString("h:mm tt", Ar) : string.Empty)
                .Replace("{doctor}", s.DoctorName ?? string.Empty)
                .Replace("{clinic}", s.ClinicName ?? string.Empty)
                .Replace("{phone}", string.IsNullOrWhiteSpace(s.Phone) ? "العيادة" : s.Phone)
                .Replace("{address}", string.IsNullOrWhiteSpace(s.Address) ? string.Empty : "📍 " + s.Address);

            // شيل السطور الفاضية اللي بتفضل لما متغير يكون فاضي
            var lines = text.Replace("\r", "").Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0);
            return string.Join("\n", lines);
        }
    }
}
