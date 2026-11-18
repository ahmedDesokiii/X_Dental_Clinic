// ============================================================
// 📁 File: Modules/Clinical/Services/ClinicalRecordService.cs  (ملف جديد)
// تعديل الدينتال شارت بعد حفظ السيشن (من صفحة المريض Details)
// القواعد:
//   • إضافة تشخيص  → زيارة سريعة جديدة (Completed) — الحسابات القديمة متتلمسش
//   • تعديل/حذف    → Visit.TotalAmount بيتصحح بـ PriceSnapshot (سعر وقت التشخيص)
//   • لو الزيارة بقت مدفوعة بزيادة → حركة استرداد بالسالب في المدفوعات بسبب واضح
// ============================================================
using Dental.Core.Results;
using Dental.Data;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Clinical.Interfaces;
using Dental.Modules.Patients.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Clinical.Services
{
    public class ClinicalRecordService : IClinicalRecordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ClinicalRecordService> _logger;

        public ClinicalRecordService(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ClinicalRecordService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════
        // سجل السن
        // ════════════════════════════════════════════════════════════
        public async Task<ChartToothHistoryDto> GetToothHistoryAsync(int patientId, int toothNumber)
        {
            var dto = new ChartToothHistoryDto { ToothNumber = toothNumber };

            var visitDates = await _context.Visits
                .AsNoTracking()
                .Where(v => v.PatientId == patientId)
                .Select(v => new { v.Id, v.VisitDate })
                .ToDictionaryAsync(v => v.Id, v => v.VisitDate);

            if (visitDates.Count == 0)
                return dto;

            var visitIds = visitDates.Keys.ToList();

            var diagnoses = await _context.VisitDiagnoses
                .AsNoTracking()
                .Include(d => d.Diagnosis)
                .Where(d => visitIds.Contains(d.VisitId) && d.ToothNumber == toothNumber && !d.IsDeleted)
                .ToListAsync();

            // أسنان كل مجموعة Multi-Unit
            var groupIds = diagnoses.Where(d => d.GroupId.HasValue).Select(d => d.GroupId!.Value).Distinct().ToList();
            var groupTeeth = groupIds.Count == 0
                ? new Dictionary<Guid, List<int>>()
                : (await _context.VisitDiagnoses
                    .AsNoTracking()
                    .Where(d => d.GroupId != null && groupIds.Contains(d.GroupId.Value) && !d.IsDeleted)
                    .Select(d => new { GroupId = d.GroupId!.Value, Tooth = (int?)d.ToothNumber })
                    .ToListAsync())
                    .GroupBy(x => x.GroupId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Where(x => x.Tooth.HasValue).Select(x => x.Tooth!.Value).Distinct().OrderBy(ToothArch.Rank).ToList());

            var chartEntries = await _context.DentalChartEntries
                .AsNoTracking()
                .Where(e => e.PatientId == patientId && e.ToothNumber == toothNumber && !e.IsDeleted)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            // صف لكل تشخيص
            foreach (var d in diagnoses)
            {
                var chart = chartEntries.FirstOrDefault(e => e.VisitId == d.VisitId);
                var teeth = d.GroupId.HasValue && groupTeeth.TryGetValue(d.GroupId.Value, out var gt) ? gt : new List<int> { toothNumber };

                dto.Records.Add(new ChartToothRecordDto
                {
                    VisitDiagnosisId = d.Id,
                    ChartEntryId = chart?.Id ?? 0,
                    VisitId = d.VisitId,
                    Date = visitDates.TryGetValue(d.VisitId, out var vDate) ? vDate : DateTime.MinValue,
                    DiagnosisCatalogId = d.DiagnosisCatalogId,
                    DiagnosisName = d.Diagnosis?.NameEn ?? d.Diagnosis?.NameAr,
                    Price = UnitPrice(d),
                    Units = teeth.Count,
                    GroupId = d.GroupId,
                    GroupTeeth = teeth,
                    Status = chart != null ? (int)chart.Status : null,
                    StatusText = chart?.Status.ToString(),
                    Notes = d.Notes
                });
            }

            // سجلات حالة بدون تشخيص (زي Missing/Extracted من غير تشخيص)
            var visitsWithDiag = diagnoses.Select(d => d.VisitId).ToHashSet();
            foreach (var e in chartEntries.Where(e => e.VisitId == null || !visitsWithDiag.Contains(e.VisitId.Value)))
            {
                dto.Records.Add(new ChartToothRecordDto
                {
                    VisitDiagnosisId = 0,
                    ChartEntryId = e.Id,
                    VisitId = e.VisitId ?? 0,
                    Date = e.Date,
                    Units = 1,
                    GroupId = e.GroupId,
                    GroupTeeth = new List<int> { toothNumber },
                    Status = (int)e.Status,
                    StatusText = e.Status.ToString(),
                    Notes = e.Notes
                });
            }

            dto.Records = dto.Records.OrderByDescending(r => r.Date).ToList();

            dto.XRays = await _context.PatientXRays
                .AsNoTracking()
                .Where(x => x.PatientId == patientId && x.ToothNumber == toothNumber && !x.IsDeleted)
                .OrderByDescending(x => x.TakenDate)
                .Select(x => new ChartXRayDto
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    Notes = x.Notes,
                    TakenDate = x.TakenDate
                })
                .ToListAsync();

            return dto;
        }

        // ════════════════════════════════════════════════════════════
        // إضافة → زيارة سريعة جديدة
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<int>> AddRecordsAsync(ChartAddRecordsRequest req)
        {
            var teeth = (req.Teeth ?? new()).Distinct().ToList();
            if (teeth.Count == 0)
                return ServiceResult<int>.Fail("اختار سن واحد على الأقل.");
            if (teeth.Any(t => !ToothArch.IsValid(t)))
                return ServiceResult<int>.Fail("رقم سن غير صالح.");
            if (teeth.Count > 1 && !ToothArch.IsContiguous(teeth))
                return ServiceResult<int>.Fail("أسنان المجموعة لازم تكون متجاورة في نفس الفك.");

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == req.PatientId && p.IsActive);
            if (patient == null)
                return ServiceResult<int>.Fail("المريض غير موجود.");

            DiagnosisCatalog? diag = null;
            if (req.DiagnosisCatalogId.HasValue && req.DiagnosisCatalogId.Value > 0)
            {
                diag = await _context.DiagnosisCatalogs.FirstOrDefaultAsync(d => d.Id == req.DiagnosisCatalogId.Value && d.IsActive);
                if (diag == null)
                    return ServiceResult<int>.Fail("التشخيص غير موجود.");
            }
            else if (teeth.Count > 1)
            {
                return ServiceResult<int>.Fail("اختار تشخيص للوحدات.");
            }

            var category = req.Category
                ?? (patient.PatientType == Dental.Models.Patient.PatientType.Orthodontics
                    ? VisitCategory.Orthodontics
                    : VisitCategory.General);

            teeth = teeth.OrderBy(ToothArch.Rank).ToList();
            var groupId = teeth.Count > 1 ? Guid.NewGuid() : (Guid?)null;
            var status = (Dental.Models.Patient.ToothStatus)req.Status;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) زيارة سريعة
                var visit = new Visit
                {
                    PatientId = patient.Id,
                    VisitDate = DateTime.Now,
                    EndDate = DateTime.Now,
                    Status = VisitStatus.Completed,
                    FollowUpType = FollowUpType.Normal,
                    Category = category,
                    ChiefComplaint = "تعديل من ملف المريض"
                };
                _context.Visits.Add(visit);
                await _context.SaveChangesAsync();

                var labPrice = diag != null ? await GetCurrentLabPriceAsync(diag) : 0;

                // 2) سجل لكل سن
                foreach (var tooth in teeth)
                {
                    _context.DentalChartEntries.Add(new DentalChartEntry
                    {
                        PatientId = patient.Id,
                        VisitId = visit.Id,
                        ToothNumber = tooth,
                        Status = status,
                        SurfacesJson = "[]",
                        Notes = req.Notes,
                        GroupId = groupId,
                        Date = DateTime.Now
                    });

                    if (diag != null)
                    {
                        _context.VisitDiagnoses.Add(new VisitDiagnosis
                        {
                            VisitId = visit.Id,
                            DiagnosisCatalogId = diag.Id,
                            ToothNumber = tooth,
                            Notes = req.Notes,
                            GroupId = groupId,
                            PriceSnapshot = diag.Price,      // ⭐ سعر السن وقت التشخيص
                            LabPriceSnapshot = labPrice
                        });
                        visit.TotalAmount += diag.Price;     // السعر × عدد الوحدات
                    }
                }

                // 3) الأشعة (مرة واحدة على أول سن)
                SaveXRayFiles(patient.Id, visit.Id, teeth[0], req.XRays);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult<int>.Ok(visit.Id, teeth.Count > 1
                    ? $"تم تسجيل {teeth.Count} وحدات في زيارة جديدة."
                    : "تم تسجيل السن في زيارة جديدة.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "AddRecords failed for patient {PatientId}", req.PatientId);
                return ServiceResult<int>.Fail("حدث خطأ أثناء الحفظ.");
            }
        }

        // ════════════════════════════════════════════════════════════
        // تعديل
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<int>> UpdateRecordAsync(ChartUpdateRecordRequest req)
        {
            var vd = await _context.VisitDiagnoses
                .Include(x => x.Diagnosis)
                .FirstOrDefaultAsync(x => x.Id == req.VisitDiagnosisId && !x.IsDeleted);
            if (vd == null)
                return ServiceResult<int>.Fail("السجل غير موجود.");

            var newDiag = await _context.DiagnosisCatalogs.FirstOrDefaultAsync(d => d.Id == req.DiagnosisCatalogId);
            if (newDiag == null)
                return ServiceResult<int>.Fail("التشخيص غير موجود.");

            var rows = await GetGroupRowsAsync(vd);
            var visit = await _context.Visits.FindAsync(vd.VisitId);
            if (visit == null)
                return ServiceResult<int>.Fail("الزيارة غير موجودة.");

            var oldName = vd.Diagnosis?.NameEn ?? vd.Diagnosis?.NameAr ?? "-";
            var oldTotal = rows.Sum(UnitPrice);
            var diagChanged = newDiag.Id != vd.DiagnosisCatalogId;
            var newLab = diagChanged ? await GetCurrentLabPriceAsync(newDiag) : 0;

            foreach (var r in rows)
            {
                if (diagChanged)
                {
                    r.DiagnosisCatalogId = newDiag.Id;
                    r.PriceSnapshot = newDiag.Price;
                    r.LabPriceSnapshot = newLab;
                }
                else if (r.PriceSnapshot <= 0)
                {
                    r.PriceSnapshot = r.Diagnosis?.Price ?? 0; // تثبيت السعر للسجلات القديمة
                }
                r.Notes = req.Notes;
            }

            var newTotal = diagChanged ? newDiag.Price * rows.Count : oldTotal;
            visit.TotalAmount += newTotal - oldTotal;

            // تعديل حالة السن لنفس الزيارة (اختياري)
            if (req.Status.HasValue)
            {
                var teeth = rows.Where(r => r.ToothNumber.HasValue).Select(r => r.ToothNumber!.Value).ToList();
                var entries = await _context.DentalChartEntries
                    .Where(e => e.VisitId == vd.VisitId && teeth.Contains(e.ToothNumber) && !e.IsDeleted
                             && (vd.GroupId == null || e.GroupId == vd.GroupId))
                    .ToListAsync();
                foreach (var e in entries)
                    e.Status = (Dental.Models.Patient.ToothStatus)req.Status.Value;
            }

            if (newTotal < oldTotal)
            {
                SettleOverpayment(visit,
                    $"استرداد — تعديل تشخيص {oldName} → {newDiag.NameEn} ({TeethLabel(rows)}) — زيارة {visit.VisitDate:dd/MM/yyyy}");
            }

            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(rows.Count, rows.Count > 1
                ? $"تم تعديل {rows.Count} وحدات."
                : "تم تعديل التشخيص.");
        }

        // ════════════════════════════════════════════════════════════
        // حذف
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<bool>> DeleteRecordAsync(ChartDeleteRecordRequest req)
        {
            // سجل حالة بدون تشخيص → حذف بدون أي أثر مالي
            if (req.VisitDiagnosisId <= 0)
            {
                var entry = await _context.DentalChartEntries.FirstOrDefaultAsync(e => e.Id == req.ChartEntryId && !e.IsDeleted);
                if (entry == null)
                    return ServiceResult<bool>.Fail("السجل غير موجود.");
                entry.IsDeleted = true;
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "تم حذف السجل.");
            }

            var vd = await _context.VisitDiagnoses
                .Include(x => x.Diagnosis)
                .FirstOrDefaultAsync(x => x.Id == req.VisitDiagnosisId && !x.IsDeleted);
            if (vd == null)
                return ServiceResult<bool>.Fail("السجل غير موجود أو اتحذف بالفعل.");

            var visit = await _context.Visits.FindAsync(vd.VisitId);
            if (visit == null)
                return ServiceResult<bool>.Fail("الزيارة غير موجودة.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var rows = await GetGroupRowsAsync(vd);
                var amount = rows.Sum(UnitPrice);
                var teeth = rows.Where(r => r.ToothNumber.HasValue).Select(r => r.ToothNumber!.Value).ToList();
                var diagName = vd.Diagnosis?.NameEn ?? vd.Diagnosis?.NameAr ?? "-";
                var teethLabel = TeethLabel(rows);

                foreach (var r in rows)
                    r.IsDeleted = true;

                // سجلات الشارت المرتبطة
                List<DentalChartEntry> entries;
                if (vd.GroupId.HasValue)
                {
                    entries = await _context.DentalChartEntries
                        .Where(e => e.GroupId == vd.GroupId && !e.IsDeleted)
                        .ToListAsync();
                }
                else
                {
                    // سن منفرد: بنحذف حالة السن بس لو مفيش تشخيص تاني على نفس السن في نفس الزيارة
                    var hasOther = await _context.VisitDiagnoses.AnyAsync(x =>
                        x.Id != vd.Id && x.VisitId == vd.VisitId && x.ToothNumber == vd.ToothNumber && !x.IsDeleted);
                    entries = hasOther
                        ? new List<DentalChartEntry>()
                        : await _context.DentalChartEntries
                            .Where(e => e.VisitId == vd.VisitId && teeth.Contains(e.ToothNumber) && e.GroupId == null && !e.IsDeleted)
                            .ToListAsync();
                }
                foreach (var e in entries)
                    e.IsDeleted = true;

                // تصحيح الزيارة + استرداد لو كانت مدفوعة بزيادة
                visit.TotalAmount -= amount;
                if (visit.TotalAmount < 0) visit.TotalAmount = 0;

                var reason = $"استرداد — حذف تشخيص {diagName} ({teethLabel}) — زيارة {visit.VisitDate:dd/MM/yyyy}"
                           + (string.IsNullOrWhiteSpace(req.Reason) ? "" : $" — {req.Reason}");
                var refund = SettleOverpayment(visit, reason);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult<bool>.Ok(true, refund > 0
                    ? $"تم الحذف وتسجيل استرداد بقيمة {refund:N0}."
                    : "تم حذف التشخيص.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "DeleteRecord failed for VisitDiagnosis {Id}", req.VisitDiagnosisId);
                return ServiceResult<bool>.Fail("حدث خطأ أثناء الحذف.");
            }
        }

        // ════════════════════════════════════════════════════════════
        // أشعة على سن
        // ════════════════════════════════════════════════════════════
        public async Task<ServiceResult<int>> AddXRaysAsync(ChartAddXRaysRequest req)
        {
            if (!ToothArch.IsValid(req.ToothNumber))
                return ServiceResult<int>.Fail("رقم سن غير صالح.");
            if (req.XRays == null || req.XRays.Count == 0)
                return ServiceResult<int>.Fail("لا توجد صور.");

            // الأشعة بتتربط بآخر زيارة للمريض
            var lastVisitId = await _context.Visits
                .Where(v => v.PatientId == req.PatientId)
                .OrderByDescending(v => v.VisitDate)
                .Select(v => (int?)v.Id)
                .FirstOrDefaultAsync();
            if (lastVisitId == null)
                return ServiceResult<int>.Fail("لا توجد زيارات لهذا المريض.");

            var count = SaveXRayFiles(req.PatientId, lastVisitId.Value, req.ToothNumber, req.XRays);
            if (count == 0)
                return ServiceResult<int>.Fail("تعذر حفظ الصور.");

            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(count, $"تم رفع {count} صورة.");
        }

        // ════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════

        // سعر السن الواحد وقت التشخيص — ولو سجل قديم من غير Snapshot → سعر الكتالوج
        private static decimal UnitPrice(VisitDiagnosis d) =>
            d.PriceSnapshot > 0 ? d.PriceSnapshot : (d.Diagnosis?.Price ?? 0);

        private async Task<List<VisitDiagnosis>> GetGroupRowsAsync(VisitDiagnosis vd)
        {
            if (!vd.GroupId.HasValue)
                return new List<VisitDiagnosis> { vd };

            return await _context.VisitDiagnoses
                .Include(x => x.Diagnosis)
                .Where(x => x.GroupId == vd.GroupId && x.VisitId == vd.VisitId && !x.IsDeleted)
                .ToListAsync();
        }

        /// <summary>
        /// لو المدفوع بقى أكبر من المطلوب → حركة استرداد بالسالب بالفرق (في حدود المدفوع).
        /// بيرجع قيمة الاسترداد.
        /// </summary>
        private decimal SettleOverpayment(Visit visit, string reason)
        {
            var remaining = visit.TotalAmount - visit.Discount - visit.PaidAmount;
            if (remaining >= 0) return 0;

            var refund = Math.Min(-remaining, visit.PaidAmount);
            if (refund <= 0) return 0;

            _context.VisitPayments.Add(new VisitPayment
            {
                VisitId = visit.Id,
                Amount = -refund,
                Method = default,   // نقدي
                Description = reason,
                ReceiptNo = $"REF-{DateTime.Now:yyyyMMddHHmmss}",
                PaymentDate = DateTime.Now
            });
            visit.PaidAmount -= refund;
            return refund;
        }

        private int SaveXRayFiles(int patientId, int visitId, int tooth, List<ChartXRayInput>? xrays)
        {
            if (xrays == null) return 0;
            var count = 0;
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "xrays");
            Directory.CreateDirectory(uploadsRoot);

            foreach (var xr in xrays)
            {
                if (string.IsNullOrWhiteSpace(xr.Base64) || string.IsNullOrWhiteSpace(xr.FileName))
                    continue;
                try
                {
                    var safeName = Dental.Core.Security.UploadSafety.XRayFileName(patientId, tooth, xr.FileName);
                    if (safeName == null) continue;   // 🔒 A2: امتداد غير مسموح
                    var base64Data = xr.Base64.Contains(',') ? xr.Base64[(xr.Base64.IndexOf(',') + 1)..] : xr.Base64;
                    File.WriteAllBytes(Path.Combine(uploadsRoot, safeName), Convert.FromBase64String(base64Data));

                    _context.PatientXRays.Add(new PatientXRay
                    {
                        PatientId = patientId,
                        VisitId = visitId,
                        ToothNumber = tooth,
                        FileName = xr.FileName,
                        FilePath = $"/uploads/xrays/{safeName}",
                        TakenDate = DateTime.Now,
                        Notes = $"سن {tooth}" + (string.IsNullOrWhiteSpace(xr.Notes) ? "" : $" — {xr.Notes}")
                    });
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save X-ray {File}", xr.FileName);
                }
            }
            return count;
        }

        // سعر المعمل وقت التشخيص — 0 لو لسه ملوش سعر معمل (بيمشي مع آخر سعر لحد ما يتحدد)
        private async Task<decimal> GetCurrentLabPriceAsync(DiagnosisCatalog diag)
        {
            if (!diag.IsLaboratoryRelated) return 0;

            var last = await _context.LaboratoryPriceHistories
                .Where(h => h.DiagnosisCatalogId == diag.Id)
                .OrderByDescending(h => h.EffectiveDate)
                .Select(h => (decimal?)h.Price)
                .FirstOrDefaultAsync();
            return last ?? 0;
        }

        private static string TeethLabel(List<VisitDiagnosis> rows)
        {
            var teeth = rows.Where(r => r.ToothNumber.HasValue).Select(r => r.ToothNumber!.Value).OrderBy(ToothArch.Rank).ToList();
            if (teeth.Count == 0) return "-";
            return teeth.Count == 1
                ? ToothArch.Label(teeth[0])
                : $"{ToothArch.Label(teeth[0])} → {ToothArch.Label(teeth[^1])}, {teeth.Count} units";
        }
    }

    /// <summary>ترتيب وتسمية الأسنان — نفس منطق الشارت (Q1=UL, Q2=UR, Q3=LL, Q4=LR).</summary>
    public static class ToothArch
    {
        private static readonly int[] Upper = Enumerable.Range(11, 8).Reverse().Concat(Enumerable.Range(21, 8)).ToArray();
        private static readonly int[] Lower = Enumerable.Range(41, 8).Reverse().Concat(Enumerable.Range(31, 8)).ToArray();

        public static bool IsValid(int t) => Upper.Contains(t) || Lower.Contains(t);

        public static int Rank(int t)
        {
            var i = Array.IndexOf(Upper, t);
            if (i >= 0) return i;
            i = Array.IndexOf(Lower, t);
            return i >= 0 ? 100 + i : 1000 + t;
        }

        public static bool IsContiguous(IEnumerable<int> teeth)
        {
            var list = teeth.Distinct().ToList();
            if (list.Count < 2) return true;
            foreach (var arch in new[] { Upper, Lower })
            {
                var idx = list.Select(t => Array.IndexOf(arch, t)).ToList();
                if (idx.All(i => i >= 0))
                {
                    idx.Sort();
                    return idx[^1] - idx[0] == idx.Count - 1;
                }
            }
            return false;
        }

        public static string Label(int t) => (t / 10) switch
        {
            1 => $"UL-{t % 10}",
            2 => $"UR-{t % 10}",
            3 => $"LL-{t % 10}",
            4 => $"LR-{t % 10}",
            _ => t.ToString()
        };
    }
}