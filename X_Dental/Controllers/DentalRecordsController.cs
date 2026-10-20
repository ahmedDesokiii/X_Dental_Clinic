// ============================================================
// 📁 File: Controllers/DentalRecordsController.cs  (ملف جديد)
// تعديل الدينتال شارت من صفحة المريض — حفظ مباشر
// ============================================================
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Clinical.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class DentalRecordsController : Controller
    {
        private readonly IClinicalRecordService _records;
        public DentalRecordsController(IClinicalRecordService records) => _records = records;

        [HttpGet]
        public async Task<IActionResult> GetToothHistory(int patientId, int tooth)
        {

            var data = await _records.GetToothHistoryAsync(patientId, tooth);
            return Json(new { success = true, data });
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddRecords([FromBody] ChartAddRecordsRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var result = await _records.AddRecordsAsync(req);
            return Json(new { success = result.Success, message = result.Message, visitId = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRecord([FromBody] ChartUpdateRecordRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var result = await _records.UpdateRecordAsync(req);
            return Json(new { success = result.Success, message = result.Message, units = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRecord([FromBody] ChartDeleteRecordRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var result = await _records.DeleteRecordAsync(req);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddXRays([FromBody] ChartAddXRaysRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var result = await _records.AddXRaysAsync(req);
            return Json(new { success = result.Success, message = result.Message, count = result.Data });
        }
    }
}