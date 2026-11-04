// ============================================================
// 📁 File: Modules/Appointments/ServicesAppointments/AppointmentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Models.Patient;
using Dental.Modules.Appointments.DTOs;
using Dental.Modules.Appointments.InterfacesAppointments;
using Dental.Modules.Patients.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Appointments.ServicesAppointments
{
    // ملاحظة: الـ Mapping هنا يدوي ومش عن طريق AutoMapper، لأن فيه منطق مركّب
    // (تحويل Duration من TimeSpan لدقائق، حساب End من Start+Duration، نصوص الحالة بالعربي)
    // مش مناسب لـ Profile عادي. لو حابب نضيف AutoMapper Profile بسيط للحالات الأبسط لاحقًا، سهل نضيفه.
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;

        // الحالات اللي بتشغل مكان في الكالندر وتُحسب في فحص التعارض
        // (الملغي والـ NoShow ميعملوش تعارض، لأن مكانهم بقى فاضي فعليًا)
        private static readonly AppointmentStatus[] _blockingStatuses =
        {
            AppointmentStatus.Scheduled,
            AppointmentStatus.Confirmed,
            AppointmentStatus.CheckedIn,
            AppointmentStatus.InProgress,
            AppointmentStatus.Completed
        };

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────
        #region Helpers

        private static string GetStatusText(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Scheduled => "محجوز",
            AppointmentStatus.Confirmed => "مؤكد",
            AppointmentStatus.CheckedIn => "حضر",
            AppointmentStatus.InProgress => "جاري الكشف",
            AppointmentStatus.Completed => "مكتمل",
            AppointmentStatus.Cancelled => "ملغي",
            AppointmentStatus.NoShow => "لم يحضر",
            _ => status.ToString()
        };

        private static string GetStatusColorClass(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Scheduled => "status-scheduled",
            AppointmentStatus.Confirmed => "status-confirmed",
            AppointmentStatus.CheckedIn => "status-checkedin",
            AppointmentStatus.InProgress => "status-inprogress",
            AppointmentStatus.Completed => "status-completed",
            AppointmentStatus.Cancelled => "status-cancelled",
            AppointmentStatus.NoShow => "status-noshow",
            _ => "status-scheduled"
        };

        private AppointmentCalendarDto MapToCalendarDto(Appointment a)
        {
            var end = a.AppointmentDate.Add(a.Duration);
            return new AppointmentCalendarDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullNameAr,
                PatientMobile = a.Patient.Mobile,
                PatientType = a.Patient.PatientType,
                Start = a.AppointmentDate,
                End = end,
                DurationMinutes = (int)a.Duration.TotalMinutes,
                Status = a.Status,
                StatusText = GetStatusText(a.Status),
                StatusColorClass = GetStatusColorClass(a.Status),
                Reason = a.Reason
            };
        }

        #endregion

        // ────────────────────────────────────────────────────────
        #region Read

        public async Task<List<AppointmentCalendarDto>> GetCalendarEventsAsync(DateTime from, DateTime to)
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.AppointmentDate >= from && a.AppointmentDate < to)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return appointments.Select(MapToCalendarDto).ToList();
        }

        public async Task<List<AppointmentListDto>> GetTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentListDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullNameAr,
                    PatientMobile = a.Patient.Mobile,
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = (int)a.Duration.TotalMinutes,
                    Status = a.Status,
                    StatusText = GetStatusText(a.Status),
                    Reason = a.Reason
                })
                .ToListAsync();
        }

        public async Task<List<AppointmentListDto>> GetUpcomingByPatientAsync(int patientId, int take = 10)
        {
            var now = DateTime.Now;

            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.PatientId == patientId && a.AppointmentDate >= now
                            && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .Take(take)
                .Select(a => new AppointmentListDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullNameAr,
                    PatientMobile = a.Patient.Mobile,
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = (int)a.Duration.TotalMinutes,
                    Status = a.Status,
                    StatusText = GetStatusText(a.Status),
                    Reason = a.Reason
                })
                .ToListAsync();
        }

        // مواعيد المريض الفعّالة في يوم معين (بدون الملغي ولم يحضر) — لتحذير الحجز المكرر في نفس اليوم
        public async Task<List<AppointmentListDto>> GetPatientDayAppointmentsAsync(int patientId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.PatientId == patientId
                            && _blockingStatuses.Contains(a.Status)
                            && a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentListDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullNameAr,
                    PatientMobile = a.Patient.Mobile,
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = (int)a.Duration.TotalMinutes,
                    Status = a.Status,
                    StatusText = GetStatusText(a.Status),
                    Reason = a.Reason
                })
                .ToListAsync();
        }

        public async Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int id)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return ServiceResult<AppointmentDetailsDto>.Fail("الموعد غير موجود.");

            var calendarDto = MapToCalendarDto(appointment);
            var dto = new AppointmentDetailsDto
            {
                Id = calendarDto.Id,
                PatientId = calendarDto.PatientId,
                PatientName = calendarDto.PatientName,
                PatientMobile = calendarDto.PatientMobile,
                PatientType = calendarDto.PatientType,
                Start = calendarDto.Start,
                End = calendarDto.End,
                DurationMinutes = calendarDto.DurationMinutes,
                Status = calendarDto.Status,
                StatusText = calendarDto.StatusText,
                StatusColorClass = calendarDto.StatusColorClass,
                Reason = calendarDto.Reason,
                Notes = appointment.Notes,
                DoctorName = appointment.DoctorName,
                VisitId = appointment.VisitId,
                IsReminderSent = appointment.IsReminderSent,
                CreatedAt = appointment.CreatedDate
            };

            return ServiceResult<AppointmentDetailsDto>.Ok(dto);
        }

        #endregion

        // ────────────────────────────────────────────────────────
        #region Conflict Check

        public async Task<bool> HasConflictAsync(DateTime appointmentDate, int durationMinutes, int? excludeId = null)
        {
            var newStart = appointmentDate;
            var newEnd = appointmentDate.AddMinutes(durationMinutes);

            // SQL Server (وEF Core عمومًا) مش قادر يترجم DateTime.Add(TimeSpan) لما الـ TimeSpan عمود ديناميكي
            // (Duration) مش قيمة ثابتة. الحل: نفلتر بنطاق قابل للترجمة (نفس اليوم + الحالة)، وهو استعلام رخيص
            // جدًا لعدد مواعيد اليوم الواحد، وبعدين نعمل المقارنة الدقيقة بالـ TimeSpan في الميموري.
            var dayStart = appointmentDate.Date;
            var dayEnd = dayStart.AddDays(1);

            var query = _context.Appointments
                .Where(a => _blockingStatuses.Contains(a.Status)
                            && a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd);

            if (excludeId.HasValue)
                query = query.Where(a => a.Id != excludeId.Value);

            var sameDayAppointments = await query
                .Select(a => new { a.AppointmentDate, a.Duration })
                .ToListAsync();

            // تعارض زمني كلاسيكي: بداية الموعد الجديد قبل نهاية القديم، ونهاية الجديد بعد بداية القديم
            return sameDayAppointments.Any(a =>
                newStart < a.AppointmentDate.Add(a.Duration) &&
                a.AppointmentDate < newEnd);
        }

        #endregion

        // ────────────────────────────────────────────────────────
        #region Write

        public async Task<ServiceResult<int>> CreateAsync(CreateAppointmentDto dto, string createdByName)
        {
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId && p.IsActive);
            if (!patientExists)
                return ServiceResult<int>.Fail("المريض غير موجود أو غير نشط.");

            if (dto.AppointmentDate < DateTime.Now.AddMinutes(-5))
                return ServiceResult<int>.Fail("لا يمكن حجز موعد في وقت ماضٍ.");

            // ⭐ تم إلغاء فحص تعارض الوقت بالكامل — ممكن تسجيل أي عدد من المرضى في نفس اليوم/الوقت من غير أي اعتراض.
            // if (await HasConflictAsync(dto.AppointmentDate, dto.DurationMinutes))
            //     return ServiceResult<int>.Fail("يوجد موعد آخر متعارض مع هذا التوقيت.");

            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                AppointmentDate = dto.AppointmentDate,
                Duration = TimeSpan.FromMinutes(dto.DurationMinutes),
                Reason = dto.Reason,
                Notes = dto.Notes,
                Status = AppointmentStatus.Scheduled,
                DoctorName = createdByName
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(appointment.Id, "تم حجز الموعد بنجاح.");
        }

        public async Task<ServiceResult<bool>> RescheduleAsync(RescheduleAppointmentDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(dto.Id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("الموعد غير موجود.");

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                return ServiceResult<bool>.Fail("لا يمكن تأجيل موعد ملغي أو مكتمل.");

            if (dto.NewAppointmentDate < DateTime.Now.AddMinutes(-5))
                return ServiceResult<bool>.Fail("لا يمكن نقل الموعد لوقت ماضٍ.");

            // ⭐ نفس المنطق — بدون فحص تعارض
            // if (await HasConflictAsync(dto.NewAppointmentDate, dto.DurationMinutes, dto.Id))
            //     return ServiceResult<bool>.Fail("يوجد موعد آخر متعارض مع الميعاد الجديد.");

            appointment.AppointmentDate = dto.NewAppointmentDate;
            appointment.Duration = TimeSpan.FromMinutes(dto.DurationMinutes);

            if (appointment.Status == AppointmentStatus.Confirmed)
                appointment.Status = AppointmentStatus.Scheduled;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تأجيل الموعد بنجاح.");
        }

        public async Task<ServiceResult<bool>> UpdateAsync(UpdateAppointmentDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(dto.Id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("الموعد غير موجود.");

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                return ServiceResult<bool>.Fail("لا يمكن تعديل موعد ملغي أو مكتمل.");

            appointment.Reason = dto.Reason;
            appointment.Notes = dto.Notes;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تحديث بيانات الموعد.");
        }


        #endregion

        // ────────────────────────────────────────────────────────
        #region Status Flow

        public async Task<ServiceResult<bool>> ConfirmAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("الموعد غير موجود.");

            if (appointment.Status != AppointmentStatus.Scheduled)
                return ServiceResult<bool>.Fail("لا يمكن تأكيد الموعد في حالته الحالية.");

            appointment.Status = AppointmentStatus.Confirmed;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تأكيد الموعد.");
        }

        public async Task<ServiceResult<bool>> CheckInAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("الموعد غير موجود.");

            if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
                return ServiceResult<bool>.Fail("لا يمكن تسجيل حضور المريض في حالة الموعد الحالية.");

            appointment.Status = AppointmentStatus.CheckedIn;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تسجيل حضور المريض.");
        }

        public async Task<ServiceResult<bool>> CancelAsync(int id, string? reason)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("الموعد غير موجود.");

            if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
                return ServiceResult<bool>.Fail("لا يمكن إلغاء موعد مكتمل أو ملغي بالفعل.");

            appointment.Status = AppointmentStatus.Cancelled;
            if (!string.IsNullOrWhiteSpace(reason))
                appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                    ? $"سبب الإلغاء: {reason}"
                    : $"{appointment.Notes}\nسبب الإلغاء: {reason}";

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم إلغاء الموعد.");
        }

        public async Task<ServiceResult<bool>> MarkNoShowAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("الموعد غير موجود.");

            if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
                return ServiceResult<bool>.Fail("لا يمكن تحديد الموعد كـ (لم يحضر) في حالته الحالية.");

            if (appointment.AppointmentDate > DateTime.Now)
                return ServiceResult<bool>.Fail("لا يمكن تحديد موعد لم يحن وقته بعد كـ (لم يحضر).");

            appointment.Status = AppointmentStatus.NoShow;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تحديد الموعد كـ (لم يحضر).");
        }

        #endregion

        // ────────────────────────────────────────────────────────
        #region Convert To Visit

        public async Task<ServiceResult<ConvertToVisitResultDto>> ConvertToVisitAsync(int appointmentId, VisitCategory category)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
                return ServiceResult<ConvertToVisitResultDto>.Fail("الموعد غير موجود.");

            if (appointment.VisitId.HasValue)
                return ServiceResult<ConvertToVisitResultDto>.Fail("تم إنشاء زيارة لهذا الموعد بالفعل.");

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
                return ServiceResult<ConvertToVisitResultDto>.Fail("لا يمكن تحويل موعد ملغي أو (لم يحضر) إلى زيارة.");

            var visit = new Visit
            {
                PatientId = appointment.PatientId,
                VisitDate = DateTime.Now,
                Category = category,
                ChiefComplaint = appointment.Reason,
                IsActive = true
            };

            _context.Visits.Add(visit);
            await _context.SaveChangesAsync(); // عشان ناخد الـ Visit.Id

            appointment.VisitId = visit.Id;
            appointment.Status = AppointmentStatus.Completed;
            await _context.SaveChangesAsync();

            var resultDto = new ConvertToVisitResultDto { VisitId = visit.Id, PatientId = appointment.PatientId };
            return ServiceResult<ConvertToVisitResultDto>.Ok(resultDto, "تم تحويل الموعد إلى زيارة بنجاح.");
        }

        #endregion
    }
}
