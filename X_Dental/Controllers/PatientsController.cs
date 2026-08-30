// ============================================================
// 📁 File: Controllers/PatientsController.cs (FINAL)
// ============================================================

using AutoMapper;
using Dental.Core.Pagination;
using Dental.Models.DataTables;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using Dental.Modules.Patients.InterfacesPatients;
using Dental.Modules.Patients.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static Dental.Constants.EnumHelper;
using static Dental.Constants.Permissions;

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
                p.IsActive && (p.PatientType == Models.Patient.PatientType.General || p.PatientType == Models.Patient.PatientType.Both));
            var ortho = await _context.Patients.CountAsync(p =>
                p.IsActive && (p.PatientType == Models.Patient.PatientType.Orthodontics || p.PatientType == Models.Patient.PatientType.Both));
            var both = await _context.Patients.CountAsync(p =>
                p.IsActive && p.PatientType == Models.Patient.PatientType.Both);
            return Json(new { total, general, ortho, both });
        }

        private static List<ToothSurfaceDto> ParseSurfaces(string? surfacesJson)
        {
            if (string.IsNullOrWhiteSpace(surfacesJson))
                return new List<ToothSurfaceDto>();

            try
            {
                var raw = System.Text.Json.JsonSerializer.Deserialize<List<ToothSurfaceRequest>>(surfacesJson) ?? new();
                return raw.Select(s => new ToothSurfaceDto
                {
                    Surface = s.Surface switch
                    {
                        "M" => ToothSurface.Mesial,
                        "D" => ToothSurface.Distal,
                        "B" => ToothSurface.Buccal,
                        "L" => ToothSurface.Lingual,
                        "O" => ToothSurface.Occlusal,
                        "I" => ToothSurface.Incisal,
                        _ => ToothSurface.Occlusal
                    },
                    Condition = Enum.TryParse<SurfaceCondition>(s.Condition, true, out var cond)
                        ? cond
                        : SurfaceCondition.Healthy,
                    Notes = string.Empty
                }).ToList();
            }
            catch
            {
                // بيانات قديمة/تالفة — منسيبهاش توقف الصفحة كلها
                return new List<ToothSurfaceDto>();
            }
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
                    query = query.Where(p => p.PatientType == Models.Patient.PatientType.General || p.PatientType == Models.Patient.PatientType.Both);
                else if (categoryFilter == "Orthodontics")
                    query = query.Where(p => p.PatientType == Models.Patient.PatientType.Orthodontics || p.PatientType == Models.Patient.PatientType.Both);

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
                        "mobile" => isAsc ? query.OrderBy(p => p.Mobile) : query.OrderByDescending(p => p.Mobile),
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
                        Phone = p.Mobile,
                        Age = p.DateOfBirth.HasValue ? DateTime.Now.Year - p.DateOfBirth.Value.Year : null,
                        GenderName = p.Gender != null ? p.Gender.NameAr : null,
                        IsActive = p.IsActive,
                        HasGeneralVisits = p.Visits.Any(v => v.Category == VisitCategory.General),
                        HasOrthodonticsVisits = p.Visits.Any(v => v.Category == VisitCategory.Orthodontics),
                        // في الـ .Select(p => new PatientListDto { ... }) ضيف:
                        PatientType = p.PatientType

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

            var visits = await _context.Visits
                .Where(v => v.PatientId == id)
                .Include(v => v.Procedures).ThenInclude(p => p.Procedure)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();

            // ── التاريخ الطبي (Sessions) — من الزيارات الفعلية ──
            var sessions = visits.Select(v => new SessionDto
            {
                Id = v.Id,
                PatientId = v.PatientId,
                SessionDate = v.VisitDate,
                ProcedureName = v.Procedures.Any()
                    ? string.Join("، ", v.Procedures.Select(p => p.Procedure.NameAr))
                    : (v.ChiefComplaint ?? "زيارة عامة"),
                Description = v.ChiefComplaint ?? string.Empty,
                Notes = v.Notes ?? string.Empty,
                Status = v.Status switch
                {
                    VisitStatus.InProgress => SessionStatus.InProgress,
                    VisitStatus.Completed => SessionStatus.Completed,
                    VisitStatus.Cancelled => SessionStatus.Cancelled,
                    _ => SessionStatus.Scheduled
                },
                Cost = v.Procedures.Sum(p => p.TotalPrice),
                ToothNumber = v.Procedures.FirstOrDefault()?.ToothNumber,
                CreatedAt = v.CreatedDate
            }).ToList();

            // ── Dental Chart — مجمّع لكل سن، آخر حالة + كل العلاجات ──
            var chartEntries = await _context.DentalChartEntries
                .Where(d => d.PatientId == id)
                .OrderByDescending(d => d.Date)
                .ToListAsync();

            DentalChartDto? dentalChart = null;
            if (chartEntries.Any())
            {
                dentalChart = new DentalChartDto
                {
                    PatientId = id,
                    LastUpdated = chartEntries.Max(e => e.Date),
                    Teeth = chartEntries
                        .GroupBy(e => e.ToothNumber)
                        .Select(g =>
                        {
                            var latest = g.OrderByDescending(e => e.Date).First();
                            return new ToothDto
                            {
                                ToothNumber = g.Key,
                                ToothName = $"سن {g.Key}",
                                Status = latest.Status,
                                Notes = latest.Notes ?? string.Empty,
                                Surfaces = ParseSurfaces(latest.SurfacesJson),
                                Treatments = g.OrderByDescending(e => e.Date).Select(e => new ToothTreatmentDto
                                {
                                    Id = e.Id,
                                    ProcedureName = e.ProcedureCode ?? "-",
                                    TreatmentDate = e.Date,
                                    Notes = e.Notes ?? string.Empty
                                }).ToList()
                            };
                        })
                        .OrderBy(t => t.ToothNumber)
                        .ToList()
                };
            }

            // ── الأشعة ──
            var xrays = await _context.PatientXRays
                .Where(x => x.PatientId == id)
                .OrderByDescending(x => x.TakenDate)
                .Select(x => new XRayDto
                {
                    Id = x.Id,
                    PatientId = x.PatientId,
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    ImageUrl = x.FilePath,
                    Description = x.Notes ?? string.Empty,
                    TakenDate = x.TakenDate,
                    CreatedAt = x.CreatedDate
                }).ToListAsync();

            // ── المستندات ──
            var documents = await _context.PatientDocuments
                .Include(d => d.DocumentType)
                .Where(d => d.PatientId == id && d.IsActive)
                .OrderByDescending(d => d.UploadedDate)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    PatientId = d.PatientId,
                    DocumentTypeName = d.DocumentType.NameAr,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    FileExtension = d.FileExtension,
                    FileSize = d.FileSize,
                    Description = d.Description,
                    UploadedDate = d.UploadedDate
                }).ToListAsync();

            var appointmentsCount = await _context.Appointments
                .CountAsync(a => a.PatientId == id
                    && a.AppointmentDate >= DateTime.Now
                    && a.Status != AppointmentStatus.Cancelled);

            var model = new PatientProfileViewModel
            {
                Patient = result.Data,
                VisitsCount = visits.Count,
                AppointmentsCount = appointmentsCount,
                TotalPaid = visits.Sum(v => v.PaidAmount),
                TotalDue = visits.Sum(v => v.Remaining),
                DentalChart = dentalChart,
                Sessions = sessions,
                XRays = xrays,
                Prescriptions = new List<PrescriptionDto>(),   // نكملها لاحقًا
                Photos = new List<PhotoDto>(),                 // نكملها لاحقًا
                Payments = new List<PaymentDto>(),              // نكملها لاحقًا
                Documents = documents
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
            _logger.LogWarning("DEBUG: GenderId={GenderId}, PatientType={PatientType}", dto.GenderId, dto.PatientType);

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
                    Category = dto.PatientType switch
                    {
                        Models.Patient.PatientType.General => VisitCategory.General,
                        Models.Patient.PatientType.Orthodontics => VisitCategory.Orthodontics,
                        Models.Patient.PatientType.Both => VisitCategory.General, // أو أي منطق تاني لو "كلا الملفين" محتاجة تفتح Visit من كل نوع
                        _ => VisitCategory.General
                    }
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
                GenderId = patient.Gender?.Id ?? 0,     
                DateOfBirth = patient.DateOfBirth,
                BloodGroupId = patient.BloodGroup?.Id,
                NationalId = patient.NationalId,
                Phone = patient.Phone,
                Mobile = patient.Mobile,
                Email = patient.Email,
                Address = patient.Address,
                Occupation = patient.Occupation,
                Notes = patient.Notes,
                PatientType = patient.PatientType
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Edit(UpdatePatientDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }

            try
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
                return RedirectToAction("Details", "Patients", new { id = dto.Id});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", dto.Id);
                ModelState.AddModelError("", "حدث خطأ أثناء تعديل بيانات المريض");
                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }
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


        [HttpGet]
        public async Task<IActionResult> NewSession(int patientId, VisitCategory? category = null)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var resolvedCategory = category ?? (patient.PatientType == Dental.Models.Patient.PatientType.Orthodontics
                ? VisitCategory.Orthodontics
                : VisitCategory.General);

            var visit = new Visit
            {
                PatientId = patientId,
                VisitDate = DateTime.Now,
                Status = VisitStatus.InProgress,
                FollowUpType = FollowUpType.Normal,
                Category = resolvedCategory
            };
            await _context.Visits.AddAsync(visit);
            await _context.SaveChangesAsync();

            return RedirectToAction("Session", "Clinical", new { patientId = patient.Id, visitId = visit.Id });
        }

    }
}