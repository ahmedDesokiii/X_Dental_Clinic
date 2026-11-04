// ============================================================
// 📁 File: Modules/Clinical/Services/OrthoService.cs
// قواعد التقويم:
//   • حالة واحدة نشطة للمريض — الفك (Upper/Lower/Both) والإجمالي بيتحددوا في أول سيشن
//   • الإجمالي والمدفوعات كلها على الـ Anchor Visit (التقارير الحالية متتأثرش)
//   • تشخيص التقويم من غير سعر — كل زيارة تقدر تسجل فك واحد أو الاتنين
//   • السيشن ممكن يبقى تشخيص بس، دفع بس، أو الاتنين — وكله في Transaction واحدة
//   • مفيش أي لمس لمسار المريض العادي (VisitDiagnosis / DentalChartEntry)
// ============================================================
using Dental.Core.Results;
using Dental.Data;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Clinical.Interfaces;
using Dental.Modules.Patients.Enums;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Clinical.Services
{
    public class OrthoService : IOrthoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrthoService> _logger;

        public OrthoService(ApplicationDbContext context, ILogger<OrthoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════
        // قراءة
        // ════════════════════════════════════════════════════════════
        public async Task<OrthoCaseDto?> GetActiveCaseAsync(int patientId)
        {
            var c = await _context.OrthoCases
                .AsNoTracking()
                .Include(x => x.TotalChanges)
                .Where(x => x.PatientId == patientId && !x.IsDeleted && x.Status == OrthoCaseStatus.Active)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            if (c == null) return null;

            var anchor = await _context.Visits.AsNoTracking().FirstOrDefaultAsync(v => v.Id == c.AnchorVisitId);

            var entries = await (
                from e in _context.OrthoVisitEntries.AsNoTracking()
                join v in _context.Visits.AsNoTracking() on e.VisitId equals v.Id
                join p in _context.OrthoProcedureCatalogs.AsNoTracking() on e.OrthoProcedureId equals (int?)p.Id into pj
                from p in pj.DefaultIfEmpty()
                where e.OrthoCaseId == c.Id && !e.IsDeleted
                orderby v.VisitDate descending, e.Arch
                select new OrthoEntryDto
                {
                    Id = e.Id,
                    VisitId = e.VisitId,
                    Date = v.VisitDate,
                    Arch = e.Arch,
                    OrthoProcedureId = e.OrthoProcedureId,
                    ProcedureName = p != null ? p.NameEn : null,
                    Comment = e.Comment
                }).ToListAsync();

            foreach (var e in entries) e.ArchText = ArchText(e.Arch);

            // ⭐ المدفوع والخصم = مجموع كل زيارات الحالة (مش الـ Anchor بس)
            var (paid, discount) = await GetCaseMoneyAsync(c.PatientId, anchor);

            return new OrthoCaseDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                ArchType = c.ArchType,
                ArchText = ArchText(c.ArchType),
                StartDate = c.StartDate,
                AnchorVisitId = c.AnchorVisitId,
                TotalRequired = c.TotalRequired,
                Paid = paid,
                Discount = discount,
                Remaining = c.TotalRequired - discount - paid,
                Entries = entries,
                TotalChanges = c.TotalChanges
                    .OrderByDescending(t => t.ChangedAt)
                    .Select(t => new OrthoTotalChangeDto
                    {
                        ChangedAt = t.ChangedAt,
                        OldTotal = t.OldTotal,
                        NewTotal = t.NewTotal,
                        Reason = t.Reason
                    }).ToList()
            };
        }

        public Task<List<OrthoProcedureDto>> GetCatalogAsync() =>
            _context.OrthoProcedureCatalogs
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.NameEn)
                .Select(p => new OrthoProcedureDto { Id = p.Id, NameAr = p.NameAr, NameEn = p.NameEn })
                .ToListAsync();

        // ════════════════════════════════════════════════════════════
        // ⭐ بناء / تصحيح الحالة من السجلات القديمة (تلقائي عند فتح Session أو Details)
        //   • مريض عنده زيارات تقويم سابقة ومالوش حالة → حالة جديدة من غير Setup
        //   • مريض اتفتحت له حالة من السيشن وعنده زيارات أقدم من الـ Anchor → الحالة تتربط بالقديم
        //   في الحالتين: Anchor = أول زيارة تقويم ، الإجمالي = أول مبلغ مطلوب > 0 من القديم ،
        //   كل المدفوعات والخصومات بتتنقل للـ Anchor ، وكل زيارة قديمة بياخد سجل (Arch = Both)
        //   Idempotent: بعد أول مرة مفيش زيارة أقدم من الـ Anchor → مفيش حاجة تتعمل
        // ════════════════════════════════════════════════════════════
        public async Task<bool> EnsureCaseFromLegacyAsync(int patientId, int? currentVisitId = null)
        {
            var orthoVisits = await _context.Visits
                .Where(v => v.PatientId == patientId && !v.IsDeleted && v.Category == VisitCategory.Orthodontics)
                .OrderBy(v => v.VisitDate).ThenBy(v => v.Id)
                .ToListAsync();

            // التاريخ = زيارات مكتملة غير زيارة النهارده
            var history = orthoVisits
                .Where(v => v.Id != currentVisitId && v.Status == VisitStatus.Completed)
                .ToList();
            if (history.Count == 0) return false;

            var first = history[0];

            var c = await _context.OrthoCases
                .Where(x => x.PatientId == patientId && !x.IsDeleted && x.Status == OrthoCaseStatus.Active)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            Visit? oldAnchor = null;
            if (c != null)
            {
                oldAnchor = orthoVisits.FirstOrDefault(v => v.Id == c.AnchorVisitId);
                // مفيش زيارات أقدم من الـ Anchor الحالي → الحالة سليمة
                if (oldAnchor != null && (first.Id == oldAnchor.Id || first.VisitDate >= oldAnchor.VisitDate))
                    return false;
            }

            // الزيارات القديمة اللي محتاجة سجل: كل التاريخ لو مفيش حالة، أو اللي قبل الـ Anchor القديم
            var legacyVisits = oldAnchor == null
                ? history
                : history.Where(v => v.VisitDate < oldAnchor.VisitDate).ToList();

            var legacyTotal = legacyVisits.FirstOrDefault(v => v.TotalAmount > 0)?.TotalAmount
                              ?? c?.TotalRequired
                              ?? 0;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (c == null)
                {
                    c = new OrthoCase
                    {
                        PatientId = patientId,
                        ArchType = OrthoArch.Both,
                        TotalRequired = legacyTotal,
                        AnchorVisitId = first.Id,
                        StartDate = first.VisitDate,
                        Notes = "اتبنت تلقائيًا من سجلات النظام القديم"
                    };
                    _context.OrthoCases.Add(c);
                }
                else
                {
                    if (c.TotalRequired != legacyTotal)
                    {
                        _context.OrthoTotalChanges.Add(new OrthoTotalChange
                        {
                            OrthoCaseId = c.Id,
                            OldTotal = c.TotalRequired,
                            NewTotal = legacyTotal,
                            Reason = "ربط الحالة بسجلات النظام القديم",
                            ChangedAt = DateTime.Now
                        });
                    }
                    c.AnchorVisitId = first.Id;
                    c.StartDate = first.VisitDate;
                    c.TotalRequired = legacyTotal;
                }

                // ── الفلوس كلها على الـ Anchor ──
                first.TotalAmount = legacyTotal;
                var others = orthoVisits.Where(v => v.Id != first.Id).ToList();
                var otherIds = others.Select(v => v.Id).ToList();

                if (otherIds.Count > 0)
                {
                    var payments = await _context.VisitPayments
                        .Where(p => otherIds.Contains(p.VisitId))
                        .ToListAsync();
                    foreach (var p in payments) p.VisitId = first.Id;   // تاريخ الدفع الحقيقي مش بيتغير
                }

                foreach (var v in others)
                {
                    first.PaidAmount += v.PaidAmount;
                    first.Discount += v.Discount;
                    v.PaidAmount = 0;
                    v.Discount = 0;
                    v.TotalAmount = 0;
                }

                await _context.SaveChangesAsync();   // عشان c.Id يتولد لو الحالة جديدة

                // ── سجل لكل زيارة قديمة مالهاش سجل ──
                var legacyIds = legacyVisits.Select(v => v.Id).ToList();
                var withEntries = await _context.OrthoVisitEntries
                    .Where(e => legacyIds.Contains(e.VisitId) && !e.IsDeleted)
                    .Select(e => e.VisitId).Distinct().ToListAsync();

                foreach (var v in legacyVisits.Where(v => !withEntries.Contains(v.Id)))
                {
                    // ⭐ Examination ممكن يكون فيه ملاحظات متابعة بصيغة JSON (المرحلة 2) → نقرأه كنص مقروء
                    var exam = VisitNotesCodec.PlainText(v.Examination);
                    var comment = !string.IsNullOrWhiteSpace(exam) ? exam!.Trim()
                                : !string.IsNullOrWhiteSpace(v.Notes) ? v.Notes!.Trim()
                                : "زيارة تقويم (مرحّلة)";
                    _context.OrthoVisitEntries.Add(new OrthoVisitEntry
                    {
                        OrthoCaseId = c.Id,
                        VisitId = v.Id,
                        Arch = OrthoArch.Both,
                        OrthoProcedureId = null,
                        Comment = comment,
                        Date = v.VisitDate
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "EnsureCaseFromLegacy failed for patient {PatientId}", patientId);
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════
        // فتح الحالة (أول سيشن)
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<int>> CreateCaseAsync(OrthoCreateCaseRequest req)
        {
            if (req.Arch == OrthoArch.None || (int)req.Arch > 3)
                return ServiceResult<int>.Fail("اختار الفك (Upper / Lower / Both).");
            if (req.TotalRequired < 0)
                return ServiceResult<int>.Fail("الإجمالي لا يمكن أن يكون سالب.");

            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == req.VisitId && v.PatientId == req.PatientId);
            if (visit == null)
                return ServiceResult<int>.Fail("الزيارة غير موجودة.");

            var exists = await _context.OrthoCases.AnyAsync(x =>
                x.PatientId == req.PatientId && !x.IsDeleted && x.Status == OrthoCaseStatus.Active);
            if (exists)
                return ServiceResult<int>.Fail("المريض عنده حالة تقويم نشطة بالفعل.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var c = new OrthoCase
                {
                    PatientId = req.PatientId,
                    ArchType = req.Arch,
                    TotalRequired = req.TotalRequired,
                    AnchorVisitId = visit.Id,
                    StartDate = DateTime.Now,
                    Notes = req.Notes
                };
                _context.OrthoCases.Add(c);

                // الاتفاق الشامل على الـ Anchor — أي مدفوعات سابقة على الزيارة دي تفضل زي ما هي
                visit.TotalAmount = req.TotalRequired;
                visit.Category = VisitCategory.Orthodontics;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult<int>.Ok(c.Id, "تم فتح حالة التقويم.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "CreateOrthoCase failed for patient {PatientId}", req.PatientId);
                return ServiceResult<int>.Fail("حدث خطأ أثناء فتح الحالة.");
            }
        }

        // ════════════════════════════════════════════════════════════
        // تعديل الحالة (الفك / الإجمالي)
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<bool>> UpdateCaseAsync(OrthoUpdateCaseRequest req)
        {
            var c = await _context.OrthoCases.FirstOrDefaultAsync(x => x.Id == req.OrthoCaseId && !x.IsDeleted);
            if (c == null)
                return ServiceResult<bool>.Fail("الحالة غير موجودة.");

            var anchor = await _context.Visits.FindAsync(c.AnchorVisitId);
            if (anchor == null)
                return ServiceResult<bool>.Fail("زيارة الحالة الأساسية غير موجودة.");

            // ── الفك: مسموح نضيف أو نغيّر، بس مينفعش نشيل فك عليه تسجيلات ──
            if (req.Arch.HasValue && req.Arch.Value != c.ArchType)
            {
                var newArch = req.Arch.Value;
                if (newArch == OrthoArch.None || (int)newArch > 3)
                    return ServiceResult<bool>.Fail("فك غير صالح.");

                var usedArches = await _context.OrthoVisitEntries
                    .Where(e => e.OrthoCaseId == c.Id && !e.IsDeleted)
                    .Select(e => e.Arch).Distinct().ToListAsync();

                foreach (var used in usedArches)
                    if ((newArch & used) == 0)
                        return ServiceResult<bool>.Fail($"مينفعش تشيل فك {ArchText(used)} لأن عليه زيارات مسجلة.");

                c.ArchType = newArch;
            }

            // ── الإجمالي: مع تسجيل السبب ──
            if (req.TotalRequired.HasValue && req.TotalRequired.Value != c.TotalRequired)
            {
                var newTotal = req.TotalRequired.Value;
                if (newTotal < 0)
                    return ServiceResult<bool>.Fail("الإجمالي لا يمكن أن يكون سالب.");
                var (casePaid, caseDiscount) = await GetCaseMoneyAsync(c.PatientId, anchor);
                if (newTotal < casePaid + caseDiscount)
                    return ServiceResult<bool>.Fail(
                        $"الإجمالي الجديد أقل من المدفوع + الخصم ({casePaid + caseDiscount:N0}). سجّل استرداد الأول.");

                _context.OrthoTotalChanges.Add(new OrthoTotalChange
                {
                    OrthoCaseId = c.Id,
                    OldTotal = c.TotalRequired,
                    NewTotal = newTotal,
                    Reason = req.Reason,
                    ChangedAt = DateTime.Now
                });
                c.TotalRequired = newTotal;
                anchor.TotalAmount = newTotal;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تحديث الحالة.");
        }

        // ════════════════════════════════════════════════════════════
        // حفظ السيشن: تسجيلات الفك + مدفوعات السيشن (على الـ Anchor) + إنهاء الزيارة
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<bool>> SaveSessionAsync(OrthoSaveSessionRequest req, List<VisitPayment> payments)
        {
            var entries = req.Entries ?? new();
            payments ??= new();
            var validPayments = payments.Where(p => p.Amount > 0 || p.Discount > 0).ToList();

            if (entries.Count == 0 && validPayments.Count == 0)
                return ServiceResult<bool>.Fail("سجّل تشخيص لفك واحد على الأقل أو أضف دفعة.");

            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == req.VisitId && v.PatientId == req.PatientId);
            if (visit == null)
                return ServiceResult<bool>.Fail("الزيارة غير موجودة.");

            var c = await _context.OrthoCases.FirstOrDefaultAsync(x =>
                x.PatientId == req.PatientId && !x.IsDeleted && x.Status == OrthoCaseStatus.Active);
            if (c == null)
                return ServiceResult<bool>.Fail("حدد الفك والإجمالي الأول (فتح الحالة).");

            // ⭐ الـ Anchor هو اللي بيشيل الاتفاق والمدفوعات (ممكن يكون هو نفس زيارة النهارده)
            var anchor = c.AnchorVisitId == visit.Id
                ? visit
                : await _context.Visits.FirstOrDefaultAsync(v => v.Id == c.AnchorVisitId);
            if (anchor == null)
                return ServiceResult<bool>.Fail("زيارة الحالة الأساسية غير موجودة.");

            // ── Validation: تسجيلات الفك ──
            var seen = new HashSet<OrthoArch>();
            foreach (var e in entries)
            {
                if (e.Arch != OrthoArch.Upper && e.Arch != OrthoArch.Lower)
                    return ServiceResult<bool>.Fail("الفك لازم يكون Upper أو Lower.");
                if ((c.ArchType & e.Arch) == 0)
                    return ServiceResult<bool>.Fail($"فك {ArchText(e.Arch)} مش ضمن الحالة. عدّل الفك الأول.");
                if (!seen.Add(e.Arch))
                    return ServiceResult<bool>.Fail($"فك {ArchText(e.Arch)} متكرر.");
                if (!e.OrthoProcedureId.HasValue && string.IsNullOrWhiteSpace(e.Comment))
                    return ServiceResult<bool>.Fail($"فك {ArchText(e.Arch)}: اختار تشخيص أو اكتب Comment.");
            }

            var procIds = entries.Where(e => e.OrthoProcedureId.HasValue).Select(e => e.OrthoProcedureId!.Value).Distinct().ToList();
            if (procIds.Count > 0)
            {
                var found = await _context.OrthoProcedureCatalogs.CountAsync(p => procIds.Contains(p.Id));
                if (found != procIds.Count)
                    return ServiceResult<bool>.Fail("تشخيص تقويم غير موجود.");
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // ================= 1) تسجيلات الفك =================
                if (entries.Count > 0)
                {
                    var existing = await _context.OrthoVisitEntries
                        .Where(x => x.OrthoCaseId == c.Id && x.VisitId == visit.Id && !x.IsDeleted)
                        .ToListAsync();

                    foreach (var e in entries)
                    {
                        var row = existing.FirstOrDefault(x => x.Arch == e.Arch);
                        if (row != null)
                        {
                            // إعادة حفظ نفس السيشن → تحديث بدل التكرار
                            row.OrthoProcedureId = e.OrthoProcedureId;
                            row.Comment = e.Comment?.Trim();
                        }
                        else
                        {
                            _context.OrthoVisitEntries.Add(new OrthoVisitEntry
                            {
                                OrthoCaseId = c.Id,
                                VisitId = visit.Id,
                                Arch = e.Arch,
                                OrthoProcedureId = e.OrthoProcedureId,
                                Comment = e.Comment?.Trim(),
                                Date = DateTime.Now
                            });
                        }
                    }
                }

                // ================= 2) مدفوعات السيشن → الـ Anchor =================
                // نفس منطق FinishSession بالظبط، بس الـ VisitId = Anchor (تاريخ الدفع الحقيقي = النهارده)
                foreach (var pay in validPayments)
                {
                    if (pay.Amount > 0)
                    {
                        pay.VisitId = anchor.Id;
                        _context.VisitPayments.Add(pay);
                        anchor.PaidAmount += pay.Amount;
                    }

                    if (pay.Discount > 0)
                        anchor.Discount += pay.Discount;
                }

                // ================= 3) إنهاء الزيارة =================
                visit.Category = VisitCategory.Orthodontics;
                visit.Status = VisitStatus.Completed;
                visit.EndDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult<bool>.Ok(true, "تم حفظ سيشن التقويم.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "SaveOrthoSession failed for visit {VisitId}", req.VisitId);
                return ServiceResult<bool>.Fail("حدث خطأ أثناء حفظ السيشن.");
            }
        }

        // ════════════════════════════════════════════════════════════
        // ⭐ فلوس الحالة = مجموع PaidAmount / Discount لكل زيارات التقويم بتاعة المريض
        //    من الـ Anchor ولحد دلوقتي (الـ Anchor + أي زيارة تقويم بعده).
        //    كده الأرقام صح حتى لو فيه دفعات مسجلة على زيارات غير الـ Anchor
        //    (داتا قديمة قبل تجميعها، أو دفعة اتسجلت من Details على زيارة تانية).
        // ════════════════════════════════════════════════════════════
        private async Task<(decimal Paid, decimal Discount)> GetCaseMoneyAsync(int patientId, Visit? anchor)
        {
            if (anchor == null) return (0, 0);

            var totals = await _context.Visits
                .AsNoTracking()
                .Where(v => v.PatientId == patientId
                         && !v.IsDeleted
                         && v.Category == VisitCategory.Orthodontics
                         && (v.Id == anchor.Id || v.VisitDate >= anchor.VisitDate))
                .GroupBy(v => 1)
                .Select(g => new
                {
                    Paid = g.Sum(v => v.PaidAmount),
                    Discount = g.Sum(v => v.Discount)
                })
                .FirstOrDefaultAsync();

            return totals == null ? (0, 0) : (totals.Paid, totals.Discount);
        }

        // ════════════════════════════════════════════════════════════
        private static string ArchText(OrthoArch a) => a switch
        {
            OrthoArch.Upper => "Upper",
            OrthoArch.Lower => "Lower",
            OrthoArch.Both => "Upper + Lower",
            _ => "-"
        };
    }
}