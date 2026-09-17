// ============================================================
// 📁 File: Modules/Diagnosis/ServicesDiagnosis/DiagnosisService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Data;
using Dental.Models.Clinic;
using Dental.Modules.Diagnosis.DTOs;
using Dental.Modules.Diagnosis.InterfacesDiagnosis;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Diagnosis.ServicesDiagnosis
{
    public class DiagnosisService : IDiagnosisService
    {
        private readonly ApplicationDbContext _context;

        public DiagnosisService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DiagnosisListDto>> GetAllAsync()
        {
            return await _context.DiagnosisCatalogs
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.Code)
                                .Select(d => new DiagnosisListDto
                                {
                                    Id = d.Id,
                                    Code = d.Code,
                                    NameAr = d.NameAr,
                                    NameEn = d.NameEn,
                                    Category = d.Category,
                                    CategoryName = d.Category.ToString(),
                                    Price = d.Price,
                                    Description = d.Description,
                                    Notes = d.Notes,
                                    IsActive = d.IsActive,
                                    IsLaboratoryRelated = d.IsLaboratoryRelated
                                })
                .ToListAsync();
        }

        public async Task<ServiceResult<DiagnosisFormDto>> GetByIdAsync(int id)
        {
            var entity = await _context.DiagnosisCatalogs
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (entity == null)
                return ServiceResult<DiagnosisFormDto>.Fail("التشخيص غير موجود.");

            var dto = new DiagnosisFormDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                Category = entity.Category,
                Price = entity.Price,
                Description = entity.Description,
                Notes = entity.Notes,
                IsActive = entity.IsActive,
                IsLaboratoryRelated = entity.IsLaboratoryRelated
            };

            return ServiceResult<DiagnosisFormDto>.Ok(dto);
        }

        public async Task<ServiceResult<int>> CreateAsync(DiagnosisFormDto dto)
        {
            if (await ExistsCodeAsync(dto.Code))
                return ServiceResult<int>.Fail("كود التشخيص ده مستخدم بالفعل.");

            var entity = new DiagnosisCatalog
            {
                Code = dto.Code.Trim(),
                NameEn = dto.NameEn.Trim(),
                NameAr = string.IsNullOrWhiteSpace(dto.NameAr) ? dto.NameEn.Trim() : dto.NameAr.Trim(),
                Category = dto.Category,
                Price = dto.Price,
                Description = dto.Description,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                IsLaboratoryRelated = dto.IsLaboratoryRelated
            };

            _context.DiagnosisCatalogs.Add(entity);
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(entity.Id, "تم إضافة التشخيص بنجاح.");
        }

        public async Task<ServiceResult<bool>> UpdateAsync(DiagnosisFormDto dto)
        {
            var entity = await _context.DiagnosisCatalogs
                .FirstOrDefaultAsync(d => d.Id == dto.Id && !d.IsDeleted);

            if (entity == null)
                return ServiceResult<bool>.Fail("التشخيص غير موجود.");

            if (await ExistsCodeAsync(dto.Code, dto.Id))
                return ServiceResult<bool>.Fail("كود التشخيص ده مستخدم بالفعل.");

            entity.Code = dto.Code.Trim();
            entity.NameEn = dto.NameEn.Trim();
            entity.NameAr = string.IsNullOrWhiteSpace(dto.NameAr) ? dto.NameEn.Trim() : dto.NameAr.Trim();
            entity.Category = dto.Category;
            entity.Price = dto.Price;
            entity.Description = dto.Description;
            entity.Notes = dto.Notes;
            entity.IsActive = dto.IsActive;
            entity.IsLaboratoryRelated = dto.IsLaboratoryRelated;
            entity.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "تم تعديل التشخيص بنجاح.");
        }

        public async Task<ServiceResult<bool>> ToggleActiveAsync(int id)
        {
            var entity = await _context.DiagnosisCatalogs
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (entity == null)
                return ServiceResult<bool>.Fail("التشخيص غير موجود.");

            entity.IsActive = !entity.IsActive;
            entity.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, entity.IsActive ? "تم تفعيل التشخيص." : "تم إيقاف التشخيص.");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var entity = await _context.DiagnosisCatalogs
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (entity == null)
                return ServiceResult<bool>.Fail("التشخيص غير موجود.");

            var isUsed = await _context.VisitDiagnoses.AnyAsync(v => v.DiagnosisCatalogId == id);
            if (isUsed)
            {
                // متسجل في زيارات فعلية -> حذف منطقي فقط (Soft delete) حفاظًا على السجل التاريخي
                entity.IsDeleted = true;
                entity.IsActive = false;
                entity.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "التشخيص مستخدم في زيارات سابقة، تم إخفاؤه من القائمة.");
            }

            // مش مستخدم في أي زيارة -> حذف نهائي
            _context.DiagnosisCatalogs.Remove(entity);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "تم حذف التشخيص بنجاح.");
        }

        public async Task<List<DiagnosisLookupDto>> GetActiveLookupAsync()
        {
            return await _context.DiagnosisCatalogs
                .Where(d => d.IsActive && !d.IsDeleted)
                .OrderBy(d => d.NameAr)
                .Select(d => new DiagnosisLookupDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    NameAr = d.NameAr,
                    Price = d.Price
                })
                .ToListAsync();
        }

        public async Task<bool> ExistsCodeAsync(string code, int? excludeId = null)
        {
            var query = _context.DiagnosisCatalogs.Where(d => d.Code == code && !d.IsDeleted);
            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}