// ============================================================
// 📁 File: PatientService.cs
// ============================================================

using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dental.Core.Pagination;
using Dental.Core.Results;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using Dental.Modules.Patients.InterfacesPatients;
using Microsoft.EntityFrameworkCore;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientService(ApplicationDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
    }



    // ── Helper: Generate Patient Code ──
    public async Task<string> GeneratePatientCodeAsync()
    {
        var lastPatient = await _context.Patients
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        return lastPatient == null
            ? "PAT000001"
            : $"PAT{(lastPatient.Id + 1):D6}";
    }

    // ── Helper: Check Mobile Exists ──
    public async Task<bool> ExistsMobileAsync(string mobile, int? excludeId = null)
    {
        return await _context.Patients
            .AnyAsync(x => x.Mobile == mobile && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    // ── Helper: Check Phone Exists ──
    public async Task<bool> ExistsPhoneAsync(string phone, int? excludeId = null)
    {
        return await _context.Patients
            .AnyAsync(x => x.Phone == phone && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    // ── Helper: Check National ID Exists ──
    public async Task<bool> ExistsNationalIdAsync(string nationalId, int? excludeId = null)
    {
        return await _context.Patients
            .AnyAsync(x => x.NationalId == nationalId && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    // ── Search: Quick Lookup ──
    public async Task<List<PatientLookupDto>> SearchAsync(string keyword)
    {
        keyword = keyword.Trim();

        return await _context.Patients
            .Where(x => x.IsActive && (
                x.FullNameAr.Contains(keyword)
                || x.PatientCode.Contains(keyword)
                || x.Phone!.Contains(keyword)
                || x.Mobile.Contains(keyword)))
            .ProjectTo<PatientLookupDto>(_mapper.ConfigurationProvider)
            .Take(20)
            .ToListAsync();
    }

    // ── Get By Id: FULL DATA + Visit Stats ──
    public async Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.Gender)
            .Include(p => p.BloodGroup)
            .Include(p => p.MedicalInfo)
            .Include(p => p.Visits)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient == null)
            return ServiceResult<PatientDetailsDto>.Fail("Patient not found.");

        var dto = _mapper.Map<PatientDetailsDto>(patient);

        var activeVisits = patient.Visits.Where(v => v.IsActive).ToList();

        dto.VisitsCount = activeVisits.Count;
        dto.TotalPaid = activeVisits.Sum(v => v.PaidAmount);
        dto.TotalDue = activeVisits.Sum(v => v.RemainingAmount);
        dto.GeneralVisitsCount = activeVisits.Count(v => v.Category == VisitCategory.General);
        dto.OrthodonticsVisitsCount = activeVisits.Count(v => v.Category == VisitCategory.Orthodontics);
        dto.NextVisitDate = activeVisits
            .Where(v => v.NextVisitDate.HasValue && v.NextVisitDate.Value.Date >= DateTime.Today)
            .OrderBy(v => v.NextVisitDate)
            .Select(v => v.NextVisitDate)
            .FirstOrDefault();

        return ServiceResult<PatientDetailsDto>.Ok(dto);
    }

    // ── Create ──
    public async Task<ServiceResult<int>> CreateAsync(CreatePatientDto dto)
    {
        if (await ExistsMobileAsync(dto.Mobile))
            return ServiceResult<int>.Fail("رقم الموبايل مسجل بالفعل لمريض آخر.");

        if (!string.IsNullOrWhiteSpace(dto.Phone) && await ExistsPhoneAsync(dto.Phone))
            return ServiceResult<int>.Fail("رقم التليفون مسجل بالفعل لمريض آخر.");

        if (!string.IsNullOrWhiteSpace(dto.NationalId) && await ExistsNationalIdAsync(dto.NationalId))
            return ServiceResult<int>.Fail("الرقم القومي مسجل بالفعل لمريض آخر.");

        var patient = _mapper.Map<Patient>(dto);
        patient.PatientCode = await GeneratePatientCodeAsync();
        patient.IsActive = true;
        patient.CreatedBy = 1;
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        return ServiceResult<int>.Ok(patient.Id, "تم إضافة المريض بنجاح.");
    }

    // ── Update ──
    public async Task<ServiceResult<bool>> UpdateAsync(UpdatePatientDto dto)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (patient == null)
            return ServiceResult<bool>.Fail("Patient not found.");

        if (!string.IsNullOrWhiteSpace(dto.Mobile) && await ExistsMobileAsync(dto.Mobile, dto.Id))
            return ServiceResult<bool>.Fail("رقم الموبايل مسجل بالفعل لمريض آخر.");

        if (!string.IsNullOrWhiteSpace(dto.Phone) && await ExistsPhoneAsync(dto.Phone, dto.Id))
            return ServiceResult<bool>.Fail("رقم التليفون مسجل بالفعل لمريض آخر.");

        if (!string.IsNullOrWhiteSpace(dto.NationalId) && await ExistsNationalIdAsync(dto.NationalId, dto.Id))
            return ServiceResult<bool>.Fail("الرقم القومي مسجل بالفعل لمريض آخر.");

        patient.GenderId = dto.GenderId;
        patient.BloodGroupId = dto.BloodGroupId;

        _mapper.Map(dto, patient);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "تم تعديل بيانات المريض بنجاح.");
    }

    // ── Delete (Soft Deactivate) ──
    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(x => x.Id == id);
        if (patient == null)
            return ServiceResult<bool>.Fail("Patient not found.");

        patient.IsActive = false;
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "تم تعطيل المريض بنجاح.");
    }
    // ── Get Paged List (Summary Only) ──
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedRequest request)
    {
        var query = _context.Patients
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.PatientCode.Contains(search) ||
                x.FullNameAr.Contains(search) ||
                x.FullNameEn.Contains(search) ||
                (x.Phone != null && x.Phone.Contains(search)));
        }

        var total = await query.CountAsync();

        // ✅ Use Include + Map instead of ProjectTo for complex types
        var patientsEntity = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(x => x.Gender)
            .ToListAsync();

        var patients = _mapper.Map<List<PatientDto>>(patientsEntity);

        var result = new PagedResult<PatientDto>
        {
            Items = patients,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return ServiceResult<PagedResult<PatientDto>>.Ok(result);
    }

}