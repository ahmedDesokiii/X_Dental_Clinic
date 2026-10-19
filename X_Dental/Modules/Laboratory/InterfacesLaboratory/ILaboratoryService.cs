// ============================================================
// 📁 File: Modules/Laboratory/InterfacesLaboratory/ILaboratoryService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Laboratory.DTOs;

namespace Dental.Modules.Laboratory.InterfacesLaboratory
{
    public interface ILaboratoryService
    {
        /// <summary>⭐ الجدول + الـ KPIs في Query واحدة (المستخدم في صفحة المعمل).</summary>
        Task<LaboratoryDashboardDto> GetDashboardAsync(LaboratoryFilterDto? filter = null);

        Task<List<LaboratoryDiagnosisDto>> GetLaboratoryDiagnosesAsync(LaboratoryFilterDto? filter = null);
        Task<List<LaboratoryPriceHistoryDto>> GetPriceHistoryAsync(int diagnosisCatalogId);
        Task<ServiceResult<bool>> UpdatePriceAsync(UpdateLaboratoryPriceRequest req, string changedBy);
        Task<LaboratorySummaryDto> GetSummaryAsync(LaboratoryFilterDto filter);
    }
}