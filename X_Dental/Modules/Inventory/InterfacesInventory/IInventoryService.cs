using Dental.Core.Results;
using Dental.Modules.Inventory.DTOs;

namespace Dental.Modules.Inventory.InterfacesInventory
{
    public interface IInventoryService
    {
        Task<List<MaterialListDto>> GetFilteredAsync(MaterialFilterDto filter);
        Task<ServiceResult<MaterialDetailsDto>> GetByIdAsync(int id);
        Task<ServiceResult<int>> CreateAsync(MaterialFormDto dto, string userName);
        Task<ServiceResult<bool>> UpdateAsync(MaterialFormDto dto, string userName);
        Task<ServiceResult<bool>> DeleteAsync(int id, string userName);

        Task<ServiceResult<bool>> StockInAsync(StockInRequest req, string userName);
        Task<ServiceResult<bool>> StockOutAsync(StockOutRequest req, string userName);

        Task<List<MaterialHistoryDto>> GetHistoryAsync(int materialId);
        Task<InventoryDashboardDto> GetDashboardAsync();
        Task<bool> ExistsCodeAsync(string code, int? excludeId = null);
    }
}