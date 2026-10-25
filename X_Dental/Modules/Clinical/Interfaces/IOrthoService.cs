// ============================================================
// 📁 File: Modules/Clinical/Interfaces/IOrthoService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;

namespace Dental.Modules.Clinical.Interfaces
{
    public interface IOrthoService
    {
        Task<OrthoCaseDto?> GetActiveCaseAsync(int patientId);
        Task<List<OrthoProcedureDto>> GetCatalogAsync();

        /// <summary>بيبني/يصحح حالة التقويم من الزيارات القديمة. true = حصل تغيير</summary>
        Task<bool> EnsureCaseFromLegacyAsync(int patientId, int? currentVisitId = null);
        Task<ServiceResult<int>> CreateCaseAsync(OrthoCreateCaseRequest req);
        Task<ServiceResult<bool>> UpdateCaseAsync(OrthoUpdateCaseRequest req);

        /// <summary>تسجيلات الفك + مدفوعات السيشن (بتتسجل على الـ Anchor) + إنهاء الزيارة — Transaction واحدة</summary>
        Task<ServiceResult<bool>> SaveSessionAsync(OrthoSaveSessionRequest req, List<VisitPayment> payments);
    }
}