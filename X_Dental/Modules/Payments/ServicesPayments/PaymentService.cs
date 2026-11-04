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
    //
    // ⭐ المرحلة 5 — محرك دفع واحد (AllocateAsync) لكل الشاشات: السيشن، الـ Details، كشف الحساب.
    //   القاعدة (نفس اللي كانت في FinishSession):
    //    1) الخصم كله على الزيارة المختارة.
    //    2) الكاش يغطي متبقي الزيارة المختارة الأول.
    //    3) الزيادة تسدد الزيارات الأقدم من نفس النوع (الأقدم فالأحدث) — حركة لكل زيارة بنفس رقم الإيصال.
    //    4) لو فضل حاجة بعد سداد كل القديم → تفضل على الزيارة المختارة (رصيد دائن).
    //   والعرض في كل الشاشات بيجمّع حركات الإيصال الواحد في سطر واحد.
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        // نفس القيمة المستخدمة في Statement.cshtml (category === 2 → تقويم)
        private const int OrthoCategoryValue = 2;
        private const int MaxReportDays = 366;
        private const string ArrearsPrefix = "سداد متأخرات";

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

        private static decimal Due(Visit v) => v.TotalAmount - v.Discount - v.PaidAmount;

        // مفتاح الإيصال: كل حركات الإيصال الواحد (نفس المريض + نفس الرقم + نفس اليوم) = دفعة واحدة.
        // الحركات القديمة اللي من غير رقم إيصال بتفضل كل واحدة لوحدها.
        private static string ReceiptKey(int patientId, string? receiptNo, DateTime paymentDate, int paymentId)
            => string.IsNullOrWhiteSpace(receiptNo)
                ? $"id:{paymentId}"
                : $"{patientId}|{receiptNo}|{paymentDate:yyyyMMdd}";

        private static bool IsArrears(string? description)
            => !string.IsNullOrEmpty(description) && description.StartsWith(ArrearsPrefix);

        // ════════════════════════════════════════════════════════════
        // كشف الحساب
        // ════════════════════════════════════════════════════════════
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

            var visitLookup = visits.ToDictionary(v => v.Id);

            var records = payments.Select(p => new PaymentRecordDto
            {
                Id = p.Id,
                VisitId = p.VisitId,
                VisitDate = visitLookup.TryGetValue(p.VisitId, out var vv) ? vv.VisitDate : default,
                Amount = p.Amount,
                Discount = p.Discount,
                Method = p.Method,
                MethodText = GetMethodText(p.Method),
                Description = p.Description,
                ReceiptNo = p.ReceiptNo,
                PaymentDate = p.PaymentDate
            }).ToList();

            var receipts = records
                .GroupBy(r => ReceiptKey(patientId, r.ReceiptNo, r.PaymentDate, r.Id))
                .Select(g =>
                {
                    var parts = g.OrderBy(x => IsArrears(x.Description) ? 1 : 0).ThenBy(x => x.VisitDate).ToList();
                    var main = parts.FirstOrDefault(x => !IsArrears(x.Description)) ?? parts[0];
                    return new ReceiptGroupDto
                    {
                        Key = g.Key,
                        PaymentId = main.Id,
                        ReceiptNo = main.ReceiptNo,
                        PaymentDate = parts.Max(x => x.PaymentDate),
                        Amount = parts.Sum(x => x.Amount),
                        Discount = parts.Sum(x => x.Discount),
                        Method = main.Method,
                        MethodText = main.MethodText,
                        Description = main.Description,
                        IsOrtho = visitLookup.TryGetValue(main.VisitId, out var mv) && IsOrthoCategory(mv.Category),
                        Parts = parts.Select(x => new ReceiptPartDto
                        {
                            PaymentId = x.Id,
                            VisitId = x.VisitId,
                            VisitDate = x.VisitDate,
                            Amount = x.Amount,
                            Discount = x.Discount,
                            IsArrears = IsArrears(x.Description)
                        }).ToList()
                    };
                })
                .OrderByDescending(r => r.PaymentDate)
                .ToList();

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

                Payments = records,
                Receipts = receipts
            };

            return ServiceResult<PatientStatementDto>.Ok(dto);
        }

        // ════════════════════════════════════════════════════════════
        // ⭐ محرك الدفع الموحّد — بيضيف الحركات للـ Context من غير SaveChanges
        //    (عشان يشتغل جوه Transaction بتاعة FinishSession، والمستدعي هو اللي بيحفظ)
        // ════════════════════════════════════════════════════════════
        public async Task<PaymentAllocationResult> AllocateAsync(
            Visit target, decimal cash, decimal discount, PaymentMethod method, string? description, DateTime? when = null)
        {
            if (cash < 0) cash = 0;
            if (discount < 0) discount = 0;

            var at = when ?? DateTime.Now;
            var receiptNo = await GenerateReceiptNoAsync(at);
            var result = new PaymentAllocationResult { ReceiptNo = receiptNo };

            // 1) الخصم على الزيارة المختارة
            if (discount > 0) target.Discount += discount;

            // 2) متبقي الزيارة المختارة الأول
            var left = cash;
            var toCurrent = Math.Min(left, Math.Max(0m, Due(target)));
            left -= toCurrent;

            // 3) الزيادة → الزيارات الأقدم من نفس النوع (FIFO)
            if (left > 0)
            {
                // الـ Query بتجيب الـ Instances المتتبعة لو موجودة (Identity resolution)،
                // فالمتبقي بيتحسب من القيم اللي في الميموري حتى لو لسه متحفظتش في نفس الـ Transaction.
                var older = await _context.Visits
                    .Where(v => v.PatientId == target.PatientId
                             && v.IsActive
                             && v.Id != target.Id
                             && v.Category == target.Category
                             && v.TotalAmount - v.Discount - v.PaidAmount > 0)
                    .OrderBy(v => v.VisitDate)
                    .ThenBy(v => v.Id)
                    .ToListAsync();

                foreach (var old in older)
                {
                    if (left <= 0) break;
                    var oldDue = Due(old);
                    if (oldDue <= 0) continue;

                    var part = Math.Min(left, oldDue);
                    _context.VisitPayments.Add(new VisitPayment
                    {
                        VisitId = old.Id,
                        Amount = part,
                        Discount = 0,
                        Method = method,
                        Description = $"{ArrearsPrefix} — من دفعة {at:dd/MM/yyyy}"
                                      + (string.IsNullOrWhiteSpace(description) ? "" : $" — {description}"),
                        ReceiptNo = receiptNo,
                        PaymentDate = at
                    });
                    old.PaidAmount += part;
                    left -= part;
                    result.SettledOld += part;
                    result.VisitsTouched++;
                }
            }

            // 4) نصيب الزيارة المختارة + أي زيادة متبقية (رصيد دائن)
            var currentPart = toCurrent + left;
            if (currentPart > 0 || discount > 0)
            {
                _context.VisitPayments.Add(new VisitPayment
                {
                    VisitId = target.Id,
                    Amount = currentPart,
                    Discount = discount,
                    Method = method,
                    Description = description,
                    ReceiptNo = receiptNo,
                    PaymentDate = at
                });
                target.PaidAmount += currentPart;
                result.VisitsTouched++;
            }

            result.ToTarget = currentPart;
            result.Credit = left;
            return result;
        }

        // ── تسجيل دفعة من الـ Details / كشف الحساب — نفس محرك السيشن ──
        public async Task<ServiceResult<AddPaymentResultDto>> AddPaymentAsync(AddPaymentDto dto)
        {
            if (dto.Amount < 0 || dto.Discount < 0)
                return ServiceResult<AddPaymentResultDto>.Fail("القيم لازم تكون موجبة.");
            if (dto.Amount <= 0 && dto.Discount <= 0)
                return ServiceResult<AddPaymentResultDto>.Fail("ادخل مبلغ أو خصم.");

            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == dto.VisitId && v.IsActive);
            if (visit == null)
                return ServiceResult<AddPaymentResultDto>.Fail("الزيارة غير موجودة أو غير نشطة.");

            // الخصم مينفعش يعدّي متبقي الزيارة نفسها (غير كده الرصيد يبقى بالسالب من غير ما حد يدفع)
            var due = Math.Max(0m, Due(visit));
            if (dto.Discount > due)
                return ServiceResult<AddPaymentResultDto>.Fail(
                    $"الخصم ({dto.Discount:N2}) أكبر من متبقي الزيارة ({due:N2}).");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var alloc = await AllocateAsync(visit, dto.Amount, dto.Discount, dto.Method, dto.Description);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var parts = new List<string>();
                if (alloc.SettledOld > 0) parts.Add($"منها {alloc.SettledOld:N0} سداد زيارات سابقة");
                if (alloc.Credit > 0) parts.Add($"و{alloc.Credit:N0} رصيد دائن للمريض");
                var message = "تم تسجيل الدفعة بنجاح" + (parts.Count > 0 ? " — " + string.Join(" ", parts) : "") + ".";

                var result = new AddPaymentResultDto
                {
                    PaymentId = 0,
                    ReceiptNo = alloc.ReceiptNo,
                    VisitPaidAmount = visit.PaidAmount,
                    VisitRemaining = visit.Remaining,
                    SettledOld = alloc.SettledOld,
                    Credit = alloc.Credit,
                    VisitsTouched = alloc.VisitsTouched
                };
                return ServiceResult<AddPaymentResultDto>.Ok(result, message);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ── إلغاء دفعة: بيلغي الإيصال كله (كل الزيارات اللي اتوزع عليها) ويرجّع المدفوع والخصم ──
        public async Task<ServiceResult<bool>> VoidPaymentAsync(int paymentId, string? reason)
        {
            var payment = await _context.VisitPayments
                .Include(p => p.Visit)
                .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted);
            if (payment == null)
                return ServiceResult<bool>.Fail("الدفعة غير موجودة أو تم إلغاؤها بالفعل.");

            var patientId = payment.Visit.PatientId;
            List<VisitPayment> group;

            if (string.IsNullOrWhiteSpace(payment.ReceiptNo))
            {
                group = new List<VisitPayment> { payment };
            }
            else
            {
                var dayStart = payment.PaymentDate.Date;
                var dayEnd = dayStart.AddDays(1);
                group = await _context.VisitPayments
                    .Include(p => p.Visit)
                    .Where(p => !p.IsDeleted
                             && p.ReceiptNo == payment.ReceiptNo
                             && p.Visit.PatientId == patientId
                             && p.PaymentDate >= dayStart && p.PaymentDate < dayEnd)
                    .ToListAsync();
            }

            var tag = string.IsNullOrWhiteSpace(reason) ? "[ملغاة]" : $"[ملغاة: {reason}]";
            foreach (var p in group)
            {
                p.IsDeleted = true;
                p.Description = string.IsNullOrWhiteSpace(p.Description) ? tag : $"{p.Description} {tag}";

                // ⭐ بيرجّع المدفوع والخصم الاتنين (الخصم مكانش بيرجع قبل كده)
                p.Visit.PaidAmount -= p.Amount;
                if (p.Visit.PaidAmount < 0) p.Visit.PaidAmount = 0; // حماية من انحراف الأرقام في حالات نادرة

                if (p.Discount > 0)
                {
                    p.Visit.Discount -= p.Discount;
                    if (p.Visit.Discount < 0) p.Visit.Discount = 0;
                }
            }

            await _context.SaveChangesAsync();

            var msg = group.Count > 1
                ? $"تم إلغاء الدفعة بالكامل ({group.Count} حركات على زيارات مختلفة) وتصحيح الأرصدة."
                : "تم إلغاء الدفعة وتصحيح رصيد الزيارة.";
            return ServiceResult<bool>.Ok(true, msg);
        }

        // ════════════════════════════════════════════════════════════
        // ⭐ تقرير المدفوعات اليومية (عام / تقويم + مقارنة بالفترة السابقة)
        //    المرحلة 5: كل إيصال = سطر واحد، حتى لو اتوزع على أكتر من زيارة
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
                    p.Discount,
                    p.Method,
                    p.ReceiptNo,
                    p.Description,
                    p.PaymentDate,
                    VisitId = v.Id,
                    v.VisitDate,
                    v.Category,
                    PatientId = pt.Id,
                    PatientName = pt.FullNameAr,
                    pt.Mobile
                }).ToListAsync();

            // ⭐ كل الزيارات النشطة لمرضى التقرير (مش بس الزيارات اللي عليها دفعات في الفترة)
            //    عشان نحسب رصيد المريض الكلي — نفس منطق كشف الحساب (Sum of Visit.Remaining).
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

            // ── تجميع الحركات بالإيصال ──
            var all = raw
                .GroupBy(r => ReceiptKey(r.PatientId, r.ReceiptNo, r.PaymentDate, r.Id))
                .Select(g =>
                {
                    var rows = g.ToList();
                    var main = rows.FirstOrDefault(x => !IsArrears(x.Description)) ?? rows[0];
                    var isOrtho = IsOrthoCategory(main.Category);
                    var bal = patientBalances[main.PatientId];
                    var visitIds = rows.Select(x => x.VisitId).Distinct().ToList();
                    var visits = visitIds.Select(id => visitsById[id]).ToList();

                    return new DailyPaymentItemDto
                    {
                        Id = main.Id,
                        VisitId = main.VisitId,
                        PatientId = main.PatientId,
                        PatientName = main.PatientName ?? string.Empty,
                        PatientMobile = main.Mobile,
                        IsOrtho = isOrtho,
                        CategoryText = isOrtho ? "تقويم" : "عام",
                        Amount = rows.Sum(x => x.Amount),
                        Discount = rows.Sum(x => x.Discount),
                        Method = Convert.ToInt32(main.Method),
                        MethodText = GetMethodText(main.Method),
                        ReceiptNo = main.ReceiptNo,
                        Description = main.Description,
                        PaymentDate = rows.Max(x => x.PaymentDate),
                        PartsCount = visitIds.Count,
                        Parts = rows
                            .GroupBy(x => x.VisitId)
                            .Select(pg => new DailyPaymentPartDto
                            {
                                VisitId = pg.Key,
                                VisitDate = pg.First().VisitDate,
                                Amount = pg.Sum(x => x.Amount)
                            })
                            .OrderBy(x => x.VisitDate)
                            .ToList(),
                        // أرقام الزيارات اللي الإيصال اتوزع عليها (مجموعهم)
                        VisitTotal = visits.Sum(v => v.TotalAmount),
                        VisitDiscount = visits.Sum(v => v.Discount),
                        // التقويم: المدفوع في نفس اليوم فقط (المطلوب إجمالي للحالة مش بالزيارة)
                        VisitPaid = isOrtho
                            ? visitIds.Sum(id => paidPerVisitDay.GetValueOrDefault((id, main.PaymentDate.Date)))
                            : visits.Sum(v => v.PaidAmount),
                        VisitRemaining = visits.Sum(v => v.Remaining),
                        PatientTotal = bal.Total,
                        PatientRemaining = bal.Remaining,
                        PatientOpenVisits = bal.OpenVisits
                    };
                })
                .ToList();

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
            var currentRows = raw.Where(r => r.PaymentDate >= startDate).ToList();
            var periodVisits = currentRows
                .Select(r => r.VisitId)
                .Distinct()
                .Select(id => visitsById[id])
                .ToList();
            var generalVisits = periodVisits.Where(v => !IsOrthoCategory(v.Category)).ToList();
            var orthoVisits = periodVisits.Where(v => IsOrthoCategory(v.Category)).ToList();

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
                VisitsTotal = generalVisits.Sum(v => v.TotalAmount),
                VisitsPaid = generalVisits.Sum(v => v.PaidAmount),
                VisitsRemaining = generalVisits.Sum(v => v.Remaining),

                GeneralVisitsCount = generalVisits.Count,
                GeneralVisitsTotal = generalVisits.Sum(v => v.TotalAmount),
                GeneralVisitsPaid = generalVisits.Sum(v => v.PaidAmount),
                GeneralVisitsRemaining = generalVisits.Sum(v => v.Remaining),

                OrthoVisitsCount = orthoVisits.Count,
                OrthoVisitsTotal = orthoVisits.Sum(v => v.TotalAmount),
                OrthoVisitsPaid = current.Where(x => x.IsOrtho).Sum(x => x.Amount),
                OrthoVisitsRemaining = orthoVisits.Sum(v => v.Remaining),

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

        // رقم إيصال تسلسلي (مناسب لحجم عيادة واحدة).
        // ⭐ بيحسب كمان الحركات اللي لسه متحفظتش في نفس الـ Context، عشان دفعتين في نفس السيشن
        //    ميخدوش نفس الرقم (كل إيصال بيضيف حركة واحدة على الأقل → الرقم دايمًا بيزيد).
        private async Task<string> GenerateReceiptNoAsync(DateTime at)
        {
            var dayStart = at.Date;
            var dayEnd = dayStart.AddDays(1);

            var countToday = await _context.VisitPayments
                .IgnoreQueryFilters()
                .CountAsync(p => p.PaymentDate >= dayStart && p.PaymentDate < dayEnd);

            var pendingToday = _context.ChangeTracker.Entries<VisitPayment>()
                .Count(e => e.State == EntityState.Added
                         && e.Entity.PaymentDate >= dayStart && e.Entity.PaymentDate < dayEnd);

            return $"REC-{at:yyyyMMdd}-{(countToday + pendingToday + 1):D4}";
        }
    }
}
