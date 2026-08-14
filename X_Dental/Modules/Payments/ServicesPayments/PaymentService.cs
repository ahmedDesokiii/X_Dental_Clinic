// ============================================================
// 📁 File: Modules/Payments/ServicesPayments/PaymentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Payments.DTOs;
using Dental.Modules.Payments.InterfacesPayments;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Payments.ServicesPayments
{
    // ملاحظة تصميم مهمة: Visit.PaidAmount فضل Field مخزّن (مش محسوب لايف من VisitPayments)
    // بقرار مقصود، لكن الكلاس ده هو المكان الوحيد المسموح له يعدّل فيه. أي كود تاني
    // (Controller قديم أو جديد) لازم يمر من هنا عشان الرقمين يفضلوا متزامنين دايمًا.
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string GetMethodText(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => "نقدي",
            PaymentMethod.CreditCard => "بطاقة ائتمان",
            PaymentMethod.DebitCard => "بطاقة خصم",
            PaymentMethod.BankTransfer => "تحويل بنكي",
            PaymentMethod.Insurance => "تأمين",
            PaymentMethod.Installment => "تقسيط",
            _ => method.ToString()
        };

        public async Task<ServiceResult<PatientStatementDto>> GetPatientStatementAsync(int patientId)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
                return ServiceResult<PatientStatementDto>.Fail("المريض غير موجود.");

            var visits = await _context.Visits
                .AsNoTracking()
                .Where(v => v.PatientId == patientId && v.IsActive)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();

            var visitIds = visits.Select(v => v.Id).ToList();

            var payments = await _context.VisitPayments
                .AsNoTracking()
                .Where(p => visitIds.Contains(p.VisitId) && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var visitDateLookup = visits.ToDictionary(v => v.Id, v => v.VisitDate);

            var dto = new PatientStatementDto
            {
                PatientId = patient.Id,
                PatientName = patient.FullNameAr,
                PatientMobile = patient.Mobile,
                PatientCode = patient.PatientCode,

                TotalAmount = visits.Sum(v => v.TotalAmount),
                TotalDiscount = visits.Sum(v => v.Discount),
                TotalPaid = visits.Sum(v => v.PaidAmount),
                TotalRemaining = visits.Sum(v => v.Remaining),

                Visits = visits.Select(v => new VisitSummaryDto
                {
                    VisitId = v.Id,
                    VisitDate = v.VisitDate,
                    Category = v.Category,
                    TotalAmount = v.TotalAmount,
                    Discount = v.Discount,
                    PaidAmount = v.PaidAmount,
                    Remaining = v.Remaining
                }).ToList(),

                Payments = payments.Select(p => new PaymentRecordDto
                {
                    Id = p.Id,
                    VisitId = p.VisitId,
                    VisitDate = visitDateLookup.TryGetValue(p.VisitId, out var vd) ? vd : default,
                    Amount = p.Amount,
                    Method = p.Method,
                    MethodText = GetMethodText(p.Method),
                    Description = p.Description,
                    ReceiptNo = p.ReceiptNo,
                    PaymentDate = p.PaymentDate
                }).ToList()
            };

            return ServiceResult<PatientStatementDto>.Ok(dto);
        }

        public async Task<ServiceResult<AddPaymentResultDto>> AddPaymentAsync(AddPaymentDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult<AddPaymentResultDto>.Fail("قيمة الدفعة يجب أن تكون أكبر من صفر.");

            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == dto.VisitId && v.IsActive);
            if (visit == null)
                return ServiceResult<AddPaymentResultDto>.Fail("الزيارة غير موجودة أو غير نشطة.");

            // منع تسجيل دفعة أكبر من المتبقي فعليًا على الزيارة (بيحافظ على تطابق الأرقام)
            if (dto.Amount > visit.Remaining)
                return ServiceResult<AddPaymentResultDto>.Fail(
                    $"قيمة الدفعة ({dto.Amount:N2}) أكبر من المتبقي على الزيارة ({visit.Remaining:N2}).");

            var receiptNo = await GenerateReceiptNoAsync();

            var payment = new VisitPayment
            {
                VisitId = dto.VisitId,
                Amount = dto.Amount,
                Method = dto.Method,
                Description = dto.Description,
                ReceiptNo = receiptNo
            };

            _context.VisitPayments.Add(payment);
            visit.PaidAmount += dto.Amount;

            await _context.SaveChangesAsync();

            var result = new AddPaymentResultDto
            {
                PaymentId = payment.Id,
                ReceiptNo = payment.ReceiptNo,
                VisitPaidAmount = visit.PaidAmount,
                VisitRemaining = visit.Remaining
            };

            return ServiceResult<AddPaymentResultDto>.Ok(result, "تم تسجيل الدفعة بنجاح.");
        }

        public async Task<ServiceResult<bool>> VoidPaymentAsync(int paymentId, string? reason)
        {
            var payment = await _context.VisitPayments.FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted);
            if (payment == null)
                return ServiceResult<bool>.Fail("الدفعة غير موجودة أو تم إلغاؤها بالفعل.");

            var visit = await _context.Visits.FindAsync(payment.VisitId);
            if (visit == null)
                return ServiceResult<bool>.Fail("الزيارة المرتبطة بالدفعة غير موجودة.");

            payment.IsDeleted = true;
            payment.Description = string.IsNullOrWhiteSpace(reason)
                ? $"{payment.Description} [ملغاة]"
                : $"{payment.Description} [ملغاة: {reason}]";

            visit.PaidAmount -= payment.Amount;
            if (visit.PaidAmount < 0) visit.PaidAmount = 0; // حماية من انحراف الأرقام في حالات نادرة

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم إلغاء الدفعة وتصحيح رصيد الزيارة.");
        }

        // رقم إيصال تسلسلي بسيط (مناسب لحجم عيادة واحدة)، بدل الاعتماد على Random اللي ممكن يتكرر
        private async Task<string> GenerateReceiptNoAsync()
        {
            var countToday = await _context.VisitPayments
                .CountAsync(p => p.PaymentDate.Date == DateTime.Today);

            return $"REC-{DateTime.Now:yyyyMMdd}-{(countToday + 1):D4}";
        }
    }
}
