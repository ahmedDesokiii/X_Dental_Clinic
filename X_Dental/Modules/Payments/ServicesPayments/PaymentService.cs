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

        // نفس القيمة المستخدمة في Statement.cshtml (category === 2 → تقويم)
        private const int OrthoCategoryValue = 2;
        private const int MaxReportDays = 366;

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

        // Convert.ToInt32 بيشتغل سواء Category كانت enum أو int
        private static bool IsOrthoCategory(object? category)
            => category != null && Convert.ToInt32(category) == OrthoCategoryValue;

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

        // ════════════════════════════════════════════════════════════
        // ⭐ تقرير المدفوعات اليومية (عام / تقويم + مقارنة بالفترة السابقة)
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<DailyPaymentsReportDto>> GetDailyPaymentsReportAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var endDate = (dateTo ?? DateTime.Today).Date;
            var startDate = (dateFrom ?? endDate).Date;
            if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

            var days = (endDate - startDate).Days + 1;
            if (days > MaxReportDays)
                return ServiceResult<DailyPaymentsReportDto>.Fail($"أقصى مدة للتقرير {MaxReportDays} يوم.");

            var endExclusive = endDate.AddDays(1);
            var prevStart = startDate.AddDays(-days); // نفس طول الفترة، قبلها مباشرة

            // Query واحدة بتجيب الفترة الحالية + السابقة مع بعض
            var raw = await (
                from p in _context.VisitPayments.AsNoTracking()
                join v in _context.Visits.AsNoTracking() on p.VisitId equals v.Id
                join pt in _context.Patients.AsNoTracking() on v.PatientId equals pt.Id
                where !p.IsDeleted && v.IsActive
                      && p.PaymentDate >= prevStart && p.PaymentDate < endExclusive
                select new
                {
                    p.Id,
                    p.Amount,
                    p.Method,
                    p.ReceiptNo,
                    p.Description,
                    p.PaymentDate,
                    VisitId = v.Id,
                    v.Category,
                    PatientId = pt.Id,
                    PatientName = pt.FullNameAr,
                    pt.Mobile
                }).ToListAsync();

            // ⭐ كل الزيارات النشطة لمرضى التقرير (مش بس الزيارات اللي عليها دفعات في الفترة)
            //    عشان نحسب رصيد المريض الكلي — نفس منطق كشف الحساب (Sum of Visit.Remaining).
            //    بنجيبها كـ Entities عشان Remaining تتحسب بنفس منطق الموديل (حتى لو NotMapped).
            var patientIds = raw.Select(r => r.PatientId).Distinct().ToList();
            var patientVisits = await _context.Visits
                .AsNoTracking()
                .Where(v => patientIds.Contains(v.PatientId) && v.IsActive)
                .ToListAsync();

            var visitsById = patientVisits.ToDictionary(v => v.Id);

            // مدفوعات كل زيارة في نفس يوم الدفعة (بنستخدمها للتقويم بدل الإجمالي التراكمي)
            var paidPerVisitDay = raw
                .GroupBy(r => (r.VisitId, Day: r.PaymentDate.Date))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var patientBalances = patientVisits
                .GroupBy(v => v.PatientId)
                .ToDictionary(g => g.Key, g => new
                {
                    Total = g.Sum(v => v.TotalAmount),
                    Remaining = g.Sum(v => v.Remaining),
                    OpenVisits = g.Count(v => v.Remaining > 0)
                });

            var all = raw.Select(r =>
            {
                var isOrtho = IsOrthoCategory(r.Category);
                var visit = visitsById[r.VisitId];
                var bal = patientBalances[r.PatientId];
                return new DailyPaymentItemDto
                {
                    Id = r.Id,
                    VisitId = r.VisitId,
                    PatientId = r.PatientId,
                    PatientName = r.PatientName ?? string.Empty,
                    PatientMobile = r.Mobile,
                    IsOrtho = isOrtho,
                    CategoryText = isOrtho ? "تقويم" : "عام",
                    Amount = r.Amount,
                    Method = Convert.ToInt32(r.Method),
                    MethodText = GetMethodText(r.Method),
                    ReceiptNo = r.ReceiptNo,
                    Description = r.Description,
                    PaymentDate = r.PaymentDate,
                    VisitTotal = visit.TotalAmount,
                    VisitDiscount = visit.Discount,
                    // التقويم: المدفوع في نفس اليوم فقط (المطلوب إجمالي للحالة مش بالزيارة)
                    VisitPaid = isOrtho
                        ? paidPerVisitDay.GetValueOrDefault((r.VisitId, r.PaymentDate.Date))
                        : visit.PaidAmount,
                    VisitRemaining = visit.Remaining,
                    PatientTotal = bal.Total,
                    PatientRemaining = bal.Remaining,
                    PatientOpenVisits = bal.OpenVisits
                };
            }).ToList();

            var current = all.Where(x => x.PaymentDate >= startDate)
                             .OrderByDescending(x => x.PaymentDate)
                             .ToList();
            var previous = all.Where(x => x.PaymentDate < startDate).ToList();

            var dayLookup = current.ToLookup(x => x.PaymentDate.Date);
            var hourLookup = current.ToLookup(x => x.PaymentDate.Hour);

            var byDay = Enumerable.Range(0, days).Select(i =>
            {
                var day = startDate.AddDays(i);
                var list = dayLookup[day].ToList();
                return new DailyPaymentsDayDto
                {
                    Date = day,
                    GeneralAmount = list.Where(x => !x.IsOrtho).Sum(x => x.Amount),
                    OrthoAmount = list.Where(x => x.IsOrtho).Sum(x => x.Amount),
                    Amount = list.Sum(x => x.Amount),
                    Count = list.Count
                };
            }).ToList();

            var byHour = Enumerable.Range(0, 24).Select(h =>
            {
                var list = hourLookup[h].ToList();
                return new DailyPaymentsHourDto
                {
                    Hour = h,
                    GeneralAmount = list.Where(x => !x.IsOrtho).Sum(x => x.Amount),
                    OrthoAmount = list.Where(x => x.IsOrtho).Sum(x => x.Amount),
                    Amount = list.Sum(x => x.Amount),
                    Count = list.Count
                };
            }).ToList();

            var byMethod = current
                .GroupBy(x => new { x.Method, x.MethodText })
                .Select(g => new DailyPaymentsMethodDto
                {
                    Method = g.Key.Method,
                    MethodText = g.Key.MethodText,
                    Amount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var totalAmount = current.Sum(x => x.Amount);
            var prevAmount = previous.Sum(x => x.Amount);

            // كل زيارة مرة واحدة بس (عشان الزيارة اللي عليها كذا دفعة متتحسبش مرتين)
            var periodVisits = current
                .GroupBy(x => x.VisitId)
                .Select(g => g.First())
                .ToList();
            var generalVisits = periodVisits.Where(x => !x.IsOrtho).ToList();
            var orthoVisits = periodVisits.Where(x => x.IsOrtho).ToList();

            var dto = new DailyPaymentsReportDto
            {
                DateFrom = startDate,
                DateTo = endDate,
                Days = days,

                TotalAmount = totalAmount,
                TotalCount = current.Count,
                AveragePayment = current.Count > 0 ? Math.Round(totalAmount / current.Count, 2) : 0,

                GeneralAmount = current.Where(x => !x.IsOrtho).Sum(x => x.Amount),
                GeneralCount = current.Count(x => !x.IsOrtho),
                OrthoAmount = current.Where(x => x.IsOrtho).Sum(x => x.Amount),
                OrthoCount = current.Count(x => x.IsOrtho),

                // كارت "إجمالي المطلوب" وحلقة السداد للعام فقط — التقويم مطلوبه إجمالي للحالة مش بالزيارة
                VisitsCount = generalVisits.Count,
                VisitsTotal = generalVisits.Sum(x => x.VisitTotal),
                VisitsPaid = generalVisits.Sum(x => x.VisitPaid),
                VisitsRemaining = generalVisits.Sum(x => x.VisitRemaining),

                GeneralVisitsCount = generalVisits.Count,
                GeneralVisitsTotal = generalVisits.Sum(x => x.VisitTotal),
                GeneralVisitsPaid = generalVisits.Sum(x => x.VisitPaid),
                GeneralVisitsRemaining = generalVisits.Sum(x => x.VisitRemaining),

                OrthoVisitsCount = orthoVisits.Count,
                OrthoVisitsTotal = orthoVisits.Sum(x => x.VisitTotal),
                OrthoVisitsPaid = current.Where(x => x.IsOrtho).Sum(x => x.Amount),
                OrthoVisitsRemaining = orthoVisits.Sum(x => x.VisitRemaining),

                PreviousTotalAmount = prevAmount,
                PreviousTotalCount = previous.Count,
                ChangePercent = prevAmount > 0
                    ? Math.Round((totalAmount - prevAmount) / prevAmount * 100, 1)
                    : null,

                ByDay = byDay,
                ByHour = byHour,
                ByMethod = byMethod,
                Payments = current
            };

            return ServiceResult<DailyPaymentsReportDto>.Ok(dto);
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