// ============================================================
// 📁 File: Modules/Laboratory/ServicesLaboratory/LaboratoryService.cs
// ⭐ Multi-Unit + LabPriceSnapshot + Query واحدة للجدول والـ KPIs
// ============================================================
using Dental.Core.Results;
using Dental.Data;
using Dental.Models.Clinic;
using Dental.Modules.Laboratory.DTOs;
using Dental.Modules.Laboratory.InterfacesLaboratory;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Laboratory.ServicesLaboratory
{
    public class LaboratoryService : ILaboratoryService
    {
        private readonly ApplicationDbContext _context;
        public LaboratoryService(ApplicationDbContext context) => _context = context;

        // ترتيب الأسنان على امتداد كل فك — نفس ترتيب الشارت والـ ClinicalController
        private static readonly int[] UpperArchOrder =
            Enumerable.Range(11, 8).Reverse().Concat(Enumerable.Range(21, 8)).ToArray(); // 18..11, 21..28
        private static readonly int[] LowerArchOrder =
            Enumerable.Range(41, 8).Reverse().Concat(Enumerable.Range(31, 8)).ToArray(); // 48..41, 31..38

        private static int ArchRank(int tooth)
        {
            var i = Array.IndexOf(UpperArchOrder, tooth);
            if (i >= 0) return i;
            i = Array.IndexOf(LowerArchOrder, tooth);
            return i >= 0 ? 100 + i : 1000 + tooth;
        }

        // ════════════════════════════════════════════════════════════
        // ⭐ الجدول + الـ KPIs (Endpoint واحد للصفحة)
        // ════════════════════════════════════════════════════════════
        public Task<LaboratoryDashboardDto> GetDashboardAsync(LaboratoryFilterDto? filter = null)
            => BuildDashboardAsync(filter);

        public async Task<List<LaboratoryDiagnosisDto>> GetLaboratoryDiagnosesAsync(LaboratoryFilterDto? filter = null)
            => (await BuildDashboardAsync(filter)).Diagnoses;

        public async Task<LaboratorySummaryDto> GetSummaryAsync(LaboratoryFilterDto filter)
        {
            var dash = await BuildDashboardAsync(filter);

            var items = dash.Diagnoses
                .Where(d => d.UnitsCount > 0)
                .Select(d => new LaboratorySummaryItemDto
                {
                    DiagnosisCatalogId = d.Id,
                    NameAr = d.NameAr,
                    NameEn = d.NameEn,
                    Count = d.UnitsCount,
                    CurrentPrice = d.CurrentPrice,
                    Total = d.Total
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            return new LaboratorySummaryDto
            {
                Items = items,
                TotalAmount = items.Sum(x => x.Total),
                TotalCount = items.Sum(x => x.Count)
            };
        }

        private async Task<LaboratoryDashboardDto> BuildDashboardAsync(LaboratoryFilterDto? filter)
        {
            // 1) التشخيصات المرتبطة بالمعمل
            var diagQuery = _context.DiagnosisCatalogs
                .AsNoTracking()
                .Where(d => d.IsLaboratoryRelated && !d.IsDeleted);

            if (filter?.DiagnosisCatalogId is int onlyDiagId)
                diagQuery = diagQuery.Where(d => d.Id == onlyDiagId);

            var list = await diagQuery
                .OrderBy(d => d.NameEn)
                .Select(d => new LaboratoryDiagnosisDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    NameAr = d.NameAr,
                    NameEn = d.NameEn,
                    CurrentPrice = d.Price, // fallback لحد ما يتسجل أول سعر معمل
                    IsActive = d.IsActive
                })
                .ToListAsync();

            var dto = new LaboratoryDashboardDto { Diagnoses = list };
            if (list.Count == 0)
                return dto;

            var ids = list.Select(x => x.Id).ToList();

            // 2) آخر سعر معمل لكل تشخيص (من الداتابيز مباشرة، مش كل التاريخ)
            var latestByDiag = await GetLatestLabPricesAsync(ids);
            foreach (var item in list)
            {
                if (latestByDiag.TryGetValue(item.Id, out var last))
                {
                    item.CurrentPrice = last.Price;
                    item.LastUpdated = last.EffectiveDate;
                    item.UpdatedBy = last.ChangedBy;
                }
            }
            var currentPriceById = list.ToDictionary(x => x.Id, x => x.CurrentPrice);

            // 3) الحالات داخل الفترة — Projection بالأعمدة المطلوبة بس
            var dateFrom = filter?.DateFrom?.Date;
            var dateToExclusive = filter?.DateTo?.Date.AddDays(1);

            var caseQuery =
                from vd in _context.VisitDiagnoses.AsNoTracking()
                join v in _context.Visits.AsNoTracking() on vd.VisitId equals v.Id
                join p in _context.Patients.AsNoTracking() on v.PatientId equals p.Id
                where ids.Contains(vd.DiagnosisCatalogId)
                select new
                {
                    VdId = vd.Id,
                    vd.DiagnosisCatalogId,
                    Tooth = (int?)vd.ToothNumber,
                    vd.GroupId,
                    Snapshot = (decimal?)vd.LabPriceSnapshot,
                    VisitId = v.Id,
                    v.VisitDate,
                    PatientId = p.Id,
                    p.FullNameAr,
                    p.FullNameEn,
                    p.Mobile,
                    p.Phone
                };

            if (dateFrom.HasValue)
                caseQuery = caseQuery.Where(x => x.VisitDate >= dateFrom.Value);
            if (dateToExclusive.HasValue)
                caseQuery = caseQuery.Where(x => x.VisitDate < dateToExclusive.Value);
            if (filter?.PatientId is int onlyPatientId)
                caseQuery = caseQuery.Where(x => x.PatientId == onlyPatientId);

            var rows = await caseQuery.ToListAsync();

            // 4) تجميع: مجموعة Multi-Unit = حالة واحدة، والسن المنفرد = حالة لوحده
            //    المبلغ = مجموع LabPriceSnapshot لكل وحدة (ولو مفيش Snapshot → آخر سعر معمل)
            var casesByDiag = rows
                .GroupBy(r => new
                {
                    r.DiagnosisCatalogId,
                    CaseKey = r.GroupId.HasValue ? r.GroupId.Value.ToString() : "vd-" + r.VdId
                })
                .Select(g =>
                {
                    var first = g.First();
                    var fallbackPrice = currentPriceById[g.Key.DiagnosisCatalogId];
                    var units = g.Count();

                    return new
                    {
                        g.Key.DiagnosisCatalogId,
                        Case = new LaboratoryCaseDto
                        {
                            VisitId = first.VisitId,
                            VisitDate = first.VisitDate,
                            PatientId = first.PatientId,
                            PatientName = !string.IsNullOrWhiteSpace(first.FullNameAr) ? first.FullNameAr : (first.FullNameEn ?? string.Empty),
                            Mobile = !string.IsNullOrWhiteSpace(first.Mobile) ? first.Mobile : first.Phone,
                            Units = units,
                            IsMultiUnit = first.GroupId.HasValue && units > 1,
                            Teeth = g.Where(x => x.Tooth.HasValue && x.Tooth.Value > 0)
                                     .Select(x => x.Tooth!.Value)
                                     .Distinct()
                                     .OrderBy(ArchRank)
                                     .ToList(),
                            Amount = g.Sum(x => x.Snapshot.HasValue && x.Snapshot.Value > 0 ? x.Snapshot.Value : fallbackPrice)
                        }
                    };
                })
                .GroupBy(x => x.DiagnosisCatalogId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Case)
                          .OrderByDescending(c => c.VisitDate)
                          .ThenByDescending(c => c.VisitId)
                          .ToList());

            foreach (var item in list)
            {
                if (casesByDiag.TryGetValue(item.Id, out var cases))
                {
                    item.Cases = cases;
                    item.CasesCount = cases.Count;
                    item.UnitsCount = cases.Sum(c => c.Units);
                    item.Total = cases.Sum(c => c.Amount);
                }
            }

            // 5) الـ KPIs
            dto.TotalCases = list.Sum(x => x.CasesCount);
            dto.TotalUnits = list.Sum(x => x.UnitsCount);
            dto.TotalAmount = list.Sum(x => x.Total);
            dto.DiagnosisTypes = list.Count(x => x.CasesCount > 0);

            return dto;
        }

        public async Task<List<LaboratoryPriceHistoryDto>> GetPriceHistoryAsync(int diagnosisCatalogId)
        {
            return await _context.LaboratoryPriceHistories
                .AsNoTracking()
                .Where(h => h.DiagnosisCatalogId == diagnosisCatalogId)
                .OrderByDescending(h => h.EffectiveDate)
                .ThenByDescending(h => h.Id)
                .Select(h => new LaboratoryPriceHistoryDto
                {
                    Id = h.Id,
                    Price = h.Price,
                    EffectiveDate = h.EffectiveDate,
                    ChangedBy = h.ChangedBy,
                    Notes = h.Notes
                })
                .ToListAsync();
        }

        public async Task<ServiceResult<bool>> UpdatePriceAsync(UpdateLaboratoryPriceRequest req, string changedBy)
        {
            if (req.NewPrice < 0)
                return ServiceResult<bool>.Fail("السعر لا يمكن أن يكون أقل من صفر.");

            var diag = await _context.DiagnosisCatalogs
                .FirstOrDefaultAsync(d => d.Id == req.DiagnosisCatalogId && !d.IsDeleted);

            if (diag == null)
                return ServiceResult<bool>.Fail("التشخيص غير موجود.");

            if (!diag.IsLaboratoryRelated)
                return ServiceResult<bool>.Fail("هذا التشخيص غير مرتبط بالمعمل.");

            // سعر المعمل مستقل تمامًا عن DiagnosisCatalog.Price (سعر المريض)
            _context.LaboratoryPriceHistories.Add(new LaboratoryPriceHistory
            {
                DiagnosisCatalogId = diag.Id,
                Price = req.NewPrice,
                EffectiveDate = DateTime.Now,
                ChangedBy = changedBy,
                Notes = req.Notes
            });

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تحديث سعر المعمل بنجاح.");
        }

        // ⭐ آخر سعر لكل تشخيص بيتحسب في SQL (ROW_NUMBER) بدل تحميل كل التاريخ في الميموري
        private async Task<Dictionary<int, (decimal Price, DateTime EffectiveDate, string? ChangedBy)>> GetLatestLabPricesAsync(List<int> diagnosisIds)
        {
            if (diagnosisIds.Count == 0)
                return new();

            var latest = await _context.LaboratoryPriceHistories
                .AsNoTracking()
                .Where(h => diagnosisIds.Contains(h.DiagnosisCatalogId))
                .GroupBy(h => h.DiagnosisCatalogId)
                .Select(g => g
                    .OrderByDescending(h => h.EffectiveDate)
                    .ThenByDescending(h => h.Id)
                    .Select(h => new { h.DiagnosisCatalogId, h.Price, h.EffectiveDate, h.ChangedBy })
                    .First())
                .ToListAsync();

            return latest.ToDictionary(
                x => x.DiagnosisCatalogId,
                x => (x.Price, x.EffectiveDate, x.ChangedBy));
        }
    }
}