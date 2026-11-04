// ============================================================
// 📁 File: Modules/Appointments/InterfacesAppointments/IAppointmentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Appointments.DTOs;
using Dental.Modules.Patients.Enums;

namespace Dental.Modules.Appointments.InterfacesAppointments
{
    public interface IAppointmentService
    {
        // ── القراءة ──
        Task<List<AppointmentCalendarDto>> GetCalendarEventsAsync(DateTime from, DateTime to);
        Task<List<AppointmentListDto>> GetTodayAsync();
        Task<List<AppointmentListDto>> GetUpcomingByPatientAsync(int patientId, int take = 10);
        Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int id);
        Task<List<AppointmentListDto>> GetPatientDayAppointmentsAsync(int patientId, DateTime date);

        // ── فحص التعارض ──
        Task<bool> HasConflictAsync(DateTime appointmentDate, int durationMinutes, int? excludeId = null);

        // ── الكتابة ──
        Task<ServiceResult<int>> CreateAsync(CreateAppointmentDto dto, string createdByName);
        Task<ServiceResult<bool>> UpdateAsync(UpdateAppointmentDto dto);
        Task<ServiceResult<bool>> RescheduleAsync(RescheduleAppointmentDto dto);

        // ── تغيير الحالة (Status Flow) ──
        Task<ServiceResult<bool>> ConfirmAsync(int id);
        Task<ServiceResult<bool>> CheckInAsync(int id);
        Task<ServiceResult<bool>> CancelAsync(int id, string? reason);
        Task<ServiceResult<bool>> MarkNoShowAsync(int id);

        // ── تحويل الموعد لزيارة فعلية عند حضور المريض ──
        Task<ServiceResult<ConvertToVisitResultDto>> ConvertToVisitAsync(int appointmentId, VisitCategory category);
    }
}
