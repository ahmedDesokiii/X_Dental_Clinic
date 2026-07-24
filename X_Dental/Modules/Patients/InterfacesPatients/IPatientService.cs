// ============================================================
// 📁 File: IPatientService.cs
// ============================================================

using Dental.Core.Pagination;
using Dental.Core.Results;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Patients.InterfacesPatients
{
    public interface IPatientService
    {
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedRequest request);
        Task<List<PatientLookupDto>> SearchAsync(string keyword);
        Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(int id);
        Task<ServiceResult<int>> CreateAsync(CreatePatientDto dto);
        Task<ServiceResult<bool>> UpdateAsync(UpdatePatientDto dto);
        Task<ServiceResult<bool>> DeleteAsync(int id);
        Task<string> GeneratePatientCodeAsync();

        // ✅ نسخة واحدة موحدة لكل تحقق - excludeId اختياري (null = إضافة جديدة، له قيمة = تعديل)
        Task<bool> ExistsMobileAsync(string mobile, int? excludeId = null);
        Task<bool> ExistsPhoneAsync(string phone, int? excludeId = null);
        Task<bool> ExistsNationalIdAsync(string nationalId, int? excludeId = null);
    }
}