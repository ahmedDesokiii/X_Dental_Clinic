// ============================================================
// 📁 File: Modules/Laboratory/ServicesLaboratory/LaboratoryService.cs
// استبدل الملف بالكامل بالنسخة دي
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

        public async Task<List<LaboratoryDiagnosisDto>> GetLaboratoryDiagnosesAsync(LaboratoryFilterDto? filter = null)
        {
            var list = await _context.DiagnosisCatalogs
                .AsNoTracking()
                .Where(d => d.IsLaboratoryRelated && !d.IsDeleted)
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

            if (list.Count == 0)
                return list;

            var ids = list.Select(x => x.Id).ToList();
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

            var dateFrom = filter?.DateFrom?.Date;
            var dateToExclusive = filter?.DateTo.HasValue == true
                ? filter!.DateTo!.Value.Date.AddDays(1)
                : (DateTime?)null;

            var caseQuery =
                from vd in _context.VisitDiagnoses.AsNoTracking()
                join v in _context.Visits.AsNoTracking() on vd.VisitId equals v.Id
                where ids.Contains(vd.DiagnosisCatalogId)
                select new { vd.DiagnosisCatalogId, v.VisitDate };

            if (dateFrom.HasValue)
                caseQuery = caseQuery.Where(x => x.VisitDate >= dateFrom.Value);
            if (dateToExclusive.HasValue)
                caseQuery = caseQuery.Where(x => x.VisitDate < dateToExclusive.Value);

            var cases = await caseQuery.ToListAsync();

            var totals = cases
                .GroupBy(x => x.DiagnosisCatalogId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var currentLabPrice = latestByDiag.TryGetValue(g.Key, out var last)
                            ? last.Price
                            : list.First(d => d.Id == g.Key).CurrentPrice;
                        return (Count: g.Count(), Total: g.Count() * currentLabPrice);
                    });

            foreach (var item in list)
            {
                if (totals.TryGetValue(item.Id, out var t))
                {
                    item.CasesCount = t.Count;
                    item.Total = t.Total;
                }
            }

            return list;
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

        public async Task<LaboratorySummaryDto> GetSummaryAsync(LaboratoryFilterDto filter)
        {
            var dateFrom = filter.DateFrom?.Date;
            var dateToExclusive = filter.DateTo.HasValue
                ? filter.DateTo.Value.Date.AddDays(1)
                : (DateTime?)null;

            var query =
                from vd in _context.VisitDiagnoses.AsNoTracking()
                join v in _context.Visits.AsNoTracking() on vd.VisitId equals v.Id
                join d in _context.DiagnosisCatalogs.AsNoTracking() on vd.DiagnosisCatalogId equals d.Id
                where d.IsLaboratoryRelated && !d.IsDeleted
                select new { vd, v, d };

            if (dateFrom.HasValue)
                query = query.Where(x => x.v.VisitDate >= dateFrom.Value);
            if (dateToExclusive.HasValue)
                query = query.Where(x => x.v.VisitDate < dateToExclusive.Value);
            if (filter.DiagnosisCatalogId.HasValue)
                query = query.Where(x => x.d.Id == filter.DiagnosisCatalogId.Value);
            if (filter.PatientId.HasValue)
                query = query.Where(x => x.v.PatientId == filter.PatientId.Value);

            var rows = await query
                .Select(x => new
                {
                    DiagnosisId = x.d.Id,
                    x.d.NameAr,
                    x.d.NameEn,
                    CatalogPrice = x.d.Price
                })
                .ToListAsync();

            var ids = rows.Select(r => r.DiagnosisId).Distinct().ToList();
            var latestByDiag = await GetLatestLabPricesAsync(ids);

            var grouped = rows
                .GroupBy(x => new { x.DiagnosisId, x.NameAr, x.NameEn })
                .Select(g =>
                {
                    var catalogPrice = g.First().CatalogPrice;
                    var currentLabPrice = latestByDiag.TryGetValue(g.Key.DiagnosisId, out var last)
                        ? last.Price
                        : catalogPrice;

                    return new LaboratorySummaryItemDto
                    {
                        DiagnosisCatalogId = g.Key.DiagnosisId,
                        NameAr = g.Key.NameAr,
                        NameEn = g.Key.NameEn,
                        Count = g.Count(),
                        CurrentPrice = currentLabPrice,
                        // عدد الحالات في الفترة × آخر سعر من Edit Price
                        // مش DiagnosisCatalog.Price (سعر التشخيص الفعلي للمريض)
                        Total = g.Count() * currentLabPrice
                    };
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            return new LaboratorySummaryDto
            {
                Items = grouped,
                TotalAmount = grouped.Sum(x => x.Total),
                TotalCount = grouped.Sum(x => x.Count)
            };
        }

        private async Task<Dictionary<int, LaboratoryPriceHistory>> GetLatestLabPricesAsync(List<int> diagnosisIds)
        {
            if (diagnosisIds.Count == 0)
                return new Dictionary<int, LaboratoryPriceHistory>();

            var histories = await _context.LaboratoryPriceHistories
                .AsNoTracking()
                .Where(h => diagnosisIds.Contains(h.DiagnosisCatalogId))
                .ToListAsync();

            return histories
                .GroupBy(h => h.DiagnosisCatalogId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(h => h.EffectiveDate).ThenByDescending(h => h.Id).First());
        }
    }
}