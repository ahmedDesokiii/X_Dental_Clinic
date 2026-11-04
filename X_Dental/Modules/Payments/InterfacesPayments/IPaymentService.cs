// ============================================================
// 📁 File: Modules/Payments/InterfacesPayments/IPaymentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
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
        Task<ServiceResult<DailyPaymentsReportDto>> GetDailyPaymentsReportAsync(DateTime? dateFrom, DateTime? dateTo);

        // ⭐ المرحلة 5 — محرك الدفع الموحّد (من غير SaveChanges — المستدعي بيحفظ جوه الـ Transaction بتاعته)
        Task<PaymentAllocationResult> AllocateAsync(
            Visit target, decimal cash, decimal discount, PaymentMethod method, string? description, DateTime? when = null);
    }
}
