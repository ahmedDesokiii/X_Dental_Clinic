// ============================================================
// 📁 File: Controllers/OrthoController.cs
// ============================================================
using System.Text.Json;
using System.Text.Json.Serialization;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Clinical.Interfaces;
using Dental.Modules.Patients.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class OrthoController : Controller
    {
        private readonly IOrthoService _ortho;
        private readonly ILogger<OrthoController> _logger;

        public OrthoController(IOrthoService ortho, ILogger<OrthoController> logger)
        {
            _ortho = ortho;
            _logger = logger;
        }

        /// <summary>
        /// الحالة النشطة + الكتالوج (بيتنادى من Session ومن Details).
        /// ⭐ قبل القراءة: لو المريض عنده زيارات تقويم قديمة → الحالة بتتبني/تتصحح منها تلقائيًا.
        /// created = true معناها الأرقام اتغيرت (شاشة السيشن بتعمل reload عشان تاب الدفع يتحدث).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCase(int patientId, int? visitId)
        {
            var created = await _ortho.EnsureCaseFromLegacyAsync(patientId, visitId);
            var data = await _ortho.GetActiveCaseAsync(patientId);
            var catalog = await _ortho.GetCatalogAsync();
            return Json(new { success = true, hasCase = data != null, created, data, catalog });
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

        /// <summary>
        /// حفظ سيشن التقويم. الـ body نفس شكل FinishSession:
        /// { PatientId, VisitId, Entries: [...], Payments: window.pendingPayments }
        /// المدفوعات بتتقرا بنفس DTO بتاع FinishSession (نفس شاشة _PaymentsCompact) وبتتسجل على الـ Anchor.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveSession()
        {
            string rawBody;
            using (var reader = new StreamReader(Request.Body))
                rawBody = await reader.ReadToEndAsync();

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            OrthoSaveSessionRequest? req;
            FinishSessionRequest? payReq;
            try
            {
                req = JsonSerializer.Deserialize<OrthoSaveSessionRequest>(rawBody, opts);
                payReq = JsonSerializer.Deserialize<FinishSessionRequest>(rawBody, opts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ortho SaveSession JSON parse failed. Raw body: {Body}", rawBody);
                return Json(new { success = false, message = "خطأ في قراءة بيانات السيشن: " + ex.Message });
            }

            if (req == null || req.PatientId <= 0 || req.VisitId <= 0)
                return Json(new { success = false, message = "بيانات غير صالحة." });

            // نفس تحويل FinishSession → VisitPayment (الـ VisitId بيتحدد في الـ Service = Anchor)
            var payments = new List<VisitPayment>();
            if (payReq?.Payments != null)
            {
                foreach (var pay in payReq.Payments)
                {
                    if (pay.Amount <= 0 && pay.Discount <= 0) continue;
                    payments.Add(new VisitPayment
                    {
                        Amount = pay.Amount,
                        Discount = pay.Discount,
                        Method = pay.Method,
                        Description = pay.Description
                        // رقم الإيصال بيتعمل في محرك الدفع الموحّد (B3)
                    });
                }
            }

            var r = await _ortho.SaveSessionAsync(req, payments);
            return Json(new { success = r.Success, message = r.Message });
        }
    }
}