using Dental.Modules.Laboratory.DTOs;
using Dental.Modules.Laboratory.InterfacesLaboratory;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class LaboratoryController : Controller
    {
        private readonly ILaboratoryService _labService;
        public LaboratoryController(ILaboratoryService labService) => _labService = labService;

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetDiagnoses()
        {
            var data = await _labService.GetLaboratoryDiagnosesAsync();
            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<IActionResult> GetPriceHistory(int diagnosisCatalogId)
        {
            var data = await _labService.GetPriceHistoryAsync(diagnosisCatalogId);
            return Json(new { success = true, data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice([FromBody] UpdateLaboratoryPriceRequest req)
        {
            var changedBy = User?.Identity?.Name ?? "System";
            var result = await _labService.UpdatePriceAsync(req, changedBy);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetSummary([FromQuery] LaboratoryFilterDto filter)
        {
            var data = await _labService.GetSummaryAsync(filter);
            return Json(new { success = true, data });
        }
    }
}