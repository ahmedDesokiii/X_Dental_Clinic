// ============================================================
// 📁 File: Controllers/PatientsController.cs (FINAL)
// ============================================================

using AutoMapper;
using Dental.Core.Pagination;
using Dental.Models.DataTables;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using Dental.Modules.Patients.InterfacesPatients;
using Dental.Modules.Patients.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static Dental.Constants.EnumHelper;

namespace Dental.Controllers
{
    public class PatientsController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PatientsController> _logger;
        private readonly IMapper _mapper;

        public PatientsController(
            IPatientService patientService,
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<PatientsController> logger,
            IMapper mapper)
        {
            _patientService = patientService;
            _context = context;
            _cache = cache;
            _logger = logger;
            _mapper = mapper;
        }

        #region 🚀 Index (Server-Side DataTables)
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientsStats()
        {
            var total = await _context.Patients.CountAsync(p => p.IsActive);
            var general = await _context.Patients.CountAsync(p =>
                p.IsActive && p.Visits.Any(v => v.Category == VisitCategory.General));
            var ortho = await _context.Patients.CountAsync(p =>
                p.IsActive && p.Visits.Any(v => v.Category == VisitCategory.Orthodontics));
            var both = await _context.Patients.CountAsync(p =>
                p.IsActive
                && p.Visits.Any(v => v.Category == VisitCategory.General)
                && p.Visits.Any(v => v.Category == VisitCategory.Orthodontics));

            return Json(new { total, general, ortho, both });
        }

        [HttpPost]
        public async Task<IActionResult> GetPatientsData([FromBody] DataTableRequest request)
        {
            try
            {
                var categoryFilter = request.CategoryFilter;

                var query = _context.Patients
                    .AsNoTracking()
                    .Include(p => p.Gender)
                    .Include(p => p.Visits)
                    .AsQueryable();

                if (categoryFilter == "General")
                {
                    query = query.Where(p => p.Visits.Any(v => v.Category == VisitCategory.General));
                }
                else if (categoryFilter == "Orthodontics")
                {
                    query = query.Where(p => p.Visits.Any(v => v.Category == VisitCategory.Orthodontics));
                }

                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    var search = request.Search.Value.Trim().ToLower();
                    query = query.Where(p =>
                        (p.FullNameAr != null && p.FullNameAr.Contains(search)) ||
                        (p.PatientCode != null && p.PatientCode.Contains(search)) ||
                        (p.Phone != null && p.Phone.Contains(search)) ||
                        (p.Mobile != null && p.Mobile.Contains(search)) ||
                        (p.NationalId != null && p.NationalId.Contains(search))
                    );
                }

                var recordsTotal = await _context.Patients.CountAsync();
                var recordsFiltered = await query.CountAsync();

                if (request.Order.Any() && request.Columns.Any())
                {
                    var order = request.Order.First();
                    var columnName = request.Columns[order.Column].Data;
                    var isAsc = order.Dir == "asc";

                    query = columnName switch
                    {
                        "patientCode" => isAsc ? query.OrderBy(p => p.PatientCode) : query.OrderByDescending(p => p.PatientCode),
                        "fullNameAr" => isAsc ? query.OrderBy(p => p.FullNameAr) : query.OrderByDescending(p => p.FullNameAr),
                        "phone" => isAsc ? query.OrderBy(p => p.Phone) : query.OrderByDescending(p => p.Phone),
                        "age" => isAsc ? query.OrderBy(p => p.DateOfBirth) : query.OrderByDescending(p => p.DateOfBirth),
                        "genderName" => isAsc
                            ? query.OrderBy(p => p.Gender != null ? p.Gender.NameAr : "")
                            : query.OrderByDescending(p => p.Gender != null ? p.Gender.NameAr : ""),
                        _ => query.OrderByDescending(p => p.Id)
                    };
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }

                var data = await query
                    .Skip(request.Start)
                    .Take(request.Length)
                    .Select(p => new PatientListDto
                    {
                        Id = p.Id,
                        PatientCode = p.PatientCode,
                        FullNameAr = p.FullNameAr,
                        Phone = p.Phone,
                        Age = p.DateOfBirth.HasValue ? DateTime.Now.Year - p.DateOfBirth.Value.Year : null,
                        GenderName = p.Gender != null ? p.Gender.NameAr : null,
                        IsActive = p.IsActive,
                        HasGeneralVisits = p.Visits.Any(v => v.Category == VisitCategory.General),
                        HasOrthodonticsVisits = p.Visits.Any(v => v.Category == VisitCategory.Orthodontics)
                    })
                    .ToListAsync();

                return Json(new DataTableResponse<PatientListDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = recordsTotal,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patients data for DataTables");
                return Json(new DataTableResponse<PatientListDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<PatientListDto>()
                });
            }
        }
        #endregion

        #region Details
        public async Task<IActionResult> Details(int id)
        {
            var result = await _patientService.GetByIdAsync(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Patient not found";
                return RedirectToAction(nameof(Index));
            }

            var model = new PatientProfileViewModel
            {
                Patient = result.Data,
                VisitsCount = result.Data.VisitsCount,
                AppointmentsCount = 0,
                TotalPaid = result.Data.TotalPaid,
                TotalDue = result.Data.TotalDue,
                DentalChart = null,
                Sessions = new List<SessionDto>(),
                XRays = new List<XRayDto>(),
                Prescriptions = new List<PrescriptionDto>(),
                Photos = new List<PhotoDto>(),
                Payments = new List<PaymentDto>()
            };

            return View(model);
        }
        #endregion

        #region Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadSelectListsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePatientDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }

            try
            {
                // 1️⃣ إنشاء المريض
                var patient = _mapper.Map<Patient>(dto);
                patient.PatientCode = await _patientService.GeneratePatientCodeAsync();
                patient.IsActive = true;
                patient.CreatedBy = 1;

                // Medical Info لو موجود في الـ DTO
                if (!string.IsNullOrWhiteSpace(dto.Allergies)
                    || !string.IsNullOrWhiteSpace(dto.ChronicDiseases)
                    || !string.IsNullOrWhiteSpace(dto.CurrentMedications))
                {
                    patient.MedicalInfo = new PatientMedicalInfo
                    {
                        Allergies = dto.Allergies,
                        ChronicDiseases = dto.ChronicDiseases,
                        CurrentMedications = dto.CurrentMedications,
                        MedicalNotes = dto.MedicalNotes,
                        SmokingStatus = dto.SmokingStatus,
                        IsPregnant = dto.IsPregnant,
                        EmergencyNotes = dto.EmergencyNotes
                    };
                }

                await _context.Patients.AddAsync(patient);
                await _context.SaveChangesAsync();

                // 2️⃣ إنشاء Visit تلقائي بعد الحفظ
                var visit = new Visit
                {
                    PatientId = patient.Id,
                    VisitDate = DateTime.Now,
                    Status = VisitStatus.InProgress,
                    FollowUpType = FollowUpType.Normal,
                    Category = VisitCategory.General // أو نحدده من PatientType
                };

                await _context.Visits.AddAsync(visit);
                await _context.SaveChangesAsync();

                TempData[Helper.MsgType] = Helper.Success;
                TempData[Helper.Title] = "تم بنجاح";
                TempData[Helper.Msg] = "تم إضافة المريض وفتح سيشن جديد";

                // 3️⃣ التوجيه للسيشن مباشرة
                return RedirectToAction("Session", "Clinical", new { patientId = patient.Id, visitId = visit.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                ModelState.AddModelError("", "حدث خطأ أثناء إضافة المريض");
                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }
        }
        #endregion

        #region Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _patientService.GetByIdAsync(id);

            if (!result.Success || result.Data == null)
                return NotFound();

            var patient = result.Data;
            await LoadSelectListsAsync(patient.Gender?.Id, patient.BloodGroup?.Id);

            var model = new UpdatePatientDto
            {
                Id = patient.Id,
                FullNameAr = patient.FullNameAr,
                FullNameEn = patient.FullNameEn,
                GenderId = (int)(patient.Gender?.Id),
                DateOfBirth = patient.DateOfBirth,
                BloodGroupId = patient.BloodGroup?.Id,
                NationalId = patient.NationalId,
                Phone = patient.Phone,
                Mobile = patient.Mobile,
                Email = patient.Email,
                Address = patient.Address,
                Occupation = patient.Occupation,
                Notes = patient.Notes,
                PatientType = patient.PatientType // ⭐ لو موجود في الـ DTO
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdatePatientDto dto)
        {
            var result = await _patientService.UpdateAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }

            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "تم بنجاح";
            TempData[Helper.Msg] = "تم تعديل بيانات المريض بنجاح";

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _patientService.DeleteAsync(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", id);
                return Json(new { success = false, message = "حدث خطأ أثناء الحذف" });
            }
        }
        #endregion

        #region Search (AJAX)
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var patients = await _context.Patients
                .AsNoTracking()
                .Where(p =>
                    (p.FullNameAr != null && p.FullNameAr.Contains(term)) ||
                    (p.PatientCode != null && p.PatientCode.Contains(term)) ||
                    (p.Phone != null && p.Phone.Contains(term)))
                .Take(20)
                .Select(p => new
                {
                    p.Id,
                    p.FullNameAr,
                    p.PatientCode,
                    p.Phone
                })
                .ToListAsync();

            return Json(patients);
        }
        #endregion

        #region ⚡ Helper Methods
        private async Task LoadSelectListsAsync(int? selectedGenderId = null, int? selectedBloodGroupId = null)
        {
            var genders = await _cache.GetOrCreateAsync("genders_select", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                return await _context.Genders
                    .AsNoTracking()
                    .Where(g => g.IsActive)
                    .ToListAsync();
            });

            var bloodGroups = await _cache.GetOrCreateAsync("blood_groups_select", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                return await _context.BloodGroups
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .ToListAsync();
            });

            ViewBag.Genders = new SelectList(genders, "Id", "NameAr", selectedGenderId);
            ViewBag.BloodGroups = new SelectList(bloodGroups, "Id", "Name", selectedBloodGroupId);
        }
        #endregion
    }
}