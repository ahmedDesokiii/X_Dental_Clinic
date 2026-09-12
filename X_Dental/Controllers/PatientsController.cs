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
        private readonly IWebHostEnvironment _env;

        public PatientsController(
            IPatientService patientService,
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<PatientsController> logger,
            IMapper mapper,
            IWebHostEnvironment env)   // ⭐ NEW
        {
            _patientService = patientService;
            _context = context;
            _cache = cache;
            _logger = logger;
            _mapper = mapper;
            _env = env;                // ⭐ NEW
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
                    .Include(p => p.Appointments)
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
                        PatientType = p.PatientType,
                        NextSession = p.Appointments
                        .Where(a => a.AppointmentDate >= DateTime.Today && a.Status != AppointmentStatus.Cancelled)
                        .OrderBy(a => a.AppointmentDate)
                        .Select(a => (DateTime?)a.AppointmentDate)
                        .FirstOrDefault(),

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

        #region ⚡ Modal (Create & Edit shared)
        [HttpGet]
        public async Task<IActionResult> Modal(int? id)
        {
            await LoadSelectListsAsync();

            bool isEdit = id.HasValue && id.Value > 0;
            ViewBag.IsEdit = isEdit;

            if (isEdit)
            {
                var result = await _patientService.GetByIdAsync(id!.Value);
                if (!result.Success || result.Data == null)
                    return NotFound();

                var p = result.Data;
                ViewBag.PatientId = p.Id;
                ViewBag.FullName = p.FullNameAr;
                ViewBag.Mobile = p.Mobile;
                ViewBag.Address = p.Address;
                ViewBag.DateOfBirth = p.DateOfBirth;
                ViewBag.GenderId = p.Gender?.Id ?? 0;
                ViewBag.PatientType = (int)p.PatientType;
                ViewBag.MedNotes = p.Notes;
            }

            // ✅ مفيش Model متبعت — كله ViewBag، مفيش Casting = مفيش Exception
            return PartialView("_PatientFormModal");
        }
        #endregion









        #region Details
        // ============================================================
        // 📁 PATCH: Controllers/PatientsController.cs — Details() method
        // ============================================================
        // استبدل الـ method بتاعة Details بالكامل بالنسخة دي.
        // الإضافات الجديدة اتحطلها تعليق ⭐ NEW عشان تلاقيها بسهولة.
        // ============================================================

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

            var completedVisits = visits.Where(v => v.Status == VisitStatus.Completed).ToList();

            var sessions = completedVisits.Select(v => new SessionDto
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

            // ── Dental Chart — مجمّع لكل سن ──
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

            // ⭐ NEW: خريطة أحدث حالة/تشخيص لكل سن — تُستخدم في الـ Tooltip الخاص بالـ Dental Chart التفاعلي
            var statusLabelsForTooltip = new Dictionary<Dental.Models.Patient.ToothStatus, string>
    {
        { Dental.Models.Patient.ToothStatus.Healthy, "سليم" },
        { Dental.Models.Patient.ToothStatus.Decayed, "تسوس" },
        { Dental.Models.Patient.ToothStatus.Treated, "معالج" },
        { Dental.Models.Patient.ToothStatus.Missing, "مفقود" },
        { Dental.Models.Patient.ToothStatus.Extracted, "مخلوع" },
        { Dental.Models.Patient.ToothStatus.RootCanal, "عصب" },
        { Dental.Models.Patient.ToothStatus.Crown, "تاج" },
        { Dental.Models.Patient.ToothStatus.Filling, "حشوة" }
    };

            if (chartEntries.Any())
            {
                var latestPerTooth = chartEntries
                    .GroupBy(d => d.ToothNumber)
                    .Select(g => g.OrderByDescending(x => x.Date).First())
                    .ToList();

                var latestVisitIds = latestPerTooth.Select(d => d.VisitId).Distinct().ToList();
                var latestDiagnoses = await _context.VisitDiagnoses
                    .Where(d => latestVisitIds.Contains(d.VisitId))
                    .Include(d => d.Diagnosis)
                    .ToListAsync();

                var toothInfoMap = latestPerTooth.ToDictionary(
                    e => e.ToothNumber.ToString(),
                    e => new
                    {
                        status = statusLabelsForTooltip.TryGetValue(e.Status, out var lbl) ? lbl : e.Status.ToString(),
                        diagnosis = latestDiagnoses.FirstOrDefault(d => d.VisitId == e.VisitId && d.ToothNumber == e.ToothNumber)?.Diagnosis?.NameAr,
                        notes = e.Notes
                    });

                ViewBag.ToothInfoMap = System.Text.Json.JsonSerializer.Serialize(toothInfoMap);
            }
            else
            {
                ViewBag.ToothInfoMap = "{}";
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

            // ⭐ NEW: أقرب موعد قادم — يتعرض في KPI
            var nextAppointmentDate = await _context.Appointments
                .Where(a => a.PatientId == id
                    && a.AppointmentDate >= DateTime.Now
                    && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => (DateTime?)a.AppointmentDate)
                .FirstOrDefaultAsync();

            var model = new PatientProfileViewModel
            {
                Patient = result.Data,
                VisitsCount = completedVisits.Count,
                AppointmentsCount = appointmentsCount,
                TotalPaid = completedVisits.Sum(v => v.PaidAmount),
                TotalDue = completedVisits.Sum(v => v.Remaining),
                DentalChart = dentalChart,
                Sessions = sessions,
                XRays = xrays,
                Prescriptions = new List<PrescriptionDto>(),
                Photos = new List<PhotoDto>(),
                Payments = new List<PaymentDto>(),
                NextAppointmentDate = nextAppointmentDate,
                Documents = documents
            };

            ViewBag.PreviousSessions = await GetPreviousSessionsAsync(id);
            ViewBag.XRayGroups = await GetXRaysGroupedAsync(id);
            ViewBag.DocumentTypes = await _context.DocumentTypes
                .Where(d => d.IsActive)
                .ToListAsync();

            return View(model);
        }

        #region Documents Upload/Delete
        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadDocuments(int patientId, int documentTypeId, string? description, List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return Json(new { success = false, message = "No files were selected." });

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return Json(new { success = false, message = "Patient not found." });

            var allowedExtensions = new[] { ".pdf", ".xls", ".xlsx", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp" };
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadsRoot);

            var savedFiles = new List<object>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                    continue; // skip unsupported types silently

                var safeName = $"{patientId}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsRoot, safeName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var doc = new PatientDocument
                {
                    PatientId = patientId,
                    DocumentTypeId = documentTypeId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/documents/{safeName}",
                    FileExtension = ext,
                    FileSize = file.Length,
                    Description = description,
                    UploadedDate = DateTime.Now,
                    IsActive = true
                };
                _context.PatientDocuments.Add(doc);

                savedFiles.Add(new
                {
                    fileName = doc.FileName,
                    filePath = doc.FilePath,
                    fileExtension = doc.FileExtension,
                    fileSize = doc.FileSize
                });
            }

            if (!savedFiles.Any())
                return Json(new { success = false, message = "No valid files were uploaded (unsupported file type)." });

            await _context.SaveChangesAsync();
            return Json(new { success = true, count = savedFiles.Count, files = savedFiles });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var doc = await _context.PatientDocuments.FindAsync(id);
            if (doc == null)
                return Json(new { success = false, message = "Document not found." });

            doc.IsActive = false; // soft delete — keeps file on disk
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments(int patientId)
        {
            var docs = await _context.PatientDocuments
                .Include(d => d.DocumentType)
                .Where(d => d.PatientId == patientId && d.IsActive)
                .OrderByDescending(d => d.UploadedDate)
                .Select(d => new
                {
                    id = d.Id,
                    documentTypeName = d.DocumentType.NameEn,
                    fileName = d.FileName,
                    filePath = d.FilePath,
                    fileExtension = d.FileExtension,
                    fileSize = d.FileSize,
                    description = d.Description,
                    uploadedDate = d.UploadedDate
                })
                .ToListAsync();

            return Json(new { success = true, data = docs });
        }
        #endregion

        private async Task<List<PreviousSessionRecordDto>> GetPreviousSessionsAsync(int patientId)
        {
            var entries = await _context.DentalChartEntries
                .Where(e => e.PatientId == patientId)
                .Include(e => e.Visit)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            var visitIds = entries.Select(e => e.VisitId).Distinct().ToList();

            var diagnoses = await _context.VisitDiagnoses
                .Where(d => visitIds.Contains(d.VisitId))
                .Include(d => d.Diagnosis)
                .ToListAsync();

            var statusLabels = new Dictionary<Dental.Models.Patient.ToothStatus, string>
    {
        { Dental.Models.Patient.ToothStatus.Healthy, "سليم" },
        { Dental.Models.Patient.ToothStatus.Decayed, "تسوس" },
        { Dental.Models.Patient.ToothStatus.Treated, "معالج" },
        { Dental.Models.Patient.ToothStatus.Missing, "مفقود" },
        { Dental.Models.Patient.ToothStatus.Extracted, "مخلوع" },
        { Dental.Models.Patient.ToothStatus.RootCanal, "عصب" },
        { Dental.Models.Patient.ToothStatus.Crown, "تاج" },
        { Dental.Models.Patient.ToothStatus.Filling, "حشوة" }
    };

            return entries.Select(e =>
            {
                var diag = diagnoses.FirstOrDefault(d => d.VisitId == e.VisitId && d.ToothNumber == e.ToothNumber);
                return new PreviousSessionRecordDto
                {
                    VisitDiagnosisId = diag?.Id ?? 0,                    // ✅
                    VisitId = (int)e.VisitId,                                 // ✅
                    DiagnosisCatalogId = diag?.DiagnosisCatalogId,       // ✅
                    ToothNumber = e.ToothNumber,
                    Date = e.Date,
                    StatusText = statusLabels.TryGetValue(e.Status, out var lbl) ? lbl : e.Status.ToString(),
                    DiagnosisName = diag?.Diagnosis?.NameAr,
                    DiagnosisNotes = diag?.Notes,
                    DiagnosisPrice = diag?.Diagnosis?.Price ?? 0,
                    VisitTotalAmount = e.Visit?.TotalAmount ?? 0,
                    VisitPaidAmount = e.Visit?.PaidAmount ?? 0,
                    VisitDiscount = e.Visit?.Discount ?? 0,
                    VisitRemaining = e.Visit?.Remaining ?? 0
                };
            }).ToList();
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
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            _logger.LogWarning("DEBUG: GenderId={GenderId}, PatientType={PatientType}", dto.GenderId, dto.PatientType);

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var validationErrors = ModelState
                        .Where(kv => kv.Value != null && kv.Value.Errors.Count > 0)
                        .ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                    return Json(new { success = false, errors = validationErrors });
                }

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

                if (!string.IsNullOrWhiteSpace(dto.Allergies)
                    || !string.IsNullOrWhiteSpace(dto.ChronicDiseases)
                    || !string.IsNullOrWhiteSpace(dto.CurrentMedications)
                    || !string.IsNullOrWhiteSpace(dto.MedicalNotes)
                    || !string.IsNullOrWhiteSpace(dto.EmergencyNotes)
                    || !string.IsNullOrWhiteSpace(dto.SmokingStatus)
                    || dto.IsPregnant == true)
                {
                    patient.MedicalInfo ??= new PatientMedicalInfo();
                    patient.MedicalInfo.Allergies = dto.Allergies;
                    patient.MedicalInfo.ChronicDiseases = dto.ChronicDiseases;
                    patient.MedicalInfo.CurrentMedications = dto.CurrentMedications;
                    patient.MedicalInfo.MedicalNotes = dto.MedicalNotes;
                    patient.MedicalInfo.SmokingStatus = dto.SmokingStatus;
                    patient.MedicalInfo.IsPregnant = dto.IsPregnant;
                    patient.MedicalInfo.EmergencyNotes = dto.EmergencyNotes;
                    patient.Notes = dto.MedicalNotes;
                }
                    patient.DateOfBirth = dto.DateOfBirth;
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
                        Models.Patient.PatientType.Both => VisitCategory.General,
                        _ => VisitCategory.General
                    }
                };

                await _context.Visits.AddAsync(visit);
                await _context.SaveChangesAsync();

                var successMessage = "تم إضافة المريض وفتح سيشن جديد";
                var redirectUrl = Url.Action("Session", "Clinical", new { patientId = patient.Id, visitId = visit.Id });

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        message = successMessage,
                        redirectUrl
                    });
                }

                // 3️⃣ التوجيه للسيشن مباشرة (fallback بدون JS)
                TempData[Helper.MsgType] = Helper.Success;
                TempData[Helper.Title] = "تم بنجاح";
                TempData[Helper.Msg] = successMessage;
                return Redirect(redirectUrl!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");

                if (isAjax)
                {
                    return Json(new { success = false, message = "حدث خطأ أثناء إضافة المريض" });
                }

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
                Notes = patient.MedicalNotes,
                PatientType = patient.PatientType,
                Allergies = patient.Allergies,
                ChronicDiseases = patient.ChronicDiseases,
                CurrentMedications = patient.CurrentMedications,
                MedicalNotes = patient.MedicalNotes,
                SmokingStatus = patient.SmokingStatus,
                EmergencyNotes = patient.MedicalNotes,

            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdatePatientDto dto)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var validationErrors = ModelState
                        .Where(kv => kv.Value != null && kv.Value.Errors.Count > 0)
                        .ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                    return Json(new { success = false, errors = validationErrors });
                }

                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }

            try
            {
                var result = await _patientService.UpdateAsync(dto);
                if (!result.Success)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.Message ?? "تعذر تعديل بيانات المريض" });
                    }

                    ModelState.AddModelError("", result.Message);
                    await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                    return View(dto);
                }

                // ✅ Medical History — حفظ صريح
                var patient = await _context.Patients
                    .Include(p => p.MedicalInfo)
                    .FirstOrDefaultAsync(p => p.Id == dto.Id);

                if (patient != null)
                {
                    patient.MedicalInfo ??= new PatientMedicalInfo();
                    patient.MedicalInfo.Allergies = dto.Allergies;
                    patient.MedicalInfo.ChronicDiseases = dto.ChronicDiseases;
                    patient.MedicalInfo.CurrentMedications = dto.CurrentMedications;
                    patient.MedicalInfo.MedicalNotes = dto.MedicalNotes;
                    patient.MedicalInfo.EmergencyNotes = dto.MedicalNotes;
                    patient.MedicalInfo.SmokingStatus = dto.SmokingStatus;
                    patient.MedicalInfo.IsPregnant = dto.IsPregnant;
                    patient.Notes = dto.MedicalNotes;
                    patient.DateOfBirth = dto.DateOfBirth;
                }

                    await _context.SaveChangesAsync();

                var successMessage = "تم تعديل بيانات المريض بنجاح";
                var redirectUrl = Url.Action("Details", "Patients", new { id = dto.Id });

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        message = successMessage,
                        redirectUrl
                    });
                }

                TempData[Helper.MsgType] = Helper.Success;
                TempData[Helper.Title] = "تم بنجاح";
                TempData[Helper.Msg] = successMessage;
                return Redirect(redirectUrl!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", dto.Id);

                if (isAjax)
                {
                    return Json(new { success = false, message = "حدث خطأ أثناء تعديل بيانات المريض" });
                }

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

        private async Task<List<XRayGroupDto>> GetXRaysGroupedAsync(int patientId)
        {
            var xrays = await _context.PatientXRays
                .Where(x => x.PatientId == patientId && x.ToothNumber != null)
                .OrderByDescending(x => x.TakenDate)
                .ToListAsync();

            return xrays
                .GroupBy(x => x.ToothNumber!.Value)
                .OrderBy(g => g.Key)
                .Select(g => new XRayGroupDto
                {
                    ToothNumber = g.Key,
                    Items = g.Select(x => new XRayItemDto
                    {
                        Id = x.Id,
                        FileName = x.FileName,
                        FilePath = x.FilePath,
                        Notes = x.Notes,
                        TakenDate = x.TakenDate
                    }).ToList()
                }).ToList();
        }


    }
}