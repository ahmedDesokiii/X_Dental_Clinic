// ============================================================
// 📁 File: Modules/Payments/InterfacesPayments/IPaymentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Payments.DTOs;

namespace Dental.Modules.Payments.InterfacesPayments
{
    public interface IPaymentService
    {
        // ── كشف الحساب الشامل للمريض (كل زياراته + كل دفعاته) ──
        Task<ServiceResult<PatientStatementDto>> GetPatientStatementAsync(int patientId);

        // ── تسجيل دفعة جديدة على زيارة معينة ──
        Task<ServiceResult<AddPaymentResultDto>> AddPaymentAsync(AddPaymentDto dto);

        // ── إلغاء/تراجع عن دفعة (Soft delete + تصحيح رصيد الزيارة) ──
        Task<ServiceResult<bool>> VoidPaymentAsync(int paymentId, string? reason);
    }
}
