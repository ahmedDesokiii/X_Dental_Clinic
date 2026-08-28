// ============================================================
// 📁 File: Modules/Diagnosis/InterfacesDiagnosis/IDiagnosisService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Diagnosis.DTOs;

namespace Dental.Modules.Diagnosis.InterfacesDiagnosis
{
    public interface IDiagnosisService
    {
        Task<List<DiagnosisListDto>> GetAllAsync();
        Task<ServiceResult<DiagnosisFormDto>> GetByIdAsync(int id);
        Task<ServiceResult<int>> CreateAsync(DiagnosisFormDto dto);
        Task<ServiceResult<bool>> UpdateAsync(DiagnosisFormDto dto);
        Task<ServiceResult<bool>> ToggleActiveAsync(int id);
        Task<ServiceResult<bool>> DeleteAsync(int id);

        // للاستخدام في مودال السن (فيه السعر عشان الحسبة تبقى في المتصفح)
        Task<List<DiagnosisLookupDto>> GetActiveLookupAsync();

        Task<bool> ExistsCodeAsync(string code, int? excludeId = null);
    }
}
