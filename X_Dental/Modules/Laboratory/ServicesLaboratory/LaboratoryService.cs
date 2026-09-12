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

        public async Task<List<LaboratoryDiagnosisDto>> GetLaboratoryDiagnosesAsync()
        {
            var list = await _context.DiagnosisCatalogs
                .Where(d => d.IsLaboratoryRelated && !d.IsDeleted)
                .OrderBy(d => d.NameAr)
                .Select(d => new LaboratoryDiagnosisDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    NameAr = d.NameAr,
                    CurrentPrice = d.Price,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            var ids = list.Select(x => x.Id).ToList();
            var lastHistory = await _context.LaboratoryPriceHistories
                .Where(h => ids.Contains(h.DiagnosisCatalogId))
                .GroupBy(h => h.DiagnosisCatalogId)
                .Select(g => g.OrderByDescending(h => h.EffectiveDate).First())
                .ToListAsync();

            foreach (var item in list)
            {
                var last = lastHistory.FirstOrDefault(h => h.DiagnosisCatalogId == item.Id);
                item.LastUpdated = last?.EffectiveDate;
                item.UpdatedBy = last?.ChangedBy;
            }

            return list;
        }

        public async Task<List<LaboratoryPriceHistoryDto>> GetPriceHistoryAsync(int diagnosisCatalogId)
        {
            return await _context.LaboratoryPriceHistories
                .Where(h => h.DiagnosisCatalogId == diagnosisCatalogId)
                .OrderByDescending(h => h.EffectiveDate)
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

            // ⭐ سجل تاريخي — الجلسات القديمة بيّناها بالـ Snapshot فمش هتتأثر
            _context.LaboratoryPriceHistories.Add(new LaboratoryPriceHistory
            {
                DiagnosisCatalogId = diag.Id,
                Price = req.NewPrice,
                EffectiveDate = DateTime.Now,
                ChangedBy = changedBy,
                Notes = req.Notes
            });

            diag.Price = req.NewPrice;
            diag.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم تحديث سعر المعمل بنجاح.");
        }

        public async Task<LaboratorySummaryDto> GetSummaryAsync(LaboratoryFilterDto filter)
        {
            var query =
                from vd in _context.VisitDiagnoses
                join v in _context.Visits on vd.VisitId equals v.Id
                join d in _context.DiagnosisCatalogs on vd.DiagnosisCatalogId equals d.Id
                where d.IsLaboratoryRelated
                select new { vd, v, d };

            if (filter.DateFrom.HasValue)
                query = query.Where(x => x.v.VisitDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(x => x.v.VisitDate <= filter.DateTo.Value);
            if (filter.DiagnosisCatalogId.HasValue)
                query = query.Where(x => x.d.Id == filter.DiagnosisCatalogId.Value);
            if (filter.PatientId.HasValue)
                query = query.Where(x => x.v.PatientId == filter.PatientId.Value);

            var rows = await query.ToListAsync();

            var grouped = rows
                .GroupBy(x => new { x.d.Id, x.d.NameAr, x.d.Price })
                .Select(g => new LaboratorySummaryItemDto
                {
                    DiagnosisCatalogId = g.Key.Id,
                    NameAr = g.Key.NameAr,
                    Count = g.Count(),
                    CurrentPrice = g.Key.Price,
                    Total = g.Sum(x => x.vd.LabPriceSnapshot ?? x.d.Price) // ⭐ historical accuracy
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
    }
}