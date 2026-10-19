// ============================================================
// 📁 File: Controllers/PaymentsController.cs
// ============================================================
using Dental.Modules.Payments.DTOs;
using Dental.Modules.Payments.InterfacesPayments;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        // ── صفحة البحث عن مريض للدخول على كشف حسابه + لوحة المدفوعات اليومية ──
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ── صفحة البحث عن مريض لفتح كشف حسابه ──
        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }
        // ── كشف الحساب الشامل لمريض معيّن ──
        [HttpGet]
        public async Task<IActionResult> Statement(int patientId)
        {
            ViewBag.PatientId = patientId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatement(int patientId)
        {
            var result = await _paymentService.GetPatientStatementAsync(patientId);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, data = result.Data });
        }

        // ⭐ تقرير المدفوعات اليومية (dateFrom / dateTo بصيغة yyyy-MM-dd)
        [HttpGet]
        public async Task<IActionResult> GetDailyReport(DateTime? dateFrom, DateTime? dateTo)
        {
            var result = await _paymentService.GetDailyPaymentsReportAsync(dateFrom, dateTo);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, data = result.Data });
        }

        // ── تسجيل دفعة جديدة (تُستخدم من صفحة الكشف، ومن صفحة الزيارة/الـ Session) ──
        [HttpPost]
        public async Task<IActionResult> AddPayment([FromBody] AddPaymentDto dto)
        {
            var result = await _paymentService.AddPaymentAsync(dto);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            // "remaining" متسابة بنفس الاسم القديم عشان صفحة الـ Session الحالية شغالة من غير أي تعديل فيها
            return Json(new
            {
                success = true,
                message = result.Message,
                receiptNo = result.Data!.ReceiptNo,
                paidAmount = result.Data.VisitPaidAmount,
                remaining = result.Data.VisitRemaining
            });
        }

        // ── إلغاء دفعة (بيصحح رصيد الزيارة تلقائيًا) ──
        [HttpPost]
        public async Task<IActionResult> VoidPayment(int paymentId, string? reason)
        {
            var result = await _paymentService.VoidPaymentAsync(paymentId, reason);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}