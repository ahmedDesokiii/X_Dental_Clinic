// ============================================================
// 📁 File: Modules/Clinical/Services/OrthoService.cs  (ملف جديد)
// قواعد التقويم:
//   • حالة واحدة نشطة للمريض — الفك (Upper/Lower/Both) والإجمالي بيتحددوا في أول سيشن
//   • الإجمالي والمدفوعات كلها على الـ Anchor Visit (التقارير الحالية متتأثرش)
//   • تشخيص التقويم من غير سعر — كل زيارة تقدر تسجل فك واحد أو الاتنين
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

            var paid = anchor?.PaidAmount ?? 0;
            var discount = anchor?.Discount ?? 0;

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
                if (newTotal < anchor.PaidAmount + anchor.Discount)
                    return ServiceResult<bool>.Fail(
                        $"الإجمالي الجديد أقل من المدفوع + الخصم ({anchor.PaidAmount + anchor.Discount:N0}). سجّل استرداد الأول.");

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
        // حفظ السيشن (فك واحد أو الاتنين) + إنهاء الزيارة
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<bool>> SaveSessionAsync(OrthoSaveSessionRequest req)
        {
            if (req.Entries == null || req.Entries.Count == 0)
                return ServiceResult<bool>.Fail("سجّل تشخيص لفك واحد على الأقل.");

            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == req.VisitId && v.PatientId == req.PatientId);
            if (visit == null)
                return ServiceResult<bool>.Fail("الزيارة غير موجودة.");

            var c = await _context.OrthoCases.FirstOrDefaultAsync(x =>
                x.PatientId == req.PatientId && !x.IsDeleted && x.Status == OrthoCaseStatus.Active);
            if (c == null)
                return ServiceResult<bool>.Fail("حدد الفك والإجمالي الأول (فتح الحالة).");

            // ── Validation ──
            var seen = new HashSet<OrthoArch>();
            foreach (var e in req.Entries)
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

            var procIds = req.Entries.Where(e => e.OrthoProcedureId.HasValue).Select(e => e.OrthoProcedureId!.Value).Distinct().ToList();
            if (procIds.Count > 0)
            {
                var found = await _context.OrthoProcedureCatalogs.CountAsync(p => procIds.Contains(p.Id));
                if (found != procIds.Count)
                    return ServiceResult<bool>.Fail("تشخيص تقويم غير موجود.");
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.OrthoVisitEntries
                    .Where(x => x.OrthoCaseId == c.Id && x.VisitId == visit.Id && !x.IsDeleted)
                    .ToListAsync();

                foreach (var e in req.Entries)
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
        private static string ArchText(OrthoArch a) => a switch
        {
            OrthoArch.Upper => "Upper",
            OrthoArch.Lower => "Lower",
            OrthoArch.Both => "Upper + Lower",
            _ => "-"
        };
    }
}