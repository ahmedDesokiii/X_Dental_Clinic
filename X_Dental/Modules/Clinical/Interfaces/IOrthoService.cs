// ============================================================
// 📁 File: Modules/Clinical/Interfaces/IOrthoService.cs  (ملف جديد)
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Clinical.DTOs;

namespace Dental.Modules.Clinical.Interfaces
{
    public interface IOrthoService
    {
        /// <summary>الحالة النشطة للمريض (أو null لو لسه ما اتفتحتش)</summary>
        Task<OrthoCaseDto?> GetActiveCaseAsync(int patientId);

        Task<List<OrthoProcedureDto>> GetCatalogAsync();

        /// <summary>أول سيشن: تحديد الفك + الإجمالي (الزيارة الحالية = Anchor)</summary>
        Task<ServiceResult<int>> CreateCaseAsync(OrthoCreateCaseRequest req);

        /// <summary>تغيير الفك (إضافة) أو تعديل الإجمالي مع تسجيل السبب</summary>
        Task<ServiceResult<bool>> UpdateCaseAsync(OrthoUpdateCaseRequest req);

        /// <summary>حفظ تشخيص الفك/الفكين في الزيارة وإنهاء السيشن</summary>
        Task<ServiceResult<bool>> SaveSessionAsync(OrthoSaveSessionRequest req);
    }
}