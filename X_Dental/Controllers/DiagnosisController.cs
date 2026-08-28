// ============================================================
// 📁 File: Controllers/DiagnosisController.cs
// ============================================================
using Dental.Modules.Diagnosis.DTOs;
using Dental.Modules.Diagnosis.InterfacesDiagnosis;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class DiagnosisController : Controller
    {
        private readonly IDiagnosisService _diagnosisService;

        public DiagnosisController(IDiagnosisService diagnosisService)
        {
            _diagnosisService = diagnosisService;
        }

        // ── الصفحة الرئيسية: قائمة + مودال إضافة/تعديل ──
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _diagnosisService.GetAllAsync();
            return View(list);
        }

        // ── جلب بيانات تشخيص واحد لملء مودال التعديل ──
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _diagnosisService.GetByIdAsync(id);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, data = result.Data });
        }

        // ── حفظ (إضافة أو تعديل حسب Id) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(DiagnosisFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = string.Join(" | ", errors) });
            }

            if (dto.Id > 0)
            {
                var updateResult = await _diagnosisService.UpdateAsync(dto);
                return Json(new { success = updateResult.Success, message = updateResult.Message });
            }

            var createResult = await _diagnosisService.CreateAsync(dto);
            return Json(new { success = createResult.Success, message = createResult.Message, id = createResult.Data });
        }

        // ── تفعيل / إيقاف سريع ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var result = await _diagnosisService.ToggleActiveAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── حذف ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _diagnosisService.DeleteAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}