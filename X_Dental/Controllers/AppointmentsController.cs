// ============================================================
// 📁 File: Controllers/AppointmentsController.cs
// ============================================================
using Dental.Modules.Appointments.DTOs;
using Dental.Modules.Appointments.InterfacesAppointments;
using Dental.Modules.Patients.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        // ── صفحة الكالندر الرئيسية ──
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ── تغذية الكالندر بالأحداث خلال فترة زمنية (Weekly/Monthly) ──
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            try
            {
                var events = await _appointmentService.GetCalendarEventsAsync(start, end);
                return Json(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar events");
                return Json(new { success = false, message = "حدث خطأ أثناء تحميل المواعيد." });
            }
        }

        // ── مواعيد اليوم (لاستخدامها كـ widget في الداشبورد) ──
        [HttpGet]
        public async Task<IActionResult> GetToday()
        {
            var data = await _appointmentService.GetTodayAsync();
            return Json(new { success = true, data });
        }

        // ── مواعيد مريض معين (تُستخدم في صفحة تفاصيل المريض) ──
        [HttpGet]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var data = await _appointmentService.GetUpcomingByPatientAsync(patientId);
            return Json(new { success = true, data });
        }

        // ── تفاصيل موعد واحد (لفتح الـ Modal) ──
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _appointmentService.GetByIdAsync(id);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, data = result.Data });
        }

        // ── فحص التعارض قبل الحفظ (Live check أثناء تعبئة الفورم) ──
        [HttpPost]
        public async Task<IActionResult> CheckConflict([FromBody] ConflictCheckRequestDto dto)
        {
            var hasConflict = await _appointmentService.HasConflictAsync(
                dto.AppointmentDate, dto.DurationMinutes, dto.ExcludeId);

            return Json(new { success = true, hasConflict });
        }

        // ── إنشاء موعد جديد ──
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "البيانات المدخلة غير صحيحة." });

            // ⭐ تحذير الحجز المكرر: لو المريض عنده موعد فعّال في نفس اليوم نرجّع التفاصيل للفرونت
            // وهو يسأل المستخدم؛ لو أكد بيبعت AllowSameDay = true ويتم الحجز عادي.
            if (!dto.AllowSameDay)
            {
                var existing = await _appointmentService.GetPatientDayAppointmentsAsync(dto.PatientId, dto.AppointmentDate);
                if (existing.Any())
                {
                    return Json(new
                    {
                        success = false,
                        duplicate = true,
                        message = "المريض لديه موعد محجوز بالفعل في نفس اليوم.",
                        existing = existing.Select(a => new
                        {
                            a.Id,
                            a.PatientName,
                            a.PatientMobile,
                            a.AppointmentDate,
                            a.StatusText,
                            a.Reason
                        })
                    });
                }
            }

            var doctorName = User.Identity?.Name ?? "System";
            var result = await _appointmentService.CreateAsync(dto, doctorName);

            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message, appointmentId = result.Data });
        }

        // ── تعديل بيانات الموعد (السبب/الملاحظات) ──
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateAppointmentDto dto)
        {
            var result = await _appointmentService.UpdateAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── تأجيل الموعد لتاريخ/وقت آخر ──
        [HttpPost]
        public async Task<IActionResult> Reschedule([FromBody] RescheduleAppointmentDto dto)
        {
            var result = await _appointmentService.RescheduleAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── تأكيد الموعد ──
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var result = await _appointmentService.ConfirmAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── تسجيل حضور المريض ──
        [HttpPost]
        public async Task<IActionResult> CheckIn(int id)
        {
            var result = await _appointmentService.CheckInAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── إلغاء الموعد ──
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var result = await _appointmentService.CancelAsync(id, reason);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── تحديد الموعد كـ (لم يحضر) ──
        [HttpPost]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            var result = await _appointmentService.MarkNoShowAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── تحويل الموعد إلى زيارة فعلية عند حضور المريض للكشف ──
        [HttpPost]
        public async Task<IActionResult> ConvertToVisit(int id, VisitCategory category)
        {
            var result = await _appointmentService.ConvertToVisitAsync(id, category);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            // الفرونت يستخدم visitId + patientId عشان يوجّه المستخدم لصفحة الـ Session مباشرة
            return Json(new
            {
                success = true,
                message = result.Message,
                visitId = result.Data!.VisitId,
                patientId = result.Data.PatientId
            });
        }
    }
}
