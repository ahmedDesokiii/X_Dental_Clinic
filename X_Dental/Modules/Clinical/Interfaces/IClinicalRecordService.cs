// ============================================================
// 📁 File: Modules/Clinical/Interfaces/IClinicalRecordService.cs  (ملف جديد)
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Clinical.DTOs;

namespace Dental.Modules.Clinical.Interfaces
{
    public interface IClinicalRecordService
    {
        Task<ChartToothHistoryDto> GetToothHistoryAsync(int patientId, int toothNumber);

        /// <summary>إضافة تشخيص من صفحة المريض → زيارة سريعة جديدة. بيرجع VisitId.</summary>
        Task<ServiceResult<int>> AddRecordsAsync(ChartAddRecordsRequest req);

        /// <summary>تعديل تشخيص (مجموعة كاملة لو Multi-Unit). بيرجع عدد الوحدات.</summary>
        Task<ServiceResult<int>> UpdateRecordAsync(ChartUpdateRecordRequest req);

        /// <summary>حذف تشخيص/سجل — مع حركة استرداد بالسالب لو الزيارة كانت مدفوعة.</summary>
        Task<ServiceResult<bool>> DeleteRecordAsync(ChartDeleteRecordRequest req);

        Task<ServiceResult<int>> AddXRaysAsync(ChartAddXRaysRequest req);
    }
}