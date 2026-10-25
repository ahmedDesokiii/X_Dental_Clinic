// ============================================================
// 📁 File: Controllers/OrthoController.cs  (ملف جديد)
// ============================================================
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Clinical.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class OrthoController : Controller
    {
        private readonly IOrthoService _ortho;
        public OrthoController(IOrthoService ortho) => _ortho = ortho;

        /// <summary>الحالة النشطة + الكتالوج (بيتنادى من Session ومن Details)</summary>
        [HttpGet]
        public async Task<IActionResult> GetCase(int patientId)
        {
            var data = await _ortho.GetActiveCaseAsync(patientId);
            var catalog = await _ortho.GetCatalogAsync();
            return Json(new { success = true, hasCase = data != null, data, catalog });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCase([FromBody] OrthoCreateCaseRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var r = await _ortho.CreateCaseAsync(req);
            return Json(new { success = r.Success, message = r.Message, caseId = r.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCase([FromBody] OrthoUpdateCaseRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var r = await _ortho.UpdateCaseAsync(req);
            return Json(new { success = r.Success, message = r.Message });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSession([FromBody] OrthoSaveSessionRequest req)
        {
            if (req == null) return Json(new { success = false, message = "بيانات غير صالحة." });
            var r = await _ortho.SaveSessionAsync(req);
            return Json(new { success = r.Success, message = r.Message });
        }
    }
}