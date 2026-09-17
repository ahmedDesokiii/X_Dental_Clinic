// ============================================================
// 📁 File: Modules/Laboratory/InterfacesLaboratory/ILaboratoryService.cs
// ⭐ غيّر توقيع GetLaboratoryDiagnosesAsync عشان يستقبل الفلتر
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Laboratory.DTOs;

namespace Dental.Modules.Laboratory.InterfacesLaboratory
{
    public interface ILaboratoryService
    {
        Task<List<LaboratoryDiagnosisDto>> GetLaboratoryDiagnosesAsync(LaboratoryFilterDto? filter = null);
        Task<List<LaboratoryPriceHistoryDto>> GetPriceHistoryAsync(int diagnosisCatalogId);
        Task<ServiceResult<bool>> UpdatePriceAsync(UpdateLaboratoryPriceRequest req, string changedBy);
        Task<LaboratorySummaryDto> GetSummaryAsync(LaboratoryFilterDto filter);
    }
}