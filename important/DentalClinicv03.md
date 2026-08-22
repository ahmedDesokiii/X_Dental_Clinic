```
// Constants / EnumHelper.cs
```

```
using System.Reflection;
namespace Dental.Constants
{
    public static class EnumHelper
    {
        // ========== Enums ==========
        public enum PatientType { General = 1, Orthodontics = 2, Both = 3 }
       // public enum VisitStatus { InProgress = 1, Completed = 2, Cancelled = 3
}
        //public enum ProcedureStatus { Planned = 1, InProgress = 2, Completed =
3 }
        public enum DiagnosisCategory { Caries = 1, Periodontal = 2, Endodontic
= 3, Prosthodontic = 4, Orthodontic = 5, Other = 6 }
        public static List<SelectListItem> GetEnumSelectList<T>()
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new SelectListItem
                {
                    Value = Convert.ToInt32(e).ToString(),
                    Text = GetDisplayName(e)
                }).ToList();
        }
        private static string GetDisplayName<T>(T enumValue)
        {
            var member = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First();
```

```
            var displayAttr = member.GetCustomAttribute<DisplayAttribute>();
            return displayAttr?.Name ?? enumValue.ToString();
        }
    }
}
```

```
// Constants / Helper.cs
```

```
namespace Dental.Constants
{
    public class Helper
    {
        public const string Success = "success";
        public const string Error = "error";
        public const string Warning = "warning";
        public const string Info = "info";
        public const string MsgType = "msgType";
        public const string Title = "title";
        public const string Msg = "msg";
        public const string AddRequestAsk = "New Request Ask";
        public const string Save = "Add New";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public enum eCurrentState
```

```
        {
            Active = 2, // نشط
            Suspended = 1, // معلق
            Delete = 0 // نشطغير
        }
    }
}
```

```
// Constants / Mdoules.cs
namespace Dental.Constants
{
    public enum Modules
    {
        Patients,
    }
}
```

```
// Constants / Permissions.cs
namespace Dental.Constants
{
    public class Permissions
    {
        public static List<string> GeneratePermissionsList(string module)
        {
            var permissionsList = GenerateAllPermissions();
            return permissionsList;
        }
        public static List<string> Generate_Students_PermissionsList(string
module)
        {
            return new List<string>()
            {
                $"Permissions.{module}.المرضيقائمة",
                $"Permissions.{module}.مريضاضافة",
                $"Permissions.{module}.مريضتعديل",
                $"Permissions.{module}.مريضحذف",
                $"Permissions.{module}.مريضتفاصيل",
            };
        }
        public static List<string> GenerateAllPermissions()
        {
            var allPermissions = new List<string>();
            var Patient_Permissions = new List<string>();
```

```
Patient_Permissions.AddRange(Generate_Students_PermissionsList(Modules.Patients.
ToString()));
```

```
            allPermissions = Patient_Permissions
                //.Concat(DW_CRM_Permissions)
                                           .ToList();
            return allPermissions;
        }
        #region Patients Modules
        public static class Patients
```

```
        {
```

```
            public const string View_Patients = "Permissions.Patients. قائمة
المرضي";
```

```
            public const string Add_Patient = "Permissions.Patients.مريضاضافة";
            public const string Edit_Patient = "Permissions.Patients. تعديل
مريض";
```

```
            public const string Delete_Patient = "Permissions.Patients. حذف
مريض";
            public const string Details_Patient = "Permissions.Patients. تفاصيل
مريض";
```

```
        }
        #endregion
    }
}
```

```
// Constants / Roles.cs
namespace Dental.Constants
{
    public enum Roles
    {
        Admin,  // النظاممدير
        User    // مستخدم
    }
}
//---------------------------------------
// Controllers / AppointmentsController.cs
// ============================================================
// 📁 File: Controllers/AppointmentsController.cs
// ============================================================
using Dental.Modules.Appointments.DTOs;
using Dental.Modules.Appointments.InterfacesAppointments;
using Dental.Modules.Patients.Enums;
using Microsoft.AspNetCore.Mvc;
namespace Dental.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;
        public AppointmentsController(
            IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }
        // ── الرئيسيةالكالندرصفحة ──
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
```

```
        // ── زمنيةفترةخاللبالأحداثالكالندرتغذية (Weekly/Monthly) ──
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            try
            {
                var events = await
_appointmentService.GetCalendarEventsAsync(start, end);
                return Json(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar events");
                return Json(new { success = false, message = " أثناءخطأحدث
المواعيدتحميل." });
            }
        }
        // ── اليوممواعيد (كـالستخدامها widget الداشبوردفي) ──
        [HttpGet]
        public async Task<IActionResult> GetToday()
        {
            var data = await _appointmentService.GetTodayAsync();
            return Json(new { success = true, data });
        }
        // ── (المريضتفاصيلصفحةفيت?ستخدم) معينمريضمواعيد ──
        [HttpGet]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var data = await
_appointmentService.GetUpcomingByPatientAsync(patientId);
            return Json(new { success = true, data });
        }
```

```
        // ── واحدموعدتفاصيل (الـلفتح Modal) ──
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _appointmentService.GetByIdAsync(id);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });
            return Json(new { success = true, data = result.Data });
        }
```

```
        // ── الحفظقبلالتعارضفحص (Live check الفورمتعبئةأثناء) ──
        [HttpPost]
        public async Task<IActionResult> CheckConflict([FromBody]
ConflictCheckRequestDto dto)
        {
            var hasConflict = await _appointmentService.HasConflictAsync(
                dto.AppointmentDate, dto.DurationMinutes, dto.ExcludeId);
```

```
            return Json(new { success = true, hasConflict });
        }
```

```
        // ── جديدموعدإنشاء ──
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto
dto)
        {
            if (!ModelState.IsValid)
```

```
                return Json(new { success = false, message = " المدخلةالبيانات
صحيحةغير." });
```

```
            var doctorName = User.Identity?.Name ?? "System";
            var result = await _appointmentService.CreateAsync(dto, doctorName);
```

```
            if (!result.Success)
```

```
                return Json(new { success = false, message = result.Message });
```

```
            return Json(new { success = true, message = result.Message,
appointmentId = result.Data });
```

```
        }
```

```
        // ── (المالحظات/السبب) الموعدبياناتتعديل ──
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateAppointmentDto
dto)
```

- `{` 

```
            var result = await _appointmentService.UpdateAsync(dto);
            return Json(new { success = result.Success, message = result.Message
});
```

- `}` 

```
        // ── آخروقت/لتاريخالموعدتأجيل ──
        [HttpPost]
        public async Task<IActionResult> Reschedule([FromBody]
RescheduleAppointmentDto dto)
```

- `{` 

```
            var result = await _appointmentService.RescheduleAsync(dto);
            return Json(new { success = result.Success, message = result.Message
});
```

- `}` 

```
        // ── الموعدتأكيد ──
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
```

```
            var result = await _appointmentService.ConfirmAsync(id);
            return Json(new { success = result.Success, message = result.Message
});
```

```
        }
```

```
        // ── المريضحضورتسجيل ──
        [HttpPost]
        public async Task<IActionResult> CheckIn(int id)
        {
```

```
            var result = await _appointmentService.CheckInAsync(id);
            return Json(new { success = result.Success, message = result.Message
});
        }
```

```
        // ── الموعدإلغاء ──
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var result = await _appointmentService.CancelAsync(id, reason);
            return Json(new { success = result.Success, message = result.Message
});
        }
```

```
        // ── (يحضرلم) كـالموعدتحديد ──
        [HttpPost]
        public async Task<IActionResult> MarkNoShow(int id)
        {
```

```
            var result = await _appointmentService.MarkNoShowAsync(id);
            return Json(new { success = result.Success, message = result.Message
});
        }
        // ── للكشفالمريضحضورعندفعليةزيارةإلىالموعدتحويل ──
        [HttpPost]
        public async Task<IActionResult> ConvertToVisit(int id, VisitCategory
category)
        {
            var result = await _appointmentService.ConvertToVisitAsync(id,
category);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });
            // يستخدمالفرونت visitId + patientId الـUعشانلصفحةالمستخدمهيوج
Session مباشرة
            return Json(new
            {
                success = true,
                message = result.Message,
                visitId = result.Data!.VisitId,
                patientId = result.Data.PatientId
            });
        }
    }
}
```

```
// Controllers / ClinicalController.cs
```

```
using AutoMapper;
using Dental.Models.Clinic;
using Dental.Modules.Clinical.DTOs;    // 📁 الـ DTOs الجديدة
using Dental.Modules.Patients.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;
using Dental.Models.Patient;
using Dental.Modules.Clinical.ViewModels;
```

```
namespace Dental.Controllers
{
    public class ClinicalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public ClinicalController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
```

```
        //[HttpGet]
        //public async Task<IActionResult> Calendar()
        //{
        //    return View();
        //}
        //[HttpGet]
        //public async Task<IActionResult> Payments()
```

```
        //{
        //    return View();
        //}
        [HttpGet]
        public async Task<IActionResult> Session(int patientId, int visitId)
        {
            var patient = await _context.Patients
                .Include(p => p.Gender)
                .Include(p => p.BloodGroup)
                .Include(p => p.MedicalInfo)
                .FirstOrDefaultAsync(p => p.Id == patientId);
            var visit = await _context.Visits
                .Include(v => v.Diagnoses).ThenInclude(d => d.Diagnosis)
                .Include(v => v.Procedures).ThenInclude(p => p.Procedure)
                .Include(v => v.Payments)
                .Include(v => v.TreatmentSessions)
                .Include(v => v.Procedures)
                .Include(v => v.Payments)
                .Include(v => v.Procedures)
                .Include(v => v.XRays)
                .Include(v => v.Prescriptions)
                .FirstOrDefaultAsync(v => v.Id == visitId);
            if (patient == null || visit == null) return NotFound();
            // Master Data للـ dropdowns
            ViewBag.DiagnosisList = await _context.DiagnosisCatalogs
                .Where(d => d.IsActive).ToListAsync();
            ViewBag.ProcedureList = await _context.ProcedureCatalogs
                .Where(p => p.IsActive).ToListAsync();
            ViewBag.ToothList = Enumerable.Range(11, 8)
                .Concat(Enumerable.Range(21, 8))
                .Concat(Enumerable.Range(31, 8))
                .Concat(Enumerable.Range(41, 8))
                .ToList();
            var model = new SessionViewModel
            {
                Patient = _mapper.Map<PatientDetailsDto>(patient),
                Visit = _mapper.Map<VisitDto>(visit),
                DentalHistory = await _context.DentalChartEntries
                    .Where(d => d.PatientId == patientId)
                    .OrderByDescending(d => d.Date)
                    .ToListAsync()
            };
```

```
            return View(model);
        }
```

```
        [HttpPost]
        public async Task<IActionResult> AddDiagnosis([FromBody]
AddDiagnosisRequest req)
        {
```

```
            };
            _context.VisitDiagnoses.Add(vd);
            await _context.SaveChangesAsync();
            return Json(new { success = true, data = vd });
        }
        [HttpPost]
        public async Task<IActionResult> AddProcedure([FromBody]
AddProcedureRequest req)
        {
            var proc = await
_context.ProcedureCatalogs.FindAsync(req.ProcedureId);
            if (proc == null) return Json(new { success = false, message =
"Procedure not found" });
            var vp = new VisitProcedure
            {
                VisitId = req.VisitId,
                ProcedureCatalogId = req.ProcedureId,
                ToothNumber = req.ToothNumber,
                Surface = req.Surface,
                Quantity = req.Quantity,
                UnitPrice = req.Price ?? proc.DefaultPrice,
                Status = ProcedureStatus.Planned,  // 📁 من Models.Patients
                Notes = req.Notes
            };
            _context.VisitProcedures.Add(vp);
            var visit = await _context.Visits.FindAsync(req.VisitId);
            if (visit != null) visit.TotalAmount += vp.TotalPrice;
            await _context.SaveChangesAsync();
            return Json(new { success = true, data = vp, remaining =
visit?.Remaining });
        }
```

`[HttpPost] public async Task<IActionResult> AddPayment([FromBody] AddPaymentRequest req) { var payment = new VisitPayment { VisitId = req.VisitId, Amount = req.Amount, Method = req.Method,               // 📁 من Models.Patients Description = req.Description, ReceiptNo = $"REC-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}" }; _context.VisitPayments.Add(payment); var visit = await _context.Visits.FindAsync(req.VisitId); if (visit != null) visit.PaidAmount += req.Amount; await _context.SaveChangesAsync(); return Json(new { success = true, data = payment, remaining = visit?.Remaining }); } [HttpPost] public async Task<IActionResult> SaveDentalChart([FromBody] SaveChartRequest req) { //  Validation` ⭐ 

`if (req == null || req.Teeth == null || !req.Teeth.Any()) return Json(new { success = false, message = " أسنان توجد ال محددة." }); if (req.PatientId <= 0) return Json(new { success = false, message = " غير المريض معرف صالح." }); var patientExists = await _context.Patients.AnyAsync(p => p.Id == req.PatientId && p.IsActive); if (!patientExists) return Json(new { success = false, message = "موجود غير المريض." }); foreach (var tooth in req.Teeth) { if (tooth.Number < 11 || tooth.Number > 48) return Json(new { success = false, message = $"السن رقم {tooth.Number} صالح غير." }); var entry = new DentalChartEntry { PatientId = req.PatientId, VisitId = req.VisitId, ToothNumber = tooth.Number, Status = tooth.Status, SurfacesJson = System.Text.Json.JsonSerializer.Serialize(tooth.Surfaces), ProcedureCode = tooth.ProcedureCode, Notes = tooth.Notes, Date = DateTime.Now }; _context.DentalChartEntries.Add(entry); } await _context.SaveChangesAsync(); return Json(new { success = true }); } //  NEW: Add Prescription Endpoint` ⭐ `[HttpPost] public async Task<IActionResult> AddPrescription([FromBody] AddPrescriptionRequest req) { if (req == null || req.Items == null || !req.Items.Any()) return Json(new { success = false, message = " الروشتة فارغة." }); if (req.VisitId <= 0) return Json(new { success = false, message = " غير الزيارة معرف صالح." }); var visit = await _context.Visits.FindAsync(req.VisitId); if (visit == null) return Json(new { success = false, message = " غير الزيارة موجودة." }); var prescription = new Prescription { VisitId = req.VisitId, PrescriptionDate = DateTime.Now, Diagnosis = req.Diagnosis, Notes = req.Notes, Status = PrescriptionStatus.Active, PrescribedBy = req.PrescribedBy ?? "System"` 

```
            };
            foreach (var item in req.Items)
            {
                if (string.IsNullOrWhiteSpace(item.MedicationName))
                    continue;
                prescription.Items.Add(new PrescriptionItem
                {
                    MedicationName = item.MedicationName,
                    Dosage = item.Dosage,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Instructions = item.Instructions
                });
            }
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            return Json(new { success = true, data = prescription.Id });
        }
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody]
CreateAppointmentRequest request)
        {
            try
            {
                var patient = await
_context.Patients.FindAsync(request.PatientId);
                if (patient == null)
                    return Json(new { success = false, message = "Patient not
found" });
                var appointment = new Appointment
                {
                    PatientId = request.PatientId,
                    AppointmentDate = request.AppointmentDate,
                    Duration = TimeSpan.FromMinutes(request.Duration),
                    Reason = request.Reason,
                    Status = AppointmentStatus.Scheduled,
                    DoctorName = User.Identity?.Name ?? "System"
                };
                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, data = appointment });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating appointment");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
// Controllers / HomeController.cs
using Microsoft.AspNetCore.Mvc;
```

```
using Dental.Models;
using System.Diagnostics;
namespace Dental.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region Dashboard
        public IActionResult Index()
        {
            return View();
        }
        #endregion
        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None,
NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ??
HttpContext.TraceIdentifier });
        }
    }
}
```

```
// Controllers / PatientsController.cs
```

```
// ============================================================
// 📁 File: Controllers/PatientsController.cs (FINAL)
// ============================================================
```

```
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
```

```
namespace Dental.Controllers
{
    public class PatientsController : Controller
    {
```

```
        private readonly IPatientService _patientService;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PatientsController> _logger;
        private readonly IMapper _mapper;
```

```
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
        #region 📁 Index (Server-Side DataTables)
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetPatientsStats()
        {
            var total = await _context.Patients.CountAsync(p => p.IsActive);
            var general = await _context.Patients.CountAsync(p =>
                p.IsActive && p.Visits.Any(v => v.Category ==
VisitCategory.General));
            var ortho = await _context.Patients.CountAsync(p =>
                p.IsActive && p.Visits.Any(v => v.Category ==
VisitCategory.Orthodontics));
            var both = await _context.Patients.CountAsync(p =>
                p.IsActive
                && p.Visits.Any(v => v.Category == VisitCategory.General)
                && p.Visits.Any(v => v.Category == VisitCategory.Orthodontics));
            return Json(new { total, general, ortho, both });
        }
        private static List<ToothSurfaceDto> ParseSurfaces(string? surfacesJson)
        {
            if (string.IsNullOrWhiteSpace(surfacesJson))
                return new List<ToothSurfaceDto>();
            try
            {
                var raw =
System.Text.Json.JsonSerializer.Deserialize<List<ToothSurfaceRequest>>(surfacesJ
son) ?? new();
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
```

```
                        _ => ToothSurface.Occlusal
                    },
                    Condition = Enum.TryParse<SurfaceCondition>(s.Condition,
true, out var cond)
                        ? cond
                        : SurfaceCondition.Healthy,
                    Notes = string.Empty
                }).ToList();
            }
            catch
            {
                // كلهاالصفحةتوقفمنسيبهاش — تالفة/قديمةبيانات
                return new List<ToothSurfaceDto>();
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetPatientsData([FromBody]
DataTableRequest request)
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
                    query = query.Where(p => p.Visits.Any(v => v.Category ==
VisitCategory.General));
                }
                else if (categoryFilter == "Orthodontics")
                {
                    query = query.Where(p => p.Visits.Any(v => v.Category ==
VisitCategory.Orthodontics));
                }
                if (!string.IsNullOrWhiteSpace(request.Search?.Value))
                {
                    var search = request.Search.Value.Trim().ToLower();
                    query = query.Where(p =>
                        (p.FullNameAr != null && p.FullNameAr.Contains(search))
||
                        (p.PatientCode != null &&
p.PatientCode.Contains(search)) ||
```

```
                        (p.Phone != null && p.Phone.Contains(search)) ||
                        (p.Mobile != null && p.Mobile.Contains(search)) ||
                        (p.NationalId != null && p.NationalId.Contains(search))
                    );
                }
```

```
                var recordsTotal = await _context.Patients.CountAsync();
                var recordsFiltered = await query.CountAsync();
                if (request.Order.Any() && request.Columns.Any())
                {
                    var order = request.Order.First();
                    var columnName = request.Columns[order.Column].Data;
                    var isAsc = order.Dir == "asc";
```

```
                    query = columnName switch
                    {
                        "patientCode" => isAsc ? query.OrderBy(p =>
p.PatientCode) : query.OrderByDescending(p => p.PatientCode),
                        "fullNameAr" => isAsc ? query.OrderBy(p => p.FullNameAr)
: query.OrderByDescending(p => p.FullNameAr),
                        "phone" => isAsc ? query.OrderBy(p => p.Phone) :
query.OrderByDescending(p => p.Phone),
                        "age" => isAsc ? query.OrderBy(p => p.DateOfBirth) :
query.OrderByDescending(p => p.DateOfBirth),
                        "genderName" => isAsc
                            ? query.OrderBy(p => p.Gender != null ?
p.Gender.NameAr : "")
                            : query.OrderByDescending(p => p.Gender != null ?
p.Gender.NameAr : ""),
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
                        Age = p.DateOfBirth.HasValue ? DateTime.Now.Year -
p.DateOfBirth.Value.Year : null,
                        GenderName = p.Gender != null ? p.Gender.NameAr : null,
                        IsActive = p.IsActive,
                        HasGeneralVisits = p.Visits.Any(v => v.Category ==
VisitCategory.General),
                        HasOrthodonticsVisits = p.Visits.Any(v => v.Category ==
VisitCategory.Orthodontics)
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
                _logger.LogError(ex, "Error fetching patients data for
DataTables");
                return Json(new DataTableResponse<PatientListDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<PatientListDto>()
                });
            }
        }
```

```
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
            // ── الطبيالتاريخ (Sessions) — الفعليةالزياراتمن ──
            var sessions = visits.Select(v => new SessionDto
            {
                Id = v.Id,
                PatientId = v.PatientId,
                SessionDate = v.VisitDate,
                ProcedureName = v.Procedures.Any()
                    ? string.Join(" ،", v.Procedures.Select(p =>
p.Procedure.NameAr))
                    : (v.ChiefComplaint ?? "عامةزيارة"),
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
            // ── Dental Chart — العالجاتكل + حالةUمجمآخرسن،لكلع ──
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
                            var latest = g.OrderByDescending(e =>
e.Date).First();
                            return new ToothDto
                            {
                                ToothNumber = g.Key,
```

```
                                ToothName = $"سن {g.Key}",
                                Status = latest.Status,
                                Notes = latest.Notes ?? string.Empty,
                                Surfaces = ParseSurfaces(latest.SurfacesJson),
                                Treatments = g.OrderByDescending(e =>
e.Date).Select(e => new ToothTreatmentDto
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
```

```
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
```

```
                TotalDue = visits.Sum(v => v.Remaining),
                DentalChart = dentalChart,
                Sessions = sessions,
                XRays = xrays,
                Prescriptions = new List<PrescriptionDto>(),   // الحًق[انكملها
                Photos = new List<PhotoDto>(),                 // الحًق[انكملها
                Payments = new List<PaymentDto>(),              // الحًق[انكملها
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
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId);
                return View(dto);
            }
```

`try { //` 1️⃣� `المريض إنشاء` 1️⃣� `var patient = _mapper.Map<Patient>(dto); patient.PatientCode = await _patientService.GeneratePatientCodeAsync(); patient.IsActive = true; patient.CreatedBy = 1; // Medical Info الـ في موجود لو DTO if (!string.IsNullOrWhiteSpace(dto.Allergies) || !string.IsNullOrWhiteSpace(dto.ChronicDiseases) || !string.IsNullOrWhiteSpace(dto.CurrentMedications)) { patient.MedicalInfo = new PatientMedicalInfo { Allergies = dto.Allergies, ChronicDiseases = dto.ChronicDiseases, CurrentMedications = dto.CurrentMedications, MedicalNotes = dto.MedicalNotes, SmokingStatus = dto.SmokingStatus, IsPregnant = dto.IsPregnant, EmergencyNotes = dto.EmergencyNotes }; } await _context.Patients.AddAsync(patient); await _context.SaveChangesAsync(); //` 2️⃣� `إنشاء Visit الحفظ بعد تلًقائي var visit = new Visit { PatientId = patient.Id,` 

`VisitDate = DateTime.Now, Status = VisitStatus.InProgress, FollowUpType = FollowUpType.Normal, Category = VisitCategory.General // من نحدده أو PatientType }; await _context.Visits.AddAsync(visit); await _context.SaveChangesAsync(); TempData[Helper.MsgType] = Helper.Success; TempData[Helper.Title] = "بنجاح تم"; TempData[Helper.Msg] = "جديد سيشن وفتح المريض إضافة تم"; //` 3️⃣� `مباشرة للسيشن التوجيه` 3️⃣� `return RedirectToAction("Session", "Clinical", new { patientId = patient.Id, visitId = visit.Id }); } catch (Exception ex) { _logger.LogError(ex, "Error creating patient"); ModelState.AddModelError("", "المريض إضافة أثناء خطأ حدث"); await LoadSelectListsAsync(dto.GenderId, dto.BloodGroupId); return View(dto); } } #endregion #region Edit [HttpGet] public async Task<IActionResult> Edit(int id) { var result = await _patientService.GetByIdAsync(id); if (!result.Success || result.Data == null) return NotFound(); var patient = result.Data; await LoadSelectListsAsync(patient.Gender?.Id, patient.BloodGroup?.Id);` 

```
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
```

```
        public async Task<IActionResult> Edit(UpdatePatientDto dto)
        {
```

```
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
                TempData[Helper.Title] = "بنجاحتم";
                TempData[Helper.Msg] = "بنجاحالمريضبياناتتعديلتم";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}",
dto.Id);
                ModelState.AddModelError("", " بياناتتعديلأثناءخطأحدث
المريض");
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
                return Json(new { success = false, message = " أثناءخطأحدث
الحذف" });
            }
        }
        #endregion
```

```
        #region Search (AJAX)
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());
```

```
            var patients = await _context.Patients
                .AsNoTracking()
                .Where(p =>
```

```
                    (p.FullNameAr != null && p.FullNameAr.Contains(term)) ||
```

```
                    (p.PatientCode != null && p.PatientCode.Contains(term)) ||
```

```
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
```

```
            return Json(patients);
        }
        #endregion
```

```
        #region  Helper Methods⚡
        private async Task LoadSelectListsAsync(int? selectedGenderId = null,
int? selectedBloodGroupId = null)
        {
```

```
            var genders = await _cache.GetOrCreateAsync("genders_select", async
entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                return await _context.Genders
                    .AsNoTracking()
                    .Where(g => g.IsActive)
                    .ToListAsync();
            });
```

```
            var bloodGroups = await
_cache.GetOrCreateAsync("blood_groups_select", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                return await _context.BloodGroups
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .ToListAsync();
            });
```

```
            ViewBag.Genders = new SelectList(genders, "Id", "NameAr",
selectedGenderId);
            ViewBag.BloodGroups = new SelectList(bloodGroups, "Id", "Name",
selectedBloodGroupId);
        }
        #endregion
```

```
        [HttpGet]
        public async Task<IActionResult> NewSession(int patientId,
VisitCategory? category = null)
        {
```

```
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();
```

```
            var resolvedCategory = category ?? (patient.PatientType ==
Dental.Models.Patient.PatientType.Orthodontics
                ? VisitCategory.Orthodontics
                : VisitCategory.General);
```

```
            var visit = new Visit
            {
                PatientId = patientId,
                VisitDate = DateTime.Now,
                Status = VisitStatus.InProgress,
```

```
                FollowUpType = FollowUpType.Normal,
                Category = resolvedCategory
            };
            await _context.Visits.AddAsync(visit);
            await _context.SaveChangesAsync();
```

```
            return RedirectToAction("Session", "Clinical", new { patientId =
patient.Id, visitId = visit.Id });
        }
```

```
    }
}
```

```
// Controllers / PaymentsController.cs
```

```
// ============================================================
// 📁 File: Controllers/PaymentsController.cs
// ============================================================
using Dental.Modules.Payments.DTOs;
using Dental.Modules.Payments.InterfacesPayments;
using Microsoft.AspNetCore.Mvc;
```

```
namespace Dental.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;
```

```
        public PaymentsController(IPaymentService paymentService,
ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }
        // ── حسابهكشفعلىللدخولمريضعنالبحثصفحة ──
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        // ── نUكشفمعيلمريضالشاملالحساب ──
        [HttpGet]
        public async Task<IActionResult> Statement(int patientId)
        {
            ViewBag.PatientId = patientId;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetStatement(int patientId)
        {
            var result = await
_paymentService.GetPatientStatementAsync(patientId);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });
            return Json(new { success = true, data = result.Data });
        }
```

```
        // ── جديدةدفعةتسجيل (الـ/الزيارةصفحةومنالكشف،صفحةمنت?ستخدم
Session) ──
```

```
        [HttpPost]
        public async Task<IActionResult> AddPayment([FromBody] AddPaymentDto
dto)
        {
            var result = await _paymentService.AddPaymentAsync(dto);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });
            // "remaining" الـصفحةعشانالًقديملسمبنفسمتسابة Session الحالية
فيهاتعديلأيغيرمنشغالة
            return Json(new
            {
                success = true,
                message = result.Message,
                receiptNo = result.Data!.ReceiptNo,
                paidAmount = result.Data.VisitPaidAmount,
                remaining = result.Data.VisitRemaining
            });
        }
        // ── (تلًقائي[االزيارةرصيدبيصحح) دفعةإلغاء ──
        [HttpPost]
        public async Task<IActionResult> VoidPayment(int paymentId, string?
reason)
        {
            var result = await _paymentService.VoidPaymentAsync(paymentId,
reason);
            return Json(new { success = result.Success, message = result.Message
});
        }
    }
}
```

```
// Controllers / RolesController.cs
using Dental.Models.User;
using Dental.ViewModels.User;
namespace Dental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }
        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
        #region List Of Roles [View]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.OrderBy(x =>
x.Name).ToListAsync();
            return View(roles);
        }
```

```
        #endregion
        #region Add|Edit Roles [Create & Update]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> _SaveRole(RoleFormViewModel model)
        {
            if (model.Details == null)
                model.Details = "";
            if (model.RoleId == null)
            { //Create
                if (await _roleManager.RoleExistsAsync(model.Name))
                {
                    SessionMsg(Helper.Error, " مسجلةالمجموعة" ,"! مكررةمجموعة
قبلمن !");
                    return View("Index", await _roleManager.Roles.OrderBy(x =>
x.Name).ToListAsync());
                }
                else
                {
                    var role = new ApplicationRole()
                    {
                        Name = model.Name,
                        Details = model.Details
                    };
                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)// Succeeded
                        SessionMsg(Helper.Success, " اضافةتم" ,"! الإضافةتم
بنجاحالمستخدممجموعة!");
                    else // Not Successeded
                        SessionMsg(Helper.Error, " خطأحدث" ,"الإضافةفيخطأ
المجموعةاضافةاثناء!");
                }
            }
            else
            { //Update
                var roleUpdate = await _roleManager.FindByIdAsync(model.RoleId);
                roleUpdate.Name = model.Name;
                roleUpdate.Details = model.Details;
                var Result = await _roleManager.UpdateAsync(roleUpdate);
                if (Result.Succeeded) // Succeeded
                    SessionMsg(Helper.Success, " مجموعةتعديلتم" ,"التعديلتم
بنجاحالمستخدم !");
                else  // Not Successeded
                    SessionMsg(Helper.Error, " اثناءخطأحدث" ,"التعديلفيخطأ
المجموعةتعديل !");
            }
            return RedirectToAction(nameof(Index), await
_roleManager.Roles.OrderBy(x => x.Name).ToListAsync());
        }
        #endregion
        #region Delete Role [Delete]
        public async Task<IActionResult> DeleteRole(string Id)
        {
```

```
            var role = _roleManager.Roles.FirstOrDefault(x => x.Id.ToString() ==
Id);
```

- `return NotFound();` 

```
            if ((await _roleManager.DeleteAsync(role)).Succeeded)
                return RedirectToAction(nameof(Index));
```

```
            return RedirectToAction("Index");
```

```
        #region Manage Permissions
        public async Task<IActionResult> ManagePermissions(string roleId)
```

```
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = await _roleManager.FindByIdAsync(roleId);
```

```
                return NotFound();
```

```
            var roleClaims = _roleManager.GetClaimsAsync(role).Result.Select(c
=> c.Value).ToList();
            var allClaims = Permissions.GenerateAllPermissions();
            var allPermissions = allClaims.Select(p => new CheckBoxViewModel
{ DisplayValue = p }).ToList();
```

```
            foreach (var permission in allPermissions)
                    permission.IsSelected = true;
            var permissionVM = new PermissionsFormViewModel()
                RoleId = roleId,
                RoleName = role.Name,
                Details = role.Details,
                RoleCalims = allPermissions
```

```
            return View(permissionVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
ManagePermissions(PermissionsFormViewModel model)
        {
```

```
            var role = await _roleManager.FindByIdAsync(model.RoleId);
```

```
                return NotFound();
```

```
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in roleClaims)
                await _roleManager.RemoveClaimAsync(role, claim);
```

```
            var selectedClaims = model.RoleCalims.Where(c =>
c.IsSelected).ToList();
```

```
            foreach (var claim in selectedClaims)
                await _roleManager.AddClaimAsync(role, new Claim("Permission",
claim.DisplayValue));
```

```
            return RedirectToAction(nameof(Index));
        }
        #endregion
```

```
    }
}
```

```
// Controllers / UsersController.cs
```

```
// ============================================================
// 📁 File: Controllers/UsersController.cs (FINAL - WORKING)
// ============================================================
using Dental.Models.DataTables;
using Dental.Modules.Users.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
```

```
namespace Dental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
            _cache = cache;
        }
        #region Index (Legacy - N+1 FIXED)
        public async Task<IActionResult> Index()
        {
            // Step 1: Fetch all users (single query)
            var users = await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CurrentState)
                .ToListAsync();
```

```
            // Step 2: Fetch ALL user-role mappings in ONE query
            var userIds = users.Select(u => u.Id).ToList();
```

```
            var userRoleMappings = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();
            // Step 3: Fetch ALL roles in ONE query
            var roleIds = userRoleMappings.Select(ur =>
ur.RoleId).Distinct().ToList();
```

```
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();
            // Step 4: Build lookup dictionaries in memory (no more DB queries!)
            var roleNamesById = roles.ToDictionary(r => r.Id, r => r.Name ??
"");
```

```
            var rolesByUserId = userRoleMappings
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ur => roleNamesById.ContainsKey(ur.RoleId)
                        ? roleNamesById[ur.RoleId]
                        : "").Where(name => !
string.IsNullOrEmpty(name)).ToList()
                );
```

```
            // Step 5: Build ViewModels
            var userVM = users.Select(u => new UserViewModel
            {
                Id = u.Id.ToString(),
                Code = u.Code,
                FullName_Ar = u.FullName_Ar,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                CurrentState = u.CurrentState,
                Roles = rolesByUserId.ContainsKey(u.Id) ? rolesByUserId[u.Id] :
new List<string>()
            }).ToList();
```

```
            return View(userVM);
```

```
        #region 📁 DataTables Server-Side Processing
        [HttpPost]
        public async Task<IActionResult> GetUsersData([FromBody]
DataTableRequest request)
        {
            // Step 1: Build base query
            var query = _context.Users.AsNoTracking().AsQueryable();
```

```
            // Step 2: Global Search
            if (!string.IsNullOrWhiteSpace(request.Search?.Value))
            {
                var search = request.Search.Value.Trim().ToLower();
                query = query.Where(u =>
                    (u.Code != null && u.Code.ToLower().Contains(search)) ||
                    (u.FullName_Ar != null && u.FullName_Ar.Contains(search)) ||
                    (u.UserName != null &&
u.UserName.ToLower().Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
```

```
                );
            }
```

```
            var recordsTotal = await _context.Users.CountAsync();
            var recordsFiltered = await query.CountAsync();
```

```
            // Step 3: Sorting
            if (request.Order.Any() && request.Columns.Any())
            {
                var order = request.Order.First();
                var columnName = request.Columns[order.Column].Data;
                var isAsc = order.Dir == "asc";
                query = columnName switch
                {
                    "code" => isAsc ? query.OrderBy(u => u.Code) :
query.OrderByDescending(u => u.Code),
                    "fullNameAr" => isAsc ? query.OrderBy(u => u.FullName_Ar) :
query.OrderByDescending(u => u.FullName_Ar),
                    "userName" => isAsc ? query.OrderBy(u => u.UserName) :
query.OrderByDescending(u => u.UserName),
                    "phoneNumber" => isAsc ? query.OrderBy(u => u.PhoneNumber) :
query.OrderByDescending(u => u.PhoneNumber),
                    "currentState" => isAsc ? query.OrderBy(u => u.CurrentState)
: query.OrderByDescending(u => u.CurrentState),
                    _ => query.OrderByDescending(u => u.CurrentState)
                };
            }
            else
            {
                query = query.OrderByDescending(u => u.CurrentState);
            }
```

```
            // Step 4: Get paged users
            var pagedUsers = await query
                .Skip(request.Start)
                .Take(request.Length)
                .ToListAsync();
```

```
            // Step 5: Get roles for paged users ONLY (3 queries total, not N+1)
            var pagedUserIds = pagedUsers.Select(u => u.Id).ToList();
```

```
            var pagedUserRoleMappings = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => pagedUserIds.Contains(ur.UserId))
                .ToListAsync();
```

```
            var pagedRoleIds = pagedUserRoleMappings.Select(ur =>
ur.RoleId).Distinct().ToList();
```

```
            var pagedRoles = await _context.Roles
                .AsNoTracking()
                .Where(r => pagedRoleIds.Contains(r.Id))
                .ToListAsync();
            // Step 6: Build lookups
            var pagedRoleNamesById = pagedRoles.ToDictionary(r => r.Id, r =>
r.Name ?? "");
```

```
            var pagedRolesByUserId = pagedUserRoleMappings
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ur =>
```

```
pagedRoleNamesById.ContainsKey(ur.RoleId)
                        ? pagedRoleNamesById[ur.RoleId]
                        : "").Where(name => !
string.IsNullOrEmpty(name)).ToList()
                );
            // Step 7: Build DTOs
            var data = pagedUsers.Select(u => new UserListDto
            {
                Id = u.Id,
                Code = u.Code,
                FullName_Ar = u.FullName_Ar,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                CurrentState = (int)u.CurrentState,
                Roles = pagedRolesByUserId.ContainsKey(u.Id) ?
pagedRolesByUserId[u.Id] : new List<string>()
            }).ToList();
            return Json(new DataTableResponse<UserListDto>
            {
                Draw = request.Draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = data
            });
        }
        #endregion
        #region User Process [Add & Update]
        [HttpGet]
        public async Task<IActionResult> AddUser(string userId)
        {
            var selectConn =
HttpContext.Session.GetString("SelectedConnection");
            var connectionStrings =
_configuration.GetSection("ConnectionStrings")
                                     .GetChildren()
                                     .Select(cs => cs.Key)
                                     .ToList();
```

```
            var roles = await _cache.GetOrCreateAsync("all_roles", async entry
=>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _roleManager.Roles
                    .AsNoTracking()
                    .Select(r => new CheckBoxViewModel
                    {
                        RoleId = r.Id.ToString(),
                        DisplayValue = r.Name
                    })
                    .ToListAsync();
            });
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addUserVM = new AddUserViewModel
            {
                Roles = roles ?? new List<CheckBoxViewModel>(),
                ConnectionNames = connectionStrings,
                SelectedConnection = selectConn
            };
```

```
            return View(addUserVM);
```

```
        }
```

```
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AddUserViewModel model)
        {
```

```
            if (!ModelState.IsValid)
                return View(model);
```

```
            if (!model.Roles.Any(r => r.IsSelected))
            {
                ModelState.AddModelError("Roles", " مجموعةالأقلعلىحدد،[فضال
واحدة");
                return View(model);
            }
            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError("UserName", " موجودالمستخدماسم
[مسبًقا");
                return View(model);
            }
            var user = new ApplicationUser()
            {
                Code = model.Code,
                Gender = model.Gender,
                PassUser = model.PassUser,
                EmailConfirmed = true,
                CurrentState = 2,
                FullName_Ar = model.FullName_Ar,
                PhoneNumber = model.PhoneNumber,
                UserName = model.UserName,
                Email = model.Email,
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }
            await _userManager.AddToRolesAsync(user,
                model.Roles.Where(r => r.IsSelected).Select(r =>
r.DisplayValue));
```

```
            _cache.Remove("all_roles");
```

```
            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "بنجاحتم";
            TempData[Helper.Msg] = "بنجاحالمستخدمإضافةتم";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
```

```
            var profileFormVM = new ProfileFormViewModel
            {
                Id = userId,
                FullName_Ar = user.FullName_Ar,
                FullName_En = user.FullName_En,
                IdentityNum = user.IdentityNum,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                PassUser = user.PassUser,
                Email = user.Email
            };
            return View(profileFormVM);
        }
```

```
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(ProfileFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();
            user.FullName_Ar = model.FullName_Ar;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;
            await _userManager.UpdateAsync(user);
            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "بنجاحتم";
            TempData[Helper.Msg] = "بنجاحالمستخدمبياناتتعديلتم";
```

```
            return RedirectToAction(nameof(Index));
        }
        #endregion
        #region Roles Process [Manage Roles]
        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var allRoles = await
_roleManager.Roles.AsNoTracking().ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRolesVM = new UserRolesViewModel
            {
                UserId = user.Id.ToString(),
                UserName = user.UserName,
                Roles = allRoles.Select(role => new CheckBoxViewModel
                {
                    RoleId = role.Id.ToString(),
                    DisplayValue = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                }).ToList()
            };
            return View(userRolesVM);
        }
```

```
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in model.Roles)
            {
                if (userRoles.Contains(role.DisplayValue) && !role.IsSelected)
                    await _userManager.RemoveFromRoleAsync(user,
role.DisplayValue);
```

```
                if (!userRoles.Contains(role.DisplayValue) && role.IsSelected)
                    await _userManager.AddToRoleAsync(user, role.DisplayValue);
            }
```

```
            var updatedRoles = await _userManager.GetRolesAsync(user);
            user.CurrentState = updatedRoles.Count == 0 ? 1 : 2;
            user.EmailConfirmed = updatedRoles.Count > 0;
            await _userManager.UpdateAsync(user);
            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "بنجاحتم";
            TempData[Helper.Msg] = "المستخدمصالحياتتحديثتم";
            return RedirectToAction(nameof(Index));
        }
        #endregion
        #region Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int Id)
        {
            var user = await _userManager.FindByIdAsync(Id.ToString());
            if (user == null)
            {
                return Json(new { success = false, message = " غيرالمستخدم
موجود" });
            }
            user.CurrentState = 0;
            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);
            return Json(new { success = true, message = " المستخدمتعطيلتم
بنجاح" });
        }
        #endregion
        [HttpGet]
        public IActionResult GetUserName(int userId)
        {
            var name = _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullName_Ar)
                .FirstOrDefault();
```

```
            return Json(new { userName = name });
```

```
        }
    }
}
```

```
//---------------------------------------------------------------
```

```
// Data / ApplicationDBContext
```

```
using Dental.Models;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Models.User;
using Dental.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
```

```
namespace Dental.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser,
ApplicationRole, int>
    {
```

```
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>
options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
```

```
            base.OnModelCreating(builder);
```

```
builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            #region EF - Users | Roles
```

```
            builder.Entity<ApplicationUser>().ToTable("ERP_WebUsers");
            builder.Entity<ApplicationRole>().ToTable("ERP_Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("ERP_UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("ERP_UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("ERP_UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("ERP_RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("ERP_UserToken");
            #endregion
```

```
            builder.Entity<Visit>()
        .Property(v => v.TotalAmount)
        .HasPrecision(18, 2);
```

```
            builder.Entity<Visit>()
                .Property(v => v.Discount)
                .HasPrecision(18, 2);
```

```
            builder.Entity<Visit>()
                .Property(v => v.PaidAmount)
                .HasPrecision(18, 2);
```

```
            builder.Entity<VisitProcedure>()
                .Property(v => v.UnitPrice)
                .HasPrecision(18, 2);
```

```
            builder.Entity<VisitPayment>()
                .Property(v => v.Amount)
```

```
                .HasPrecision(18, 2);
```

```
            builder.Entity<ProcedureCatalog>()
                .Property(p => p.DefaultPrice)
                .HasPrecision(18, 2);
            builder.Entity<TreatmentPlanItem>()
                .Property(t => t.EstimatedCost)
                .HasPrecision(18, 2);
            builder.Entity<TreatmentPlanItem>()
                .Property(t => t.Discount)
                .HasPrecision(18, 2);
        builder.Entity<PatientDocument>()
           .HasOne(d => d.DocumentType)
           .WithMany()
           .HasForeignKey(d => d.DocumentTypeId)
           .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<DocumentType>().HasData(
              new DocumentType { Id = 1, NameAr = "الًقوميالرقمبطاقة", IsActive
= true, CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 2, NameAr = "أشعةتًقرير", IsActive = true,
CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 3, NameAr = "طبيتأمين", IsActive = true,
CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 4, NameAr = "عالجموافًقة", IsActive = true,
CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 5, NameAr = "أخرى", IsActive = true,
CreatedDate = new DateTime(2026, 1, 1) }
  );
```

```
        }
```

```
        protected override void OnConfiguring(DbContextOptionsBuilder
optionsBuilder)
        {
```

```
            base.OnConfiguring(optionsBuilder);
        }
```

```
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Gender> Genders { get; set; }
        public DbSet<BloodGroup> BloodGroups { get; set; }
        public DbSet<PatientContact> PatientContacts { get; set; }
        public DbSet<PatientDocument> PatientDocuments { get; set; }
        public DbSet<PatientMedicalInfo> PatientMedicalInfos { get; set; }
        public DbSet<PatientTag> PatientTags { get; set; }
        public DbSet<PatientXRay> PatientXRays { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TreatmentCatalog> TreatmentCatalogs { get; set; }
        public DbSet<Visit> Visits { get; set; }
```

`//  NEW` ⭐ `public DbSet<Appointment> Appointments { get; set; } public DbSet<Prescription> Prescriptions { get; set; } public DbSet<PrescriptionItem> PrescriptionItems { get; set; } public DbSet<DiagnosisCatalog> DiagnosisCatalogs { get; set; } public DbSet<ProcedureCatalog> ProcedureCatalogs { get; set; } public DbSet<VisitDiagnosis> VisitDiagnoses { get; set; } public DbSet<VisitProcedure> VisitProcedures { get; set; } public DbSet<VisitPayment> VisitPayments { get; set; }` 

```
        public DbSet<DentalChartEntry> DentalChartEntries { get; set; }
        public DbSet<TreatmentPlan> TreatmentPlans { get; set; }
        public DbSet<TreatmentPlanItem> TreatmentPlanItems { get; set; }
        public DbSet<TreatmentSession> TreatmentSessions { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
    }
}
// -----------------------------------------------------------------------
```

```
// Models / Clinic / Appointment.cs
```

```
    // Models/Patient/Appointment.cs
    using global::Dental.Models.Common;
    using global::Dental.Models.Patient;
    namespace Dental.Models.Patient
    {
        public class Appointment : BaseEntity
        {
            public int PatientId { get; set; }
            public Patient Patient { get; set; } = null!;
            public DateTime AppointmentDate { get; set; }
            public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);
            public string? Reason { get; set; }               // الزيارةسبب
            public string? Notes { get; set; }
            public AppointmentStatus Status { get; set; } =
AppointmentStatus.Scheduled;
```

```
            // If converted to Visit
            public int? VisitId { get; set; }
            public Visit? Visit { get; set; }
            // Doctor/Clinic
            public int? DoctorId { get; set; }
            public string? DoctorName { get; set; }
            public int? ClinicId { get; set; }
            // Reminder
            public bool IsReminderSent { get; set; } = false;
            public DateTime? ReminderDate { get; set; }
        }
    }
```

```
// Models / Clinic / DentalChartEntry.cs
```

```
using Dental.Models.Common;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Models.Patient
{
```

```
    public enum ToothStatus
    {
        Healthy = 1, Decayed = 2, Treated = 3, Missing = 4,
        Extracted = 5, Impacted = 6, Crown = 7, Bridge = 8,
        Implant = 9, Fractured = 10, RootCanal = 11, Filling = 12
    }
```

```
    public class DentalChartEntry : BaseEntity
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int? VisitId { get; set; }
        public Visit? Visit { get; set; }
        public int ToothNumber { get; set; }              // FDI: 11-18, 21-
28...
        public ToothStatus Status { get; set; } = ToothStatus.Healthy;
        // JSON: [{"surface":"M","condition":"Caries"},
{"surface":"O","condition":"Filling"}]
        public string? SurfacesJson { get; set; }
        public string? ProcedureCode { get; set; }        // CRN, RCT...
        public string? Notes { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
```

```
// Models / Clinic / DiagnosisCatalog.cs
using Dental.Models.Common;
using static Dental.Constants.EnumHelper;
```

```
namespace Dental.Models.Clinic
{
    // ========== Master Data: Diagnosis Catalog ==========
    public class DiagnosisCatalog : BaseEntity
    {
        public string Code { get; set; } = null!; // مثال: D01
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public DiagnosisCategory Category { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
```

```
// Models / Clinic / Prescription.cs
```

```
using Dental.Models.Common;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Models.Clinic
{
    public class Prescription : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public DateTime PrescriptionDate { get; set; } = DateTime.Now;
        public string? Diagnosis { get; set; }
        public string? Notes { get; set; }
        public PrescriptionStatus Status { get; set; } =
PrescriptionStatus.Active;
```

```
        public string? PrescribedBy { get; set; }         // Doctor Name
        public int? DoctorId { get; set; }
```

```
        public ICollection<PrescriptionItem> Items { get; set; } = new
List<PrescriptionItem>();
    }
    public class PrescriptionItem : BaseEntity
    {
        public int PrescriptionId { get; set; }
        public Prescription Prescription { get; set; } = null!;
        public string MedicationName { get; set; } = null!;
        public string? Dosage { get; set; }              // 500mg
        public string? Frequency { get; set; }             // 3 times daily
        public string? Duration { get; set; }              // 7 days
        public string? Instructions { get; set; }          // After meals
        public int? Quantity { get; set; }
    }
}
```

```
// Models / Clinic / ProcedureCatalog.cs
```

```
using Dental.Models.Common;
```

```
namespace Dental.Models.Clinic
{
    // ========== Master Data: Procedure Catalog ==========
    public class ProcedureCatalog : BaseEntity
    {
        public string Code { get; set; } = null!; // مثال: RCT, CRN
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public decimal DefaultPrice { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
```

```
// Models / Clinic / TreatmentPlan.cs
```

```
using Dental.Models.Clinic;
using Dental.Models.Common;
using Dental.Models.Patient;
```

```
namespace Dental.Models.Patient
{
    public enum PlanStatus { Draft = 1, Active = 2, Completed = 3, Cancelled = 4
}
```

```
    public class TreatmentPlan : BaseEntity
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public string? PlanCode { get; set; }
        public DateTime PlanDate { get; set; } = DateTime.Now;
        public PlanStatus Status { get; set; } = PlanStatus.Draft;
        public string? Notes { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public ICollection<TreatmentPlanItem> Items { get; set; } = new
List<TreatmentPlanItem>();
        public ICollection<TreatmentSession> Sessions { get; set; } = new
```

```
List<TreatmentSession>();
    }
    public class TreatmentPlanItem : BaseEntity
    {
        public int TreatmentPlanId { get; set; }
        public TreatmentPlan TreatmentPlan { get; set; } = null!;
        public int? ProcedureCatalogId { get; set; }
        public ProcedureCatalog? Procedure { get; set; }
        public int? ToothNumber { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal? Discount { get; set; }
        public decimal FinalCost => EstimatedCost - (Discount ?? 0);
        public ProcedureStatus Status { get; set; } = ProcedureStatus.Planned;
        public int? Priority { get; set; }                // 1 = Highest
        public string? Notes { get; set; }
    }
    public class TreatmentSession : BaseEntity
    {
        public int TreatmentPlanId { get; set; }
        public TreatmentPlan TreatmentPlan { get; set; } = null!;
        public int? VisitId { get; set; }
        public Visit? Visit { get; set; }
        public DateTime SessionDate { get; set; }
        public string? SessionName { get; set; }          // "Session 1: Access"
        public string? Notes { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
```

```
}
```

```
// Models / Clinic / VisitDiagnosis.cs
```

```
using Dental.Models.Common;
using Dental.Models.Patient;
```

```
namespace Dental.Models.Clinic
{
    public class VisitDiagnosis : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public int DiagnosisCatalogId { get; set; }
        public DiagnosisCatalog Diagnosis { get; set; } = null!;
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; } // M, D, B, L, O
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; } = false;
    }
}
```

```
// Models / Clinic / VisitPayment.cs
```

```
using Dental.Models.Common;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Models.Clinic
{
    // ========== VisitPayment (بالإجراءمرتبطة) ==========
    public class VisitPayment : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; } // "جزئيدفع — Crown 16 + Root
Canal"
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? ReceiptNo { get; set; }
    }
}
```

```
// Models / Clinic / VisitProcedure.cs
using Dental.Models.Common;
using Dental.Models.Patient;
using static Dental.Constants.EnumHelper;
namespace Dental.Models.Clinic
{
    // ========== VisitProcedure (الذهب) ==========
    public class VisitProcedure : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public int ProcedureCatalogId { get; set; }
        public ProcedureCatalog Procedure { get; set; } = null!;
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public ProcedureStatus Status { get; set; } = ProcedureStatus.Planned;
        public string? Notes { get; set; }
    }
}
```

```
// Models / Common / BaseEntity.cs
namespace Dental.Models.Common
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
// Models / DataTables / DataTableModels.cs
```

```
// ============================================================
// 📁 File: Models/DataTables/DataTableModels.cs
// ============================================================
```

```
namespace Dental.Models.DataTables
{
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; } = 10;
        public List<DataTableColumn> Columns { get; set; } = new();
        public List<DataTableOrder> Order { get; set; } = new();
        public DataTableSearch Search { get; set; } = new();
        public string? CategoryFilter { get; set; }
    }
    public class DataTableColumn
    {
        public string Data { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Searchable { get; set; } = true;
        public bool Orderable { get; set; } = true;
        public DataTableSearch Search { get; set; } = new();
    }
    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }
    public class DataTableSearch
    {
        public string Value { get; set; } = "";
        public bool Regex { get; set; }
    }
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
// Models / Patient / BloodGroup.cs
using Dental.Models.Common;
namespace Dental.Models.Patient
{
    public class BloodGroup : BaseEntity
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
```

```
    }
}
```

```
// Models / Patient / DocumentTyp.cs
using Dental.Models.Common;
namespace Dental.Models.Patient
{
    public class DocumentType : BaseEntity
    {
        public string NameAr { get; set; } = null!;
        public string? NameEn { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
// Models / Patient / Gender.cs
using Dental.Models.Common;
namespace Dental.Models.Patient
{
    public class Gender : BaseEntity
    {
        public string Code { get; set; } = null!;
```

```
        public string NameAr { get; set; } = null!;
```

```
        public string? NameEn { get; set; }
```

```
        public bool IsActive { get; set; } = true;
    }
}
```

```
// Models / Patient / Patient.cs
```

```
using Dental.Models.Common;
using System.ComponentModel.DataAnnotations.Schema;
using static Dental.Constants.EnumHelper;
```

```
namespace Dental.Models.Patient
{
    public enum PatientType { General = 1, Orthodontics = 2, Both = 3 }
    public enum ProcedureStatus { Planned = 1, InProgress = 2, Completed = 3 }
    public enum DiagnosisCategory { Caries = 1, Periodontal = 2, Endodontic = 3,
Prosthodontic = 4, Orthodontic = 5, Other = 6 }
    public enum AppointmentStatus { Scheduled = 1, Confirmed = 2, CheckedIn = 3,
InProgress = 4, Completed = 5, Cancelled = 6, NoShow = 7 }
    public class Patient : BaseEntity
    {
        public string PatientCode { get; set; } = null!;
        public string FullNameAr { get; set; } = null!;
        public string? FullNameEn { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public string Mobile { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
```

```
        public string? Occupation { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
```

```
        public PatientType PatientType { get; set; } = PatientType.General;
        public string? FileNo { get; set; }
```

```
        // Navigation
        public int? GenderId { get; set; }
        public virtual Gender? Gender { get; set; }
        public int? BloodGroupId { get; set; }
        public virtual BloodGroup? BloodGroup { get; set; }
```

```
        public virtual ICollection<PatientContact> Contacts { get; set; } = new
HashSet<PatientContact>();
        public virtual PatientMedicalInfo? MedicalInfo { get; set; }
        public virtual ICollection<PatientDocument> Documents { get; set; } =
new HashSet<PatientDocument>();
        public virtual ICollection<PatientTag> PatientTags { get; set; } = new
HashSet<PatientTag>();
```

```
        // Clinical & Financial
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<Appointment> Appointments { get; set; } = new
List<Appointment>();
        public ICollection<PatientXRay> XRays { get; set; } = new
List<PatientXRay>();
        public ICollection<DentalChartEntry> DentalHistory { get; set; } = new
List<DentalChartEntry>();
        public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new
List<TreatmentPlan>();
```

```
    }
}
```

- `// Models / Patient / PatientContact.cs` 

```
    using Dental.Models.Common;
```

```
    namespace Dental.Models.Patient
```

```
        public class PatientContact : BaseEntity
            public int PatientId { get; set; }
```

```
            public int ContactTypeId { get; set; }
```

```
            public string? Name { get; set; }
```

```
            public string Phone { get; set; } = null!;
```

```
            public string? Email { get; set; }
```

```
            public string? Address { get; set; }
```

```
            public bool IsPrimary { get; set; } = false;
```

```
            public bool IsEmergency { get; set; } = false;
```

```
            // Navigation
```

```
            public virtual Patient Patient { get; set; } = null!;
        }
    }
```

```
// Models / Patient / PatientDocument.cs
```

```
using Dental.Models.Common;
```

```
namespace Dental.Models.Patient
{
```

```
    public class PatientDocument : BaseEntity
    {
```

```
        public int PatientId { get; set; }
```

```
        public int DocumentTypeId { get; set; }
```

```
        public string FileName { get; set; } = null!;
```

```
        public string FilePath { get; set; } = null!;
```

```
        public string? FileExtension { get; set; }
```

```
        public long? FileSize { get; set; }
```

```
        public string? Description { get; set; }
```

```
        public DateTime UploadedDate { get; set; }
            = DateTime.Now;
```

```
        public bool IsActive { get; set; } = true;
```

```
        // Navigation
```

```
        public virtual Patient Patient { get; set; } = null!;
        public virtual DocumentType DocumentType { get; set; } = null!;
    }
```

```
}
```

```
// Models / Patient / PatientMedicalInfo.cs
```

```
using Dental.Models.Common;
```

```
namespace Dental.Models.Patient
{
    public class PatientMedicalInfo : BaseEntity
    {
```

```
        public int PatientId { get; set; }
```

```
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
```

```
        public string? PreviousSurgeries { get; set; }
```

```
        public string? CurrentMedications { get; set; }
```

```
        public string? MedicalNotes { get; set; }
```

```
        public string? SmokingStatus { get; set; }
```

```
        public bool IsPregnant { get; set; }
```

```
        public string? EmergencyNotes { get; set; }
```

```
        // Navigation
```

```
        public virtual Patient Patient { get; set; } = null!;
    }
}
```

```
// Models / Patient / PatientTag.cs
```

```
using Dental.Models.Common;
```

```
namespace Dental.Models.Patient
{
    public class PatientTag : BaseEntity
    {
        public int PatientId { get; set; }
```

```
        public int TagId { get; set; }
```

```
        // Navigation
```

```
        public virtual Patient Patient { get; set; } = null!;
```

```
        public virtual Tag Tag { get; set; } = null!;
    }
}
```

```
// Models / Patient / PatientXRay.cs
```

```
using Dental.Models.Common;
```

```
namespace Dental.Models.Patient
    {
```

```
        public class PatientXRay : BaseEntity
    {
```

```
            public int PatientId { get; set; }
            public Patient Patient { get; set; } = null!;
            public int? VisitId { get; set; }
            public Visit? Visit { get; set; }
            public string FileName { get; set; } = null!;
            public string FilePath { get; set; } = null!; // path on disk/blob,
NOT binary
            public DateTime TakenDate { get; set; }
            public string? Notes { get; set; }
        }
    }
```

```
// Models / Patient / Tag.cs
```

```
using Dental.Models.Common;
namespace Dental.Models.Patient
{
    public class Tag : BaseEntity
    {
        public string Code { get; set; } = null!;
```

```
        public string NameAr { get; set; } = null!;
        public string? NameEn { get; set; }
        public string? Color { get; set; }
```

```
        public bool IsActive { get; set; } = true;
```

```
        public virtual ICollection<PatientTag> PatientTags { get; set; }
            = new HashSet<PatientTag>();
    }
}
```

```
// Models / Patient / TreatmentCatalog.cs
```

```
using Dental.Models.Common;
using Dental.Modules.Patients.Enums;
```

```
namespace Dental.Models.Patient
{
    public class TreatmentCatalog : BaseEntity
    {
        public string Name { get; set; } = null!;
        public decimal DefaultCost { get; set; }
        public VisitCategory? Category { get; set; } // null = shared between
both
        public bool IsActive { get; set; } = true;
    }
}
```

```
// Models / Patient / Visit.cs
```

```
using System.ComponentModel.DataAnnotations.Schema;
using Dental.Models.Common;
using Dental.Models.Clinic;
using Dental.Modules.Patients.Enums;
```

```
namespace Dental.Models.Patient
{
    public class Visit : BaseEntity
    {
```

```
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
```

```
        public DateTime VisitDate { get; set; }
        public DateTime? EndDate { get; set; }
```

```
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? Notes { get; set; }
```

```
        public FollowUpType FollowUpType { get; set; } = FollowUpType.Normal;
        public DateTime? FollowUpDate { get; set; }
```

```
        public VisitStatus Status { get; set; } = VisitStatus.InProgress;
        public VisitCategory Category { get; set; } = VisitCategory.General;
```

```
        public bool IsActive { get; set; } = true;
```

```
        // Financial
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
```

```
        [NotMapped]
        public decimal Remaining => TotalAmount - Discount - PaidAmount;
```

```
        // Navigation
        public ICollection<VisitDiagnosis> Diagnoses { get; set; } = new
List<VisitDiagnosis>();
        public ICollection<VisitProcedure> Procedures { get; set; } = new
List<VisitProcedure>();
        public ICollection<VisitPayment> Payments { get; set; } = new
List<VisitPayment>();
        public ICollection<PatientXRay> XRays { get; set; } = new
List<PatientXRay>();
        public ICollection<Prescription> Prescriptions { get; set; } = new
List<Prescription>();
        public ICollection<TreatmentSession> TreatmentSessions { get; set; } =
new List<TreatmentSession>();
        public ICollection<Appointment> Appointments { get; set; } = new
List<Appointment>();
    }
}
```

```
// Models / Users / ApplicationRole.cs
namespace Dental.Models.User
{
    public class ApplicationRole : IdentityRole<int>
    {
```

```
        public ApplicationRole(string roleName) : base(roleName)
        {
```

```
        }
        public ApplicationRole() { }
        public string Details { get; set; } = string.Empty;
        // 1 : معلق & 0 : نشطغير &  2 : نشط
        // public int? CurrentState { get; set; } = 0;
    }
}
```

```
// Models / Users / ApplicationUser.cs
```

```
namespace Dental.Models.User
{
    public class ApplicationUser : IdentityUser<int>
    {
```

```
        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(200), MinLength(5)]
        public string FullName_Ar { get; set; } = string.Empty;
        [MaxLength(200), MinLength(5)]
        public string FullName_En { get; set; } = string.Empty;
        [Required, MaxLength(20), MinLength(8)]
        public string IdentityNum { get; set; } = string.Empty;
        [MaxLength(10)]
        public string Gender { get; set;} = string.Empty;
```

```
        //password show
        public string PassUser { get; set; } = string.Empty;
        // 1 : معلق & 0 : نشطغير &  2 : نشط
        public int? CurrentState { get; set; } = 0;
```

```
    }
}
```

```
////////////////////// Modules -------------------------
```

```
Modules/Appointments/DTOs/ AppointmentDtos.cs
// ============================================================
// 📁 File: Modules/Appointments/DTOs/AppointmentDtos.cs
// ============================================================
using Dental.Models.Patient;
using Dental.Modules.Patients.Enums;
```

```
namespace Dental.Modules.Appointments.DTOs
{
```

```
    // ── الكالندرلتغذيةي?ستخدم (Weekly/Monthly view) ──
    public class AppointmentCalendarDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientMobile { get; set; } = null!;
        public PatientType PatientType { get; set; }
```

```
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
```

```
        public int DurationMinutes { get; set; }
        public AppointmentStatus Status { get; set; }
        public string StatusText { get; set; } = null!;
        public string StatusColorClass { get; set; } = null!; // الـفيلالستخدام
badge/border
```

```
        public string? Reason { get; set; }
    }
    // ── واحدموعدتفاصيل (للـ Modal / التعديل) ──
    public class AppointmentDetailsDto : AppointmentCalendarDto
    {
        public string? Notes { get; set; }
        public string? DoctorName { get; set; }
        public int? VisitId { get; set; }
        public bool IsReminderSent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
```

```
    // ── (معينمريضمواعيد / اليوممواعيد) مبسطةقائمة ──
    public class AppointmentListDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientMobile { get; set; } = null!;
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; }
        public AppointmentStatus Status { get; set; }
        public string StatusText { get; set; } = null!;
        public string? Reason { get; set; }
    }
```

```
    // ── جديدموعدإنشاء ──
    public class CreateAppointmentDto
    {
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }
    // ── (نفسهالميعادتغييربدون) الموعدبياناتتعديل ──
    public class UpdateAppointmentDto
    {
        public int Id { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }
```

```
    // ── تانيةمدةأولتاريخالموعدتحريك/تأجيل ──
    public class RescheduleAppointmentDto
    {
        public int Id { get; set; }
        public DateTime NewAppointmentDate { get; set; }
        public int DurationMinutes { get; set; } = 30;
    }
    // ── (الحفظقبلالفورمفيالكتابةأثناءي?ستخدم) التعارضفحص ──
    public class ConflictCheckRequestDto
    {
```

```
        public DateTime AppointmentDate { get; set; }
```

```
        public int DurationMinutes { get; set; } = 30;
        public int? ExcludeId { get; set; }
    }
```

```
    // ──  للصفحةUالفرونتهنوجعشانلتنينمحتاجين) زيارةإلىالموعدتحويلنتيجة
(الصح ──
    public class ConvertToVisitResultDto
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
    }
}
```

```
// Modules / Appointments / InterfacesAppointments / IAppointmentService.cs
// ============================================================
```

```
// 📁 File: Modules/Appointments/InterfacesAppointments/IAppointmentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Appointments.DTOs;
using Dental.Modules.Patients.Enums;
```

```
namespace Dental.Modules.Appointments.InterfacesAppointments
{
```

```
    public interface IAppointmentService
    {
```

```
        // ── الًقراءة ──
        Task<List<AppointmentCalendarDto>> GetCalendarEventsAsync(DateTime from,
DateTime to);
        Task<List<AppointmentListDto>> GetTodayAsync();
```

```
        Task<List<AppointmentListDto>> GetUpcomingByPatientAsync(int patientId,
int take = 10);
```

```
        Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int id);
```

```
        // ── التعارضفحص ──
        Task<bool> HasConflictAsync(DateTime appointmentDate, int
durationMinutes, int? excludeId = null);
```

```
        // ── الكتابة ──
```

```
        Task<ServiceResult<int>> CreateAsync(CreateAppointmentDto dto, string
createdByName);
```

```
        Task<ServiceResult<bool>> UpdateAsync(UpdateAppointmentDto dto);
        Task<ServiceResult<bool>> RescheduleAsync(RescheduleAppointmentDto dto);
```

```
        // ── الحالةتغيير (Status Flow) ──
        Task<ServiceResult<bool>> ConfirmAsync(int id);
        Task<ServiceResult<bool>> CheckInAsync(int id);
        Task<ServiceResult<bool>> CancelAsync(int id, string? reason);
        Task<ServiceResult<bool>> MarkNoShowAsync(int id);
```

```
        // ── المريضحضورعندفعليةلزيارةالموعدتحويل ──
        Task<ServiceResult<ConvertToVisitResultDto>> ConvertToVisitAsync(int
appointmentId, VisitCategory category);
    }
```

```
}
```

```
// Modules / Appointments / ServicesAppointments / AppointmentService.cs
```

```
// ============================================================
```

```
// 📁 File: Modules/Appointments/ServicesAppointments/AppointmentService.cs
// ============================================================
using Dental.Core.Results;
```

```
using Dental.Models.Patient;
using Dental.Modules.Appointments.DTOs;
using Dental.Modules.Appointments.InterfacesAppointments;
using Dental.Modules.Patients.Enums;
using Microsoft.EntityFrameworkCore;
```

```
namespace Dental.Modules.Appointments.ServicesAppointments
{
```

```
    // الـ :مالحظة Mapping طريقعنومشيدويهنا AutoMapper ،بUمركمنطقفيهلأن
    // (تحويل Duration من TimeSpan حسابلدقائق، End من Start+Duration ،نصوص
بالعربيالحالة)
    // لـمناسبمش Profile نضيفحاببلو .عادي AutoMapper Profile  لتللحبسيط
نضيفهسهلالحًق[ا،الأبسط.
    public class AppointmentService : IAppointmentService
    {
```

```
        private readonly ApplicationDbContext _context;
```

```
        // التعارضفحصفيوت?حسبالكالندرفيمكانبتشغلالليلتالح
        // (والـالملغي NoShow فعلي[افاضيبًقىمكانهملأنتعارض،ميعملوش)
        private static readonly AppointmentStatus[] _blockingStatuses =
        {
```

```
            AppointmentStatus.Scheduled,
            AppointmentStatus.Confirmed,
            AppointmentStatus.CheckedIn,
            AppointmentStatus.InProgress,
            AppointmentStatus.Completed
        };
```

```
        public AppointmentService(ApplicationDbContext context)
        {
```

```
            _context = context;
        }
```

```
        // ────────────────────────────────────────────────────────
        #region Helpers
```

```
        private static string GetStatusText(AppointmentStatus status) => status
switch
```

```
        {
```

```
            AppointmentStatus.Scheduled => "محجوز",
            AppointmentStatus.Confirmed => "مؤكد",
            AppointmentStatus.CheckedIn => "حضر",
            AppointmentStatus.InProgress => "الكشفجاري",
            AppointmentStatus.Completed => "مكتمل",
            AppointmentStatus.Cancelled => "ملغي",
            AppointmentStatus.NoShow => "يحضرلم",
            _ => status.ToString()
        };
```

```
        private static string GetStatusColorClass(AppointmentStatus status) =>
status switch
        {
```

```
            AppointmentStatus.Scheduled => "status-scheduled",
            AppointmentStatus.Confirmed => "status-confirmed",
            AppointmentStatus.CheckedIn => "status-checkedin",
            AppointmentStatus.InProgress => "status-inprogress",
            AppointmentStatus.Completed => "status-completed",
            AppointmentStatus.Cancelled => "status-cancelled",
            AppointmentStatus.NoShow => "status-noshow",
            _ => "status-scheduled"
        };
```

```
        private AppointmentCalendarDto MapToCalendarDto(Appointment a)
        {
```

```
            var end = a.AppointmentDate.Add(a.Duration);
            return new AppointmentCalendarDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullNameAr,
                PatientMobile = a.Patient.Mobile,
                PatientType = a.Patient.PatientType,
                Start = a.AppointmentDate,
                End = end,
                DurationMinutes = (int)a.Duration.TotalMinutes,
                Status = a.Status,
                StatusText = GetStatusText(a.Status),
                StatusColorClass = GetStatusColorClass(a.Status),
                Reason = a.Reason
            };
        }
        #endregion
```

```
        // ────────────────────────────────────────────────────────
        #region Read
```

```
        public async Task<List<AppointmentCalendarDto>>
GetCalendarEventsAsync(DateTime from, DateTime to)
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.AppointmentDate >= from && a.AppointmentDate < to)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();
```

```
            return appointments.Select(MapToCalendarDto).ToList();
        }
```

```
        public async Task<List<AppointmentListDto>> GetTodayAsync()
        {
```

```
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate <
tomorrow)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentListDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullNameAr,
                    PatientMobile = a.Patient.Mobile,
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = (int)a.Duration.TotalMinutes,
                    Status = a.Status,
                    StatusText = GetStatusText(a.Status),
                    Reason = a.Reason
                })
                .ToListAsync();
        }
```

```
        public async Task<List<AppointmentListDto>>
GetUpcomingByPatientAsync(int patientId, int take = 10)
```

```
        {
            var now = DateTime.Now;
```

```
            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.PatientId == patientId && a.AppointmentDate >= now
                            && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .Take(take)
                .Select(a => new AppointmentListDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullNameAr,
                    PatientMobile = a.Patient.Mobile,
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = (int)a.Duration.TotalMinutes,
                    Status = a.Status,
                    StatusText = GetStatusText(a.Status),
                    Reason = a.Reason
                })
                .ToListAsync();
        }
```

```
        public async Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int
id)
        {
```

```
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
                return ServiceResult<AppointmentDetailsDto>.Fail(" غيرالموعد
موجود.");
            var calendarDto = MapToCalendarDto(appointment);
            var dto = new AppointmentDetailsDto
            {
                Id = calendarDto.Id,
                PatientId = calendarDto.PatientId,
                PatientName = calendarDto.PatientName,
                PatientMobile = calendarDto.PatientMobile,
                PatientType = calendarDto.PatientType,
                Start = calendarDto.Start,
                End = calendarDto.End,
                DurationMinutes = calendarDto.DurationMinutes,
                Status = calendarDto.Status,
                StatusText = calendarDto.StatusText,
                StatusColorClass = calendarDto.StatusColorClass,
                Reason = calendarDto.Reason,
                Notes = appointment.Notes,
                DoctorName = appointment.DoctorName,
                VisitId = appointment.VisitId,
                IsReminderSent = appointment.IsReminderSent,
                CreatedAt = appointment.CreatedDate
            };
```

```
            return ServiceResult<AppointmentDetailsDto>.Ok(dto);
        }
```

```
        #endregion
```

```
        // ────────────────────────────────────────────────────────
        #region Conflict Check
```

```
        public async Task<bool> HasConflictAsync(DateTime appointmentDate, int
durationMinutes, int? excludeId = null)
```

```
        {
```

```
            var newStart = appointmentDate;
            var newEnd = appointmentDate.AddMinutes(durationMinutes);
```

```
            // SQL Server (وEF Core عموم[ا) يترجمقادرمش DateTime.Add(TimeSpan)
الـلما TimeSpan ديناميكيعمود
            // (Duration)  نفس) للترجمةقابلبنطاقنفلتر :الحل .ثابتةقيمةمش
رخيصاستعالموهو (،الحالة + اليوم
            // بالـالدقيًقةالمًقارنةنعملوبعدينالواحد،اليوممواعيدلعددجد[ا
TimeSpan الميموريفي.
            var dayStart = appointmentDate.Date;
            var dayEnd = dayStart.AddDays(1);
            var query = _context.Appointments
                .Where(a => _blockingStatuses.Contains(a.Status)
                            && a.AppointmentDate >= dayStart &&
a.AppointmentDate < dayEnd);
```

```
            if (excludeId.HasValue)
                query = query.Where(a => a.Id != excludeId.Value);
```

```
            var sameDayAppointments = await query
                .Select(a => new { a.AppointmentDate, a.Duration })
                .ToListAsync();
```

```
            //  ونهايةالًقديم،نهايةقبلالجديدالموعدبداية :كالسيكيزمنيتعارض
الًقديمبدايةبعدالجديد
            return sameDayAppointments.Any(a =>
                newStart < a.AppointmentDate.Add(a.Duration) &&
                a.AppointmentDate < newEnd);
        }
```

```
        #endregion
```

```
        // ────────────────────────────────────────────────────────
        #region Write
```

```
        public async Task<ServiceResult<int>> CreateAsync(CreateAppointmentDto
dto, string createdByName)
```

```
        {
            var patientExists = await _context.Patients.AnyAsync(p => p.Id ==
dto.PatientId && p.IsActive);
```

```
            if (!patientExists)
```

```
                return ServiceResult<int>.Fail("نشطغيرأوموجودغيرالمريض.");
            if (dto.AppointmentDate < DateTime.Now.AddMinutes(-5))
                return ServiceResult<int>.Fail("ماضoوقتفيموعدحجزيمكنال.");
```

```
            if (await HasConflictAsync(dto.AppointmentDate,
dto.DurationMinutes))
```

```
                return ServiceResult<int>.Fail(" هذامعمتعارضآخرموعديوجد
التوقيت.");
```

```
            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                AppointmentDate = dto.AppointmentDate,
                Duration = TimeSpan.FromMinutes(dto.DurationMinutes),
                Reason = dto.Reason,
```

```
                Notes = dto.Notes,
                Status = AppointmentStatus.Scheduled,
                DoctorName = createdByName
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
```

```
            return ServiceResult<int>.Ok(appointment.Id, " الموعدحجزتم
بنجاح.");
        }
```

```
        public async Task<ServiceResult<bool>> UpdateAsync(UpdateAppointmentDto
dto)
        {
            var appointment = await _context.Appointments.FindAsync(dto.Id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("موجودغيرالموعد.");
```

```
            if (appointment.Status is AppointmentStatus.Cancelled or
AppointmentStatus.Completed)
                return ServiceResult<bool>.Fail(" أوملغيموعدتعديليمكنال
مكتمل.");
```

```
            appointment.Reason = dto.Reason;
            appointment.Notes = dto.Notes;
```

```
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "الموعدبياناتتحديثتم.");
        }
```

```
        public async Task<ServiceResult<bool>>
RescheduleAsync(RescheduleAppointmentDto dto)
        {
```

```
            var appointment = await _context.Appointments.FindAsync(dto.Id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("موجودغيرالموعد.");
```

```
            if (appointment.Status is AppointmentStatus.Cancelled or
AppointmentStatus.Completed)
```

```
                return ServiceResult<bool>.Fail(" أوملغيموعدتأجيليمكنال
مكتمل.");
```

```
            if (dto.NewAppointmentDate < DateTime.Now.AddMinutes(-5))
                return ServiceResult<bool>.Fail("ماضoلوقتالموعدنًقليمكنال.");
```

```
            if (await HasConflictAsync(dto.NewAppointmentDate,
dto.DurationMinutes, dto.Id))
                return ServiceResult<bool>.Fail("الميعادمعمتعارضآخرموعديوجد
الجديد.");
```

```
            appointment.AppointmentDate = dto.NewAppointmentDate;
            appointment.Duration = TimeSpan.FromMinutes(dto.DurationMinutes);
```

```
            // رUأياتغيالميعادلأنتلًقائي[ابيتلغيسابقتأكيد
            if (appointment.Status == AppointmentStatus.Confirmed)
                appointment.Status = AppointmentStatus.Scheduled;
```

```
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "بنجاحالموعدتأجيلتم.");
        }
```

```
        #endregion
```

```
        // ────────────────────────────────────────────────────────
        #region Status Flow
```

```
        public async Task<ServiceResult<bool>> ConfirmAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("موجودغيرالموعد.");
            if (appointment.Status != AppointmentStatus.Scheduled)
                return ServiceResult<bool>.Fail(" حالتهفيالموعدتأكيديمكنال
الحالية.");
            appointment.Status = AppointmentStatus.Confirmed;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "الموعدتأكيدتم.");
        }
        public async Task<ServiceResult<bool>> CheckInAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("موجودغيرالموعد.");
            if (appointment.Status is not (AppointmentStatus.Scheduled or
AppointmentStatus.Confirmed))
                return ServiceResult<bool>.Fail(" فيالمريضحضورتسجيليمكنال
الحاليةالموعدحالة.");
```

```
            appointment.Status = AppointmentStatus.CheckedIn;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "المريضحضورتسجيلتم.");
        }
```

```
        public async Task<ServiceResult<bool>> CancelAsync(int id, string?
reason)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("موجودغيرالموعد.");
            if (appointment.Status is AppointmentStatus.Completed or
AppointmentStatus.Cancelled)
                return ServiceResult<bool>.Fail("ملغيأومكتملموعدإلغاءيمكنال
بالفعل.");
```

```
            appointment.Status = AppointmentStatus.Cancelled;
            if (!string.IsNullOrWhiteSpace(reason))
                appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                    ? $"الإلغاءسبب: {reason}"
                    : $"{appointment.Notes}\nالإلغاءسبب: {reason}";
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "الموعدإلغاءتم.");
        }
        public async Task<ServiceResult<bool>> MarkNoShowAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult<bool>.Fail("موجودغيرالموعد.");
```

```
            if (appointment.Status is not (AppointmentStatus.Scheduled or
AppointmentStatus.Confirmed))
```

```
                return ServiceResult<bool>.Fail(" لم) كـالموعدتحديديمكنال
الحاليةحالتهفي (يحضر.");
            if (appointment.AppointmentDate > DateTime.Now)
                return ServiceResult<bool>.Fail(" وقتهيحنلمموعدتحديديمكنال
(يحضرلم) كـبعد.");
            appointment.Status = AppointmentStatus.NoShow;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, " لم) كـالموعدتحديدتم
(يحضر.");
        }
        #endregion
```

```
        // ────────────────────────────────────────────────────────
        #region Convert To Visit
```

```
        public async Task<ServiceResult<ConvertToVisitResultDto>>
ConvertToVisitAsync(int appointmentId, VisitCategory category)
        {
            var appointment = await
_context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
                return ServiceResult<ConvertToVisitResultDto>.Fail(" غيرالموعد
موجود.");
```

```
            if (appointment.VisitId.HasValue)
                return ServiceResult<ConvertToVisitResultDto>.Fail(" إنشاءتم
بالفعلالموعدلهذازيارة.");
            if (appointment.Status is AppointmentStatus.Cancelled or
AppointmentStatus.NoShow)
                return ServiceResult<ConvertToVisitResultDto>.Fail("تحويليمكنال
زيارةإلى (يحضرلم) أوملغيموعد.");
            var visit = new Visit
            {
                PatientId = appointment.PatientId,
                VisitDate = DateTime.Now,
                Category = category,
                ChiefComplaint = appointment.Reason,
                IsActive = true
            };
            _context.Visits.Add(visit);
            await _context.SaveChangesAsync(); // الـناخدعشان Visit.Id
            appointment.VisitId = visit.Id;
            appointment.Status = AppointmentStatus.Completed;
            await _context.SaveChangesAsync();
```

```
            var resultDto = new ConvertToVisitResultDto { VisitId = visit.Id,
PatientId = appointment.PatientId };
            return ServiceResult<ConvertToVisitResultDto>.Ok(resultDto, " تم
بنجاحزيارةإلىالموعدتحويل.");
        }
        #endregion
    }
}
```

```
// Modules / Clinical / DTOs / Requests.cs
```

```
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
namespace Dental.Modules.Clinical.DTOs
{
    // ── Diagnosis Request ──
    public class AddDiagnosisRequest
    {
        public int VisitId { get; set; }
        public int DiagnosisId { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; } = false;
    }
    // ── Procedure Request ──
    public class AddProcedureRequest
    {
        public int VisitId { get; set; }
        public int ProcedureId { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? Price { get; set; } // null = use catalog default
        public string? Notes { get; set; }
    }
    // ── Payment Request ──
    public class AddPaymentRequest
    {
        public int VisitId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
    }
    // ── Dental Chart Request ──
    public class SaveChartRequest
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public List<ToothChartRequest> Teeth { get; set; } = new();
    }
    public class ToothChartRequest
    {
        public int Number { get; set; }
        public ToothStatus Status { get; set; }
        public List<ToothSurfaceRequest> Surfaces { get; set; } = new();
        public string? ProcedureCode { get; set; }
        public string? Notes { get; set; }
    }
    public class ToothSurfaceRequest
    {
        public string Surface { get; set; } = string.Empty; // M, D, B, L, O
        public string Condition { get; set; } = string.Empty; // Healthy,
Caries...
    }
    public class CreateAppointmentRequest
    {
```

```
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int Duration { get; set; } = 30; // minutes
        public string? Reason { get; set; }
    }
    public class AddPrescriptionRequest
    {
        public int VisitId { get; set; }
        public string? Diagnosis { get; set; }
        public string? Notes { get; set; }
        public string? PrescribedBy { get; set; }
        public List<PrescriptionItemRequest> Items { get; set; } = new();
    }
    public class PrescriptionItemRequest
    {
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }
}
// Modules / Clinical / DTOs / VisitDto.cs
```

```
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using static Dental.Constants.EnumHelper;
```

```
namespace Dental.Modules.Clinical.DTOs
{
    public class VisitDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime VisitDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? Notes { get; set; }
        public FollowUpType FollowUpType { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public VisitStatus Status { get; set; }
        public VisitCategory Category { get; set; }
        // Financial
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining { get; set; }
        // Related Data
        public List<VisitDiagnosisDto> Diagnoses { get; set; } = new();
        public List<VisitProcedureDto> Procedures { get; set; } = new();
        public List<VisitPaymentDto> Payments { get; set; } = new();
        public List<XRayDto> XRays { get; set; } = new();
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
    }
```

```
    public class VisitDiagnosisDto
    {
```

```
        public int Id { get; set; }
        public int DiagnosisCatalogId { get; set; }
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisCode { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
    }
    public class VisitProcedureDto
    {
        public int Id { get; set; }
        public int ProcedureCatalogId { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string? ProcedureCode { get; set; }
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public ProcedureStatus Status { get; set; }
        public string? Notes { get; set; }
    }
    public class VisitPaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? ReceiptNo { get; set; }
    }
}
// Modules / clinic / Mapping /ClinicalMappingProfile.cs
```

```
using AutoMapper;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Patients.DTOs;
namespace Dental.Modules.Clinical.Mapping
{
    public class ClinicalMappingProfile : Profile
    {
        public ClinicalMappingProfile()
        {
            // Visit → VisitDto
            CreateMap<Visit, VisitDto>()
                .ForMember(dest => dest.Remaining, opt => opt.MapFrom(src =>
src.Remaining));
```

```
            // VisitDiagnosis → VisitDiagnosisDto
            CreateMap<VisitDiagnosis, VisitDiagnosisDto>()
                .ForMember(dest => dest.DiagnosisName, opt => opt.MapFrom(src =>
src.Diagnosis.NameAr))
                .ForMember(dest => dest.DiagnosisCode, opt => opt.MapFrom(src =>
src.Diagnosis.Code));
```

```
            // VisitProcedure → VisitProcedureDto
            CreateMap<VisitProcedure, VisitProcedureDto>()
```

```
                .ForMember(dest => dest.ProcedureName, opt => opt.MapFrom(src =>
src.Procedure.NameAr))
```

```
                .ForMember(dest => dest.ProcedureCode, opt => opt.MapFrom(src =>
src.Procedure.Code))
```

```
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src =>
src.TotalPrice));
```

```
            // VisitPayment → VisitPaymentDto
            CreateMap<VisitPayment, VisitPaymentDto>();
```

```
            // PatientXRay → XRayDto (محتاجهلو)
            CreateMap<PatientXRay, XRayDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src =>
$"/uploads/xrays/{src.FileName}"));
```

```
            // Prescription → PrescriptionDto (الـمن DTOs الموجودة)
            CreateMap<Prescription, PrescriptionDto>();
        }
    }
}
```

```
// Modules / clinic / viewmodels / SessionViewModel.cs
```

```
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Modules.Clinical.ViewModels
{
    public class SessionViewModel
    {
        public PatientDetailsDto Patient { get; set; } = null!;
        public VisitDto Visit { get; set; } = null!;
        public List<DentalChartEntry> DentalHistory { get; set; } = new();
        public List<DiagnosisCatalog> AvailableDiagnoses { get; set; } = new();
        public List<ProcedureCatalog> AvailableProcedures { get; set; } = new();
    }
}
```

```
// Modules / patients / Dtos / All_DTOs.cs
```

```
// ============================================================
// 📁 File: Dental.Modules.Patients.DTOs (All DTOs)
// ============================================================
using Dental.Models.Patient;
```

```
namespace Dental.Modules.Patients.DTOs
{
    // ── Gender DTO ──
    public class GenderDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
    }
    // ── BloodGroup DTO ──
    public class BloodGroupDto
```

```
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
```

```
    // ── Patient DTO (For List/Index) ──
    public class PatientDto
    {
        public int Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
        public GenderDto? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public int Age { get; set; }
    }
```

```
    // ── Patient Details DTO (For Dashboard) ──
    public class PatientDetailsDto
    {
```

```
        public int Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public int Age => DateOfBirth.HasValue
            ? DateTime.Now.Year - DateOfBirth.Value.Year
            : 0;
        public GenderDto? Gender { get; set; }
        public BloodGroupDto? BloodGroup { get; set; }
        public string? NationalId { get; set; }
        public string? Occupation { get; set; }
        public string? Mobile { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
```

```
        // Medical Info (from PatientMedicalInfo navigation)
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
        public string? SmokingStatus { get; set; }
```

```
        // Dental History
        public DateTime? LastVisit { get; set; }
        public DateTime? NextAppointment { get; set; }
```

```
        public int VisitsCount { get; set; }
        public int GeneralVisitsCount { get; set; }
        public int OrthodonticsVisitsCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public DateTime? NextVisitDate { get; set; }
```

```
        public PatientType PatientType { get; set; }
    }
```

```
    // ── Patient Lookup DTO (For Search/Dropdowns) ──
    public class PatientLookupDto
    {
        public int Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullNameAr { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
    // ── Create Patient DTO ──
    public class CreatePatientDto
    {
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
        public int GenderId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public int? BloodGroupId { get; set; }
        public string? Notes { get; set; }
        public PatientType PatientType { get; set; } = PatientType.General;
```

```
        // Medical Info
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
        public string? MedicalNotes { get; set; }
        public string? SmokingStatus { get; set; }
        public bool IsPregnant { get; set; }
        public string? EmergencyNotes { get; set; }
    }
```

```
    // ── Update Patient DTO ──
    public class UpdatePatientDto
    {
        public int Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
```

```
        public int GenderId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public int? BloodGroupId { get; set; }
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? Notes { get; set; }
```

```
        public PatientType PatientType { get; set; }
```

`//  Medical Info -` ⭐ `جديد public string? Allergies { get; set; } public string? ChronicDiseases { get; set; } public string? CurrentMedications { get; set; } public string? MedicalNotes { get; set; } public string? SmokingStatus { get; set; } public bool IsPregnant { get; set; } public string? EmergencyNotes { get; set; }` 

```
    }
```

```
}
```

```
// Modules / patients / Dtos / Patient_Modules_DTOs.cs
```

```
using Dental.Models.Clinic;
using Dental.Models.Patient;
namespace Dental.Modules.Patients.DTOs
{
```

```
    // ============================================================
    // 📁 File: Dental.Modules.Patients.DTOs
    // ============================================================
    // ── Base DTOs ──
    public class SessionDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime SessionDate { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public SessionStatus Status { get; set; } = SessionStatus.Completed;
        public decimal Cost { get; set; }
        public int? ToothNumber { get; set; }
        public string DentistName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class XRayDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public XRayType Type { get; set; } = XRayType.Panoramic;
        public string Description { get; set; } = string.Empty;
        public DateTime TakenDate { get; set; }
        public string TakenBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class PrescriptionDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public List<PrescriptionItemDto> Items { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public string PrescribedBy { get; set; } = string.Empty;
        public PrescriptionStatus Status { get; set; } =
PrescriptionStatus.Active;
        public DateTime CreatedAt { get; set; }
    }
    public class PrescriptionItemDto
    {
        public int Id { get; set; }
```

```
        public int PrescriptionId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;       // e.g. "500mg"
        public string Frequency { get; set; } = string.Empty;    // e.g. "3
times daily"
        public string Duration { get; set; } = string.Empty;       // e.g. "7
days"
        public string Instructions { get; set; } = string.Empty;   // e.g.
"After meals"
    }
    public class PhotoDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public PhotoType Type { get; set; } = PhotoType.Intraoral;
        public string Category { get; set; } = string.Empty;      // e.g.
"Before", "After"
        public string Description { get; set; } = string.Empty;
        public DateTime TakenDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class PaymentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? SessionId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;
        public string Description { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class DocumentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FileExtension { get; set; }
        public long? FileSize { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedDate { get; set; }
    }
    // ── Enums ──
    public enum SessionStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Postponed
    }
```

```
    public enum XRayType
    {
        Panoramic,        // OPG
        Periapical,       // Single tooth
        Bitewing,         // Crown check
        Occlusal,         // Floor of mouth
        Cephalometric,    // Orthodontic
        CBCT              // 3D scan
    }
    public enum PrescriptionStatus
    {
        Active,
        Completed,
        Cancelled
    }
    public enum PhotoType
    {
        Portrait,         // Face photo
        Intraoral,        // Inside mouth
        Extraoral,        // Outside mouth
        XRay,             // Radiograph (duplicate for gallery)
        Smile,            // Smile line
        Profile           // Side view
    }
    public enum PaymentMethod
    {
        Cash,
        CreditCard,
        DebitCard,
        BankTransfer,
        Insurance,
        Installment
    }
    public enum PaymentStatus
    {
        Pending,
        Paid,
        PartiallyPaid,
        Refunded,
        Cancelled
    }
    // ── Dental Chart DTOs ──
    public class DentalChartDto
    {
        public int PatientId { get; set; }
        public List<ToothDto> Teeth { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
    public class ToothDto
    {
        public int ToothNumber { get; set; }          // FDI notation: 11-18,
21-28, 31-38, 41-48
        public string ToothName { get; set; } = string.Empty;
        public ToothStatus Status { get; set; } = ToothStatus.Healthy;
        public List<ToothSurfaceDto> Surfaces { get; set; } = new();
        public List<ToothTreatmentDto> Treatments { get; set; } = new();
```

```
        public string Notes { get; set; } = string.Empty;
    }
    public class ToothSurfaceDto
    {
        public ToothSurface Surface { get; set; }
        public SurfaceCondition Condition { get; set; } =
SurfaceCondition.Healthy;
```

```
        public string Notes { get; set; } = string.Empty;
    }
    public class ToothTreatmentDto
    {
        public int Id { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public DateTime TreatmentDate { get; set; }
        public string DentistName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
```

```
    //public enum ToothStatus
    //{
    //    Healthy,
    //    Decayed,          // Cavity
    //    Treated,          // Filled/Root canal
    //    Missing,
    //    Extracted,
    //    Impacted,
    //    Crown,
    //    Bridge,
    //    Implant,
    //    Fractured
    //}
```

```
    public enum ToothSurface
    {
        Mesial,           // Front side
        Distal,           // Back side
        Buccal,           // Cheek side
        Lingual,          // Tongue side
        Occlusal,         // Biting surface
        Incisal           // Cutting edge
    }
    public enum SurfaceCondition
    {
```

```
        Healthy,
        Caries,
        Filling,
        Crown,
        RootCanal,
        ExtractionNeeded
    }
}
```

```
// Modules / patients / Dtos / PatientListDto.cs
```

```
namespace Dental.Modules.Patients.DTOs
{
    public class PatientListDto
    {
        public int Id { get; set; }
        public string? PatientCode { get; set; }
```

```
        public string? FullNameAr { get; set; }
        public string? Phone { get; set; }
        public int? Age { get; set; }
        public string? GenderName { get; set; }
        public bool IsActive { get; set; } = true;
```

```
        // 📁 احتجناهلوالأماميالفلترفيمستخدمينالمباشر،للعرضمشخام، - جديد
        public bool HasGeneralVisits { get; set; }
        public bool HasOrthodonticsVisits { get; set; }
        public string GenderBadge => GenderName switch
        {
```

```
            "ذكر" => "<span class=\"badge bg-primary-subtle text-primary fw-bold
px-2 py-1 rounded-3\"><i class=\"bi bi-gender-male\"></i> ذكر</span>",
```

```
            "انثى" => "<span class=\"badge bg-danger-subtle text-danger fw-bold
px-2 py-1 rounded-3\"><i class=\"bi bi-gender-female\"></i> انثى</span>",
```

```
            _ => "<span class=\"badge bg-danger-subtle text-danger fw-bold px-2
py-1 rounded-3\"><i class=\"bi bi-gender-female\"></i> انثى</span>"
        };
```

```
        public string StatusBadge => IsActive
```

```
            ? "<span class=\"badge bg-success-subtle text-success fw-bold px-2
py-1 rounded-3\"><i class=\"bi bi-check-circle-fill\"></i> نشط</span>"
            : "<span class=\"badge bg-danger-subtle text-danger fw-bold px-2
py-1 rounded-3\"><i class=\"bi bi-x-circle-fill\"></i> نشطغير</span>";
```

```
        // 📁 (الملفينعندهالمريضلوبعضمعلتنينيبانممكن) النوعبادچ - جديد
        public string CategoryBadge
        {
            get
            {
                if (!HasGeneralVisits && !HasOrthodonticsVisits)
                    return "<span class=\"badge bg-light text-muted border fw-
bold px-2 py-1 rounded-3\">زياراتيوجدال</span>";
```

```
                var badges = "";
                if (HasGeneralVisits)
                    badges += "<span class=\"badge bg-info-subtle text-info fw-
bold px-2 py-1 rounded-3 me-1\"><i class=\"bi bi-app-indicator\"></i>
عام</span>";
```

```
                if (HasOrthodonticsVisits)
                    badges += "<span class=\"badge bg-warning-subtle text-
warning fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-braces\"></i>
تًقويم</span>";
```

```
                return badges;
            }
        }
    }
}
```

```
// Modules / patients / Enums
```

```
namespace Dental.Modules.Patients.Enums
{
    public enum VisitCategory
    {
        General = 1,
        Orthodontics = 2
    }
```

```
    public enum FollowUpType
    {
```

```
        Normal = 1,
        Long = 2
    }
    public enum VisitStatus
    {
        InProgress = 1,
        Completed = 2,
        Cancelled = 3,
        NoShow = 4
    }
```

```
}
```

```
// Modules / patients / InterfacesPatients
```

```
// ============================================================
// 📁 File: IPatientService.cs
// ============================================================
```

```
using Dental.Core.Pagination;
using Dental.Core.Results;
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Modules.Patients.InterfacesPatients
{
```

```
    public interface IPatientService
    {
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedRequest
request);
        Task<List<PatientLookupDto>> SearchAsync(string keyword);
        Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(int id);
        Task<ServiceResult<int>> CreateAsync(CreatePatientDto dto);
        Task<ServiceResult<bool>> UpdateAsync(UpdatePatientDto dto);
        Task<ServiceResult<bool>> DeleteAsync(int id);
        Task<string> GeneratePatientCodeAsync();
```

```
        // 📁 تحًققلكلموحدةواحدةنسخة - excludeId اختياري (null =  ،إضافةجديدة
تعديل = قيمةله)
        Task<bool> ExistsMobileAsync(string mobile, int? excludeId = null);
        Task<bool> ExistsPhoneAsync(string phone, int? excludeId = null);
        Task<bool> ExistsNationalIdAsync(string nationalId, int? excludeId =
null);
    }
}
```

```
// Modules / patients / MappingPatients
```

```
// ============================================================
// 📁 File: PatientMappingProfile.cs (AutoMapper)
```

```
// ============================================================
```

```
using AutoMapper;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
```

```
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
```

```
        // ── Entity → DTO ──
        CreateMap<Patient, PatientDto>();
```

```
        CreateMap<Patient, PatientDetailsDto>()
            .ForMember(dest => dest.Allergies,
                opt => opt.MapFrom(src => src.MedicalInfo != null ?
src.MedicalInfo.Allergies : null))
```

```
            .ForMember(dest => dest.ChronicDiseases,
                opt => opt.MapFrom(src => src.MedicalInfo != null ?
src.MedicalInfo.ChronicDiseases : null))
            .ForMember(dest => dest.CurrentMedications,
                opt => opt.MapFrom(src => src.MedicalInfo != null ?
src.MedicalInfo.CurrentMedications : null));
```

```
        CreateMap<Patient, PatientLookupDto>();
```

```
        // ── DTO → Entity ──
        CreateMap<CreatePatientDto, Patient>();
        CreateMap<UpdatePatientDto, Patient>()
        .ForMember(dest => dest.GenderId, opt => opt.MapFrom(src =>
src.GenderId))
        .ForMember(dest => dest.BloodGroupId, opt => opt.MapFrom(src =>
src.BloodGroupId));
```

```
        // ── Related Entities → DTOs ──
        CreateMap<Gender, GenderDto>();
        CreateMap<BloodGroup, BloodGroupDto>();
    }
}
```

```
namespace Dental.Modules.Patients.MappingPatients
{
    public class PatientMappings
    {
    }
}
```

```
// Modules / patients / ServicesPatients
```

```
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dental.Core.Pagination;
using Dental.Core.Results;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using Dental.Modules.Patients.InterfacesPatients;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
```

```
namespace Dental.Modules.Patients.ServicesPatients
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
```

```
        public PatientService(ApplicationDbContext context, IMapper mapper,
```

```
UserManager<ApplicationUser> userManager)
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
```

```
        // ── Helper: Check Mobile Exists ──
        public async Task<bool> ExistsMobileAsync(string mobile, int? excludeId
= null)
```

```
        {
            return await _context.Patients
                .AnyAsync(x => x.Mobile == mobile && (!excludeId.HasValue ||
x.Id != excludeId.Value));
```

```
        }
```

```
        // ── Helper: Check Phone Exists ──
        public async Task<bool> ExistsPhoneAsync(string phone, int? excludeId =
null)
        {
            return await _context.Patients
                .AnyAsync(x => x.Phone == phone && (!excludeId.HasValue ||
x.Id != excludeId.Value));
```

```
        }
```

```
        // ── Helper: Check National ID Exists ──
        public async Task<bool> ExistsNationalIdAsync(string nationalId, int?
excludeId = null)
        {
            return await _context.Patients
                .AnyAsync(x => x.NationalId == nationalId && (!
excludeId.HasValue || x.Id != excludeId.Value));
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
```

```
        // ── Get By Id: FULL DATA + Visit Stats ──
        public async Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(int id)
```

```
        {
            var patient = await _context.Patients
                .Include(p => p.Gender)
                .Include(p => p.BloodGroup)
                .Include(p => p.MedicalInfo)
                .Include(p => p.Visits)
                .FirstOrDefaultAsync(x => x.Id == id);
```

```
            if (patient == null)
                return ServiceResult<PatientDetailsDto>.Fail("Patient not
found.");
```

```
            var dto = _mapper.Map<PatientDetailsDto>(patient);
```

```
            // 📁 استخدم IsActive و Category من Visit
            var activeVisits = patient.Visits.Where(v => v.IsActive).ToList();
```

```
            dto.VisitsCount = activeVisits.Count;
            dto.TotalPaid = activeVisits.Sum(v => v.PaidAmount);
            dto.TotalDue = activeVisits.Sum(v => v.Remaining);
            dto.GeneralVisitsCount = activeVisits.Count(v => v.Category ==
VisitCategory.General);
            dto.OrthodonticsVisitsCount = activeVisits.Count(v => v.Category ==
VisitCategory.Orthodontics);
            dto.NextVisitDate = activeVisits
                .Where(v => v.FollowUpDate.HasValue && v.FollowUpDate.Value.Date
>= DateTime.Today)
                .OrderBy(v => v.FollowUpDate)
                .Select(v => v.FollowUpDate)
                .FirstOrDefault();
```

```
            return ServiceResult<PatientDetailsDto>.Ok(dto);
        }
        // ── Create ──
        public async Task<ServiceResult<int>> CreateAsync(CreatePatientDto dto)
        {
            if (await ExistsMobileAsync(dto.Mobile))
                return ServiceResult<int>.Fail(" لمريضبالفعلمسجلالموبايلرقم
آخر.");
```

```
            if (!string.IsNullOrWhiteSpace(dto.Phone) && await
ExistsPhoneAsync(dto.Phone))
                return ServiceResult<int>.Fail(" لمريضبالفعلمسجلالتليفونرقم
آخر.");
```

```
            if (!string.IsNullOrWhiteSpace(dto.NationalId) && await
ExistsNationalIdAsync(dto.NationalId))
                return ServiceResult<int>.Fail(" لمريضبالفعلمسجلالًقوميالرقم
آخر.");
```

```
            var patient = _mapper.Map<Patient>(dto);
            patient.PatientCode = await GeneratePatientCodeAsync();
            patient.IsActive = true;
            patient.CreatedBy = 1;
```

```
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();
```

```
            return ServiceResult<int>.Ok(patient.Id, "بنجاحالمريضإضافةتم.");
        }
        // ── Update ──
        // ── Update ──
```

```
        public async Task<ServiceResult<bool>> UpdateAsync(UpdatePatientDto dto)
        {
            var patient = await _context.Patients
                .Include(p => p.MedicalInfo)
                .FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (patient == null)
                return ServiceResult<bool>.Fail("Patient not found.");
            if (!string.IsNullOrWhiteSpace(dto.Mobile) && await
ExistsMobileAsync(dto.Mobile, dto.Id))
                return ServiceResult<bool>.Fail(" لمريضبالفعلمسجلالموبايلرقم
آخر.");
```

```
            if (!string.IsNullOrWhiteSpace(dto.Phone) && await
ExistsPhoneAsync(dto.Phone, dto.Id))
                return ServiceResult<bool>.Fail(" لمريضبالفعلمسجلالتليفونرقم
آخر.");
```

```
            if (!string.IsNullOrWhiteSpace(dto.NationalId) && await
ExistsNationalIdAsync(dto.NationalId, dto.Id))
                return ServiceResult<bool>.Fail(" لمريضبالفعلمسجلالًقوميالرقم
آخر.");
```

`// Update basic info patient.GenderId = dto.GenderId; patient.BloodGroupId = dto.BloodGroupId; _mapper.Map(dto, patient); //  Update Medical Info` ⭐ `if (patient.MedicalInfo == null) { patient.MedicalInfo = new PatientMedicalInfo { PatientId = patient.Id, Allergies = dto.Allergies, ChronicDiseases = dto.ChronicDiseases, CurrentMedications = dto.CurrentMedications, MedicalNotes = dto.MedicalNotes, SmokingStatus = dto.SmokingStatus, IsPregnant = dto.IsPregnant, EmergencyNotes = dto.EmergencyNotes }; } else { patient.MedicalInfo.Allergies = dto.Allergies; patient.MedicalInfo.ChronicDiseases = dto.ChronicDiseases; patient.MedicalInfo.CurrentMedications = dto.CurrentMedications; patient.MedicalInfo.MedicalNotes = dto.MedicalNotes; patient.MedicalInfo.SmokingStatus = dto.SmokingStatus; patient.MedicalInfo.IsPregnant = dto.IsPregnant; patient.MedicalInfo.EmergencyNotes = dto.EmergencyNotes; } await _context.SaveChangesAsync(); return ServiceResult<bool>.Ok(true, " المريض بيانات تعديل تم بنجاح."); } // ── Delete (Soft Deactivate) ── public async Task<ServiceResult<bool>> DeleteAsync(int id) {` 

```
            var patient = await _context.Patients.FirstOrDefaultAsync(x => x.Id
== id);
```

```
            if (patient == null)
                return ServiceResult<bool>.Fail("Patient not found.");
```

```
            patient.IsActive = false;
            await _context.SaveChangesAsync();
```

```
            return ServiceResult<bool>.Ok(true, "بنجاحالمريضتعطيلتم.");
        }
```

```
        public async Task<ServiceResult<PagedResult<PatientDto>>>
GetPagedAsync(PagedRequest request)
        {
```

```
            var query = _context.Patients
                .Where(x => x.IsActive)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.FullNameAr.Contains(search) ||
                    (x.FullNameEn != null && x.FullNameEn.Contains(search)) ||
                    (x.Phone != null && x.Phone.Contains(search)));
            }
```

```
            // 📁 FIX: الـحساب Total الـقبل Skip/Take
            var total = await query.CountAsync();
            var patientsEntity = await query
                .OrderByDescending(x => x.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(x => x.Gender)
                .ToListAsync();
            var patients = _mapper.Map<List<PatientDto>>(patientsEntity);
```

```
            var result = new PagedResult<PatientDto>
            {
                Items = patients,
                TotalCount = total,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
```

```
            return ServiceResult<PagedResult<PatientDto>>.Ok(result);
        }
    }
}
```

```
// Modules / patients / ValidatorsPatients
```

```
// ============================================================
// 📁 File: CreatePatientValidator.cs (Updated)
// ============================================================
```

```
using Dental.Modules.Patients.DTOs;
using FluentValidation;
```

```
namespace Dental.Modules.Patients.ValidatorsPatients
```

```
{
    public class CreatePatientValidator : AbstractValidator<CreatePatientDto>
    {
        public CreatePatientValidator()
        {
            RuleFor(x => x.FullNameAr)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(200)
                .WithMessage("Full name must not exceed 200 characters.");
            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .WithMessage("Phone must not exceed 20 characters.");
            RuleFor(x => x.Mobile)
                .MaximumLength(20)
                .WithMessage("Mobile must not exceed 20 characters.");
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.NationalId)
                .Length(14)
                .WithMessage("National ID must be exactly 14 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.NationalId));
            // 📁 Changed from Gender (enum) to GenderId (int)
            RuleFor(x => x.GenderId)
                .GreaterThan(0)
                .WithMessage("Gender is required.");
```

```
            RuleFor(x => x.Address)
                .MaximumLength(500)
                .WithMessage("Address must not exceed 500 characters.");
```

```
            RuleFor(x => x.Notes)
                .MaximumLength(2000)
                .WithMessage("Notes must not exceed 2000 characters.");
        }
    }
}
```

```
using Dental.Modules.Patients.DTOs;
using FluentValidation;
```

```
namespace Dental.Modules.Patients.ValidatorsPatients
{
```

```
    public class UpdatePatientValidator : AbstractValidator<UpdatePatientDto>
    {
        public UpdatePatientValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
```

```
            RuleFor(x => x.FullNameAr)
                .NotEmpty().WithMessage("مطلوبالكامللسم.")
                .MaximumLength(200).WithMessage("حرف200يتجاوزاللسم.");
            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("مطلوبالموبايلرقم.")
                .MaximumLength(20).WithMessage("رقم20يتجاوزالالموبايلرقم.");
```

|<br>|`RuleFor(x => x.Phone)`<br>`.MaximumLength(20).WithMessage("`<br> `رقم التليفون ل يتجاوز20`<br> `رقم.");`|
|---|---|
|<br> <br>|`RuleFor(x => x.Email)`<br>`.EmailAddress().WithMessage("`<br> `صيغة البريد غير صحيحة.")`<br>`.When(x => !string.IsNullOrWhiteSpace(x.Email));`|
|<br> <br>|`RuleFor(x => x.NationalId)`<br>`.Length(14).WithMessage("`<br> `الرقم القومي يجب أن يكون14`<br> `رقم.")`<br>`.When(x => !string.IsNullOrWhiteSpace(x.NationalId));`|
|<br>|`RuleFor(x => x.GenderId)`<br>`.GreaterThan(0).WithMessage("`<br> `النوع مطلوب.");`|
|<br>|`RuleFor(x => x.Address)`<br>`.MaximumLength(500).WithMessage("`<br> `العنوان ل يتجاوز500`<br> `حرف.");`|
|<br>|`RuleFor(x => x.Notes)`<br>`.MaximumLength(2000).WithMessage("`<br> `الملحظات ل تتجاوز2000`<br> `حرف.");`|
|<br> <br> <br> <br> <br>`حرف.");`|`// Medical Info Validation`<br>`RuleFor(x => x.Allergies)`<br>`.MaximumLength(500).WithMessage("`<br> `الحساسية ل تتجاوز500`<br> `حرف.");`<br>`RuleFor(x => x.ChronicDiseases)`<br>`.MaximumLength(500).WithMessage("`<br> `الأمراض المزمنة ل تتجاوز500`|
|<br> <br>`حرف.");`<br> <br> <br>`حرف.");`|`RuleFor(x => x.CurrentMedications)`<br>`.MaximumLength(500).WithMessage("`<br> `الأدوية الحالية ل تتجاوز500`<br>`RuleFor(x => x.MedicalNotes)`<br>`.MaximumLength(2000).WithMessage("`<br> `الملحظات الطبية ل تتجاوز2000`|
|<br> <br>`حرف.");`<br>`}`|`RuleFor(x => x.EmergencyNotes)`<br>`.MaximumLength(1000).WithMessage("`<br> `ملحظات الطوارئ ل تتجاوز1000`|
|<br>`}`<br>`}`||



```
// Modules / patients / ViewModels
// ============================================================
// 📁 File: PatientIndexViewModel.cs
// ============================================================
```

```
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Modules.Patients.ViewModels
{
    public class PatientIndexViewModel
    {
        public List<PatientDto> Patients { get; set; } = new();
        public string? Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount /
PageSize);
```

```
        public int TotalPatients { get; set; }
        public int NewThisMonth { get; set; }
        public int ActiveToday { get; set; }
    }
}
```

```
// ============================================================
// 📁 File: PatientProfileViewModel.cs
```

```
// ============================================================
```

```
using Dental.Modules.Patients.DTOs;
```

```
namespace Dental.Modules.Patients.ViewModels
{
    public class PatientProfileViewModel
    {
        public PatientDetailsDto Patient { get; set; } = null!;
        public int VisitsCount { get; set; }
        public int AppointmentsCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public DentalChartDto? DentalChart { get; set; }
        public List<SessionDto> Sessions { get; set; } = new();
        public List<XRayDto> XRays { get; set; } = new();
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
        public List<PhotoDto> Photos { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
        public List<DocumentDto> Documents { get; set; } = new();
        public decimal TotalCost => Payments?.Sum(p => p.Amount) ?? 0;
        public decimal BalanceDue => TotalCost - TotalPaid;
        public bool HasAllergies => !string.IsNullOrEmpty(Patient?.Allergies);
        public bool HasChronicDiseases => !
string.IsNullOrEmpty(Patient?.ChronicDiseases);
        public bool HasPendingPayments => Payments?.Any(p => p.Status ==
PaymentStatus.Pending) ?? false;
        public int CompletedSessions => Sessions?.Count(s => s.Status ==
SessionStatus.Completed) ?? 0;
        public int UpcomingAppointments => AppointmentsCount;
        public bool HasUpcomingAppointment =>
Patient?.NextAppointment.HasValue ?? false;
```

```
    }
}
```

```
// Modules / Payments / DTOs / PaymentDtos.cs
```

```
// ============================================================
```

```
// 📁 File: Modules/Payments/DTOs/PaymentDtos.cs
```

```
// ============================================================
using Dental.Modules.Patients.DTOs; // PaymentMethod, PaymentStatus  هناUمعرفين
[فيأصالمشروعك
using Dental.Modules.Patients.Enums;
```

```
namespace Dental.Modules.Payments.DTOs
{
    // ── (الحسابكشففييستخدم) واحدةزيارةملخص ──
    public class VisitSummaryDto
    {
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public VisitCategory Category { get; set; }
        public decimal TotalAmount { get; set; }
```

```
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining { get; set; }
        public bool IsFullyPaid => Remaining <= 0;
    }
    // ── (الحسابكشففييستخدم) واحدةدفعةسجل ──
    public class PaymentRecordDto
    {
```

```
        public int Id { get; set; }
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string MethodText { get; set; } = null!;
        public string? Description { get; set; }
        public string? ReceiptNo { get; set; }
        public DateTime PaymentDate { get; set; }
    }
    // ── (بعضمعالزياراتكل) للمريضالشاملالحسابكشف ──
    public class PatientStatementDto
    {
```

```
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientMobile { get; set; } = null!;
        public string PatientCode { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public List<VisitSummaryDto> Visits { get; set; } = new();
        public List<PaymentRecordDto> Payments { get; set; } = new();
    }
```

```
    // ── جديدةدفعةتسجيل ──
    public class AddPaymentDto
    {
        public int VisitId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string? Description { get; set; }
    }
    // ── الدفعةتسجيلنتيجة (غيرUيحتاجهامنفور[االأرقامثيحدالفرونت Reload)
──
    public class AddPaymentResultDto
    {
        public int PaymentId { get; set; }
        public string? ReceiptNo { get; set; }
        public decimal VisitPaidAmount { get; set; }
        public decimal VisitRemaining { get; set; }
    }
}
```

```
// Modules / Payments / InterfacesPayments / IPaymentService.cs
```

```
// ============================================================
// 📁 File: Modules/Payments/InterfacesPayments/IPaymentService.cs
// ============================================================
using Dental.Core.Results;
```

```
using Dental.Modules.Payments.DTOs;
```

```
namespace Dental.Modules.Payments.InterfacesPayments
{
    public interface IPaymentService
    {
        // ── (دفعاتهكل + زياراتهكل) للمريضالشاملالحسابكشف ──
        Task<ServiceResult<PatientStatementDto>> GetPatientStatementAsync(int
patientId);
        // ── معينةزيارةعلىجديدةدفعةتسجيل ──
        Task<ServiceResult<AddPaymentResultDto>> AddPaymentAsync(AddPaymentDto
dto);
```

```
        // ── دفعةعنتراجع/إلغاء (Soft delete + الزيارةرصيدتصحيح) ──
        Task<ServiceResult<bool>> VoidPaymentAsync(int paymentId, string?
reason);
    }
}
```

```
// Modules / Payments / ServicesPayments / PaymentService.cs
```

```
// ============================================================
// 📁 File: Modules/Payments/ServicesPayments/PaymentService.cs
// ============================================================
using Dental.Core.Results;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Payments.DTOs;
using Dental.Modules.Payments.InterfacesPayments;
using Microsoft.EntityFrameworkCore;
```

```
namespace Dental.Modules.Payments.ServicesPayments
{
    // مهمةتصميممالحظة: Visit.PaidAmount فضل Field نUمخز (مناليفمحسوبمش
VisitPayments)
    //  كودأي .فيهUبًقرارليعدلهالمسموحالوحيدالمكانهودهالكالسلكنمًقصود،
تاني
    // (Controller جديدأوقديم)  متزامنينيفضلواالرقمينعشانهنامنيمرالزم
دايم[ا.
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }
```

```
        private static string GetMethodText(PaymentMethod method) => method
switch
        {
            PaymentMethod.Cash => "نًقدي",
            PaymentMethod.CreditCard => "ائتمانبطاقة",
            PaymentMethod.DebitCard => "خصمبطاقة",
            PaymentMethod.BankTransfer => "بنكيتحويل",
            PaymentMethod.Insurance => "تأمين",
            PaymentMethod.Installment => "تًقسيط",
            _ => method.ToString()
        };
```

```
        public async Task<ServiceResult<PatientStatementDto>>
```

```
GetPatientStatementAsync(int patientId)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null)
                return ServiceResult<PatientStatementDto>.Fail(" غيرالمريض
موجود.");
```

```
            var visits = await _context.Visits
                .AsNoTracking()
                .Where(v => v.PatientId == patientId && v.IsActive)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();
            var visitIds = visits.Select(v => v.Id).ToList();
            var payments = await _context.VisitPayments
                .AsNoTracking()
                .Where(p => visitIds.Contains(p.VisitId) && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
```

```
            var visitDateLookup = visits.ToDictionary(v => v.Id, v =>
v.VisitDate);
```

```
            var dto = new PatientStatementDto
            {
                PatientId = patient.Id,
                PatientName = patient.FullNameAr,
                PatientMobile = patient.Mobile,
                PatientCode = patient.PatientCode,
                TotalAmount = visits.Sum(v => v.TotalAmount),
                TotalDiscount = visits.Sum(v => v.Discount),
                TotalPaid = visits.Sum(v => v.PaidAmount),
                TotalRemaining = visits.Sum(v => v.Remaining),
                Visits = visits.Select(v => new VisitSummaryDto
                {
                    VisitId = v.Id,
                    VisitDate = v.VisitDate,
                    Category = v.Category,
                    TotalAmount = v.TotalAmount,
                    Discount = v.Discount,
                    PaidAmount = v.PaidAmount,
                    Remaining = v.Remaining
                }).ToList(),
                Payments = payments.Select(p => new PaymentRecordDto
                {
                    Id = p.Id,
                    VisitId = p.VisitId,
                    VisitDate = visitDateLookup.TryGetValue(p.VisitId, out var
vd) ? vd : default,
                    Amount = p.Amount,
                    Method = p.Method,
                    MethodText = GetMethodText(p.Method),
                    Description = p.Description,
                    ReceiptNo = p.ReceiptNo,
                    PaymentDate = p.PaymentDate
                }).ToList()
            };
```

```
            return ServiceResult<PatientStatementDto>.Ok(dto);
        }
```

```
        public async Task<ServiceResult<AddPaymentResultDto>>
AddPaymentAsync(AddPaymentDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult<AddPaymentResultDto>.Fail(" يجبالدفعةقيمة
صفرمنأكبرتكونأن.");
            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id ==
dto.VisitId && v.IsActive);
            if (visit == null)
                return ServiceResult<AddPaymentResultDto>.Fail(" غيرالزيارة
نشطةغيرأوموجودة.");
            //  علىبيحافظ) الزيارةعلىفعلي[االمتبًقيمنأكبردفعةتسجيلمنع
(الأرقامتطابق
            if (dto.Amount > visit.Remaining)
                return ServiceResult<AddPaymentResultDto>.Fail(
                    $"الدفعةقيمة ({dto.Amount:N2}) الزيارةعلىالمتبًقيمنأكبر
({visit.Remaining:N2}).");
```

```
            var receiptNo = await GenerateReceiptNoAsync();
```

```
            var payment = new VisitPayment
            {
                VisitId = dto.VisitId,
                Amount = dto.Amount,
                Method = dto.Method,
                Description = dto.Description,
                ReceiptNo = receiptNo
            };
```

```
            _context.VisitPayments.Add(payment);
            visit.PaidAmount += dto.Amount;
```

```
            await _context.SaveChangesAsync();
```

```
            var result = new AddPaymentResultDto
            {
                PaymentId = payment.Id,
                ReceiptNo = payment.ReceiptNo,
                VisitPaidAmount = visit.PaidAmount,
                VisitRemaining = visit.Remaining
            };
```

```
            return ServiceResult<AddPaymentResultDto>.Ok(result, " تسجيلتم
بنجاحالدفعة.");
        }
```

```
        public async Task<ServiceResult<bool>> VoidPaymentAsync(int paymentId,
string? reason)
        {
```

```
            var payment = await _context.VisitPayments.FirstOrDefaultAsync(p =>
p.Id == paymentId && !p.IsDeleted);
```

```
            if (payment == null)
                return ServiceResult<bool>.Fail("إلغاؤهاتمأوموجودةغيرالدفعة
بالفعل.");
            var visit = await _context.Visits.FindAsync(payment.VisitId);
            if (visit == null)
                return ServiceResult<bool>.Fail(" غيربالدفعةالمرتبطةالزيارة
```

```
موجودة.");
```

```
            payment.IsDeleted = true;
            payment.Description = string.IsNullOrWhiteSpace(reason)
                ? $"{payment.Description} [ملغاة]"
                : $"{payment.Description} [ملغاة: {reason}]";
            visit.PaidAmount -= payment.Amount;
            if (visit.PaidAmount < 0) visit.PaidAmount = 0; //  انحرافمنحماية
نادرةلتحفيالأرقام
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, " رصيدوتصحيحالدفعةإلغاءتم
الزيارة.");
        }
        // علىلعتمادبدل (،واحدةعيادةلحجممناسب) بسيطتسلسليإيصالرقم
Random يتكررممكناللي
        private async Task<string> GenerateReceiptNoAsync()
        {
            var countToday = await _context.VisitPayments
                .CountAsync(p => p.PaymentDate.Date == DateTime.Today);
            return $"REC-{DateTime.Now:yyyyMMdd}-{(countToday + 1):D4}";
        }
    }
}
```

```
///////////////////// View -----------------
// Views / Appointments /
@{
    ViewData["Title"] = "المواعيد";
    Layout = "~/Views/Shared/_Layout.cshtml";
}
<style>
    :root {
        --sc-teal-900: #0a4d4f;
        --sc-teal-700: #0d7377;
        --sc-teal-600: #0f9b8e;
        --sc-teal-500: #14b8a6;
        --sc-teal-100: #c7f2ea;
        --sc-sky-50: #eafbfc;
        --sc-sky-100: #dcf5f7;
        --sc-coral-300: #f6c6bc;
        --sc-ink-900: #0b2e30;
        --sc-ink-600: #4b6b6c;
        --brand: #0d9488;
        --brand-dark: #0f766e;
        --brand-light: #f0fdfa;
        --radius-lg: 18px;
        --radius-md: 12px;
        --shadow-soft: 0 1px 3px rgba(15, 23, 42, 0.06);
        --shadow-hover: 0 10px 26px rgba(10, 77, 79, 0.12);
        --shadow-card: 0 20px 50px rgba(10, 77, 79, .08);
    }
    body {
        background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
```

```
transparent 26%), var(--sc-sky-50);
    }
    /* ── Hero ── */
    .calendar-hero {
        background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
        border-radius: var(--radius-lg);
        padding: 22px 28px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
        margin-bottom: 20px;
        flex-wrap: wrap;
        gap: 16px;
        position: relative;
        overflow: hidden;
    }
        .calendar-hero::after {
            content: '';
            position: absolute;
            inset: 0;
            background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
            pointer-events: none;
        }
        .calendar-hero .hero-content {
            display: flex;
            align-items: center;
            gap: 16px;
            position: relative;
            z-index: 1;
        }
        .calendar-hero .hero-icon {
            width: 52px;
            height: 52px;
            background: rgba(255,255,255,.18);
            color: #fff;
            border-radius: 14px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.4rem;
            flex-shrink: 0;
            backdrop-filter: blur(6px);
            border: 1px solid rgba(255,255,255,.15);
        }
        .calendar-hero h1 {
            font-size: 1.25rem;
            font-weight: 800;
            margin: 0;
            color: #fff;
        }
        .calendar-hero p {
            font-size: 0.85rem;
            margin: 4px 0 0;
            color: rgba(255,255,255,.9);
            font-weight: 600;
```

```
        }
    .btn-hero-new {
        background: #fff;
        color: var(--sc-teal-700);
        border: none;
        padding: 11px 22px;
        border-radius: 12px;
        font-weight: 800;
        display: flex;
        align-items: center;
        gap: 8px;
        text-decoration: none;
        box-shadow: 0 8px 20px rgba(10, 40, 42, .18);
        transition: all .2s ease;
        font-size: 0.9rem;
        position: relative;
        z-index: 1;
        cursor: pointer;
    }
        .btn-hero-new:hover {
            transform: translateY(-2px);
            box-shadow: 0 12px 26px rgba(10, 40, 42, .24);
            color: var(--sc-teal-700);
        }
    /* ── Stats Bar ── */
    .stats-bar {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 14px;
        margin-bottom: 20px;
    }
    .stat-card-glass {
        background: rgba(255,255,255,.78);
        backdrop-filter: blur(14px);
        border-radius: 16px;
        padding: 18px 20px;
        display: flex;
        align-items: center;
        gap: 14px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: var(--shadow-soft);
        transition: all .25s ease;
    }
        .stat-card-glass:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow-hover);
        }
    .stat-icon-box {
        width: 46px;
        height: 46px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.15rem;
        flex-shrink: 0;
    }
```

```
        .stat-icon-box.teal {
            background: #f0fdfa;
            color: #0f766e;
        }
        .stat-icon-box.blue {
            background: #eff6ff;
            color: #1d4ed8;
        }
        .stat-icon-box.green {
            background: #f0fdf4;
            color: #166534;
        }
        .stat-icon-box.orange {
            background: #fffbeb;
            color: #b45309;
        }
    .stat-value-lg {
        display: block;
        font-size: 1.4rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        line-height: 1.1;
    }
    .stat-label-lg {
        font-size: 0.78rem;
        color: var(--sc-ink-600);
        font-weight: 600;
    }
    /* ── Calendar Shell ── */
    .calendar-shell {
        background: rgba(255,255,255,.82);
        backdrop-filter: blur(16px);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow-card);
        overflow: hidden;
        border: 1px solid rgba(255,255,255,.6);
        padding: 24px;
    }
    .calendar-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 24px;
        flex-wrap: wrap;
        gap: 12px;
    }
    .cal-title {
        font-size: 1.1rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        margin: 0;
    }
    .cal-nav-group {
        display: flex;
        align-items: center;
```

```
        gap: 12px;
    }
    .cal-nav {
        width: 40px;
        height: 40px;
        border-radius: 12px;
        border: 1.5px solid var(--sc-sky-100);
        background: #fff;
        color: var(--sc-ink-600);
        cursor: pointer;
        transition: all .2s ease;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.1rem;
    }
        .cal-nav:hover {
            background: var(--brand);
            color: #fff;
            border-color: var(--brand);
            transform: translateY(-1px);
            box-shadow: 0 4px 12px rgba(13,148,136,0.25);
        }
    .cal-today-btn {
        padding: 8px 16px;
        border-radius: 10px;
        border: 1.5px solid var(--sc-sky-100);
        background: #fff;
        color: var(--sc-ink-600);
        font-weight: 700;
        font-size: 0.82rem;
        cursor: pointer;
        transition: all .2s ease;
    }
        .cal-today-btn:hover {
            background: var(--brand-light);
            border-color: var(--sc-teal-500);
            color: var(--brand-dark);
        }
    /* ── Week Grid ── */
    .week-grid {
        display: grid;
        grid-template-columns: repeat(7, 1fr);
        gap: 12px;
    }
    .day-column {
        background: rgba(255,255,255,.6);
        border-radius: 16px;
        border: 1px solid rgba(226,232,240,0.5);
        min-height: 320px;
        overflow: hidden;
        transition: all 0.3s ease;
        animation: dayFadeIn 0.5s ease forwards;
        opacity: 0;
    }
        .day-column:hover {
            box-shadow: 0 8px 24px rgba(10, 77, 79, 0.08);
```

```
            border-color: var(--sc-teal-500);
            transform: translateY(-2px);
        }
        .day-column.is-today {
            border-color: var(--sc-teal-500);
            box-shadow: 0 0 0 3px rgba(13,148,136,0.15), 0 8px 24px
rgba(13,148,136,0.1);
            background: rgba(255,255,255,.9);
        }
    @@keyframes dayFadeIn {
        from {
            opacity: 0;
            transform: translateY(20px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .day-column:nth-child(1) {
        animation-delay: .02s;
    }
    .day-column:nth-child(2) {
        animation-delay: .04s;
    }
    .day-column:nth-child(3) {
        animation-delay: .06s;
    }
    .day-column:nth-child(4) {
        animation-delay: .08s;
    }
    .day-column:nth-child(5) {
        animation-delay: .10s;
    }
    .day-column:nth-child(6) {
        animation-delay: .12s;
    }
    .day-column:nth-child(7) {
        animation-delay: .14s;
    }
    .day-header {
        padding: 14px 10px;
        text-align: center;
        border-bottom: 1px solid var(--sc-sky-100);
        background: linear-gradient(135deg, var(--sc-sky-50), transparent);
        position: relative;
    }
    .day-name {
        font-size: .75rem;
        font-weight: 800;
        color: var(--sc-ink-600);
        text-transform: uppercase;
```

```
        letter-spacing: 1px;
    }
    .day-number {
        font-size: 1.5rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        margin-top: 4px;
    }
    .day-column.is-today .day-number {
        color: var(--sc-teal-600);
    }
    .today-indicator {
        position: absolute;
        top: 8px;
        left: 8px;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: var(--sc-teal-500);
        box-shadow: 0 0 0 3px rgba(13,148,136,0.2);
        animation: pulseToday 2s infinite;
    }
    @@keyframes pulseToday {
        0%, 100% {
            transform: scale(1);
            opacity: 1;
        }
        50% {
            transform: scale(1.3);
            opacity: 0.7;
        }
    }
    .day-slots {
        padding: 10px;
        display: flex;
        flex-direction: column;
        gap: 8px;
        min-height: 60px;
    }
    /* ── Patient Slots ── */
    .patient-slot {
        background: #fff;
        border-radius: 12px;
        padding: 12px;
        border: 1px solid rgba(226,232,240,0.8);
        cursor: pointer;
        transition: all .25s ease;
        border-inline-start: 4px solid var(--sc-teal-500);
        animation: slotSlideIn 0.4s ease forwards;
        opacity: 0;
        transform: translateX(10px);
    }
        .patient-slot:hover {
            transform: translateX(-4px) translateY(-2px);
            box-shadow: 0 8px 20px rgba(10, 77, 79, 0.12);
            border-color: var(--sc-teal-500);
```

```
        }
    @@keyframes slotSlideIn {
        to {
            opacity: 1;
            transform: translateX(0);
        }
    }
    .slot-time {
        font-size: .75rem;
        font-weight: 800;
        color: var(--sc-teal-600);
        background: var(--brand-light);
        padding: 3px 10px;
        border-radius: 8px;
        display: inline-block;
        margin-bottom: 6px;
    }
    .slot-name {
        font-weight: 800;
        color: var(--sc-ink-900);
        font-size: .9rem;
        margin-bottom: 4px;
    }
    .slot-phone {
        font-size: .8rem;
        color: var(--sc-ink-600);
        display: flex;
        gap: 4px;
        font-family: 'Segoe UI', monospace;
        direction: ltr;
        justify-content: flex-end;
        align-items: center;
    }
    .slot-status {
        margin-top: 6px;
        font-size: .72rem;
        font-weight: 800;
        padding: 3px 10px;
        border-radius: 8px;
        display: inline-block;
    }
    .status-scheduled {
        background: #dbeafe;
        color: #1e40af;
    }
    .status-confirmed {
        background: #cffafe;
        color: #0e7490;
    }
    .status-checkedin {
        background: #d1fae5;
        color: #047857;
    }
    .status-inprogress {
        background: #fef3c7;
```

```
        color: #92400e;
    }
    .status-completed {
        background: #e2e8f0;
        color: #334155;
    }
    .status-cancelled {
        background: #fee2e2;
        color: #b91c1c;
        text-decoration: line-through;
        opacity: 0.7;
    }
    .status-noshow {
        background: #fef2f2;
        color: #991b1b;
    }
    .empty-day {
        text-align: center;
        color: #94a3b8;
        padding: 24px 0;
        font-size: .85rem;
        font-weight: 600;
    }
        .empty-day i {
            display: block;
            font-size: 1.5rem;
            margin-bottom: 8px;
            color: var(--sc-sky-100);
        }
    /* ── Loading Skeleton ── */
    .skeleton-day {
        background: rgba(255,255,255,.6);
        border-radius: 16px;
        border: 1px solid rgba(226,232,240,0.5);
        min-height: 320px;
        overflow: hidden;
        animation: skeletonPulse 1.5s infinite;
    }
    @@keyframes skeletonPulse {
        0%, 100% {
            opacity: 1;
        }
        50% {
            opacity: 0.6;
        }
    }
    .skeleton-header {
        height: 60px;
        background: linear-gradient(135deg, var(--sc-sky-50), transparent);
        border-bottom: 1px solid var(--sc-sky-100);
    }
    .skeleton-slots {
        padding: 10px;
        display: flex;
```

```
        flex-direction: column;
        gap: 8px;
    }
    .skeleton-slot {
        height: 80px;
        background: var(--sc-sky-50);
        border-radius: 12px;
    }
    /* ── Modals ── */
    .custom-modal {
        display: none;
        position: fixed;
        inset: 0;
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }
        .custom-modal.show {
            display: flex;
        }
    .modal-overlay {
        position: absolute;
        inset: 0;
        background: rgba(15,23,42,0.45);
        backdrop-filter: blur(4px);
    }
    .modal-box {
        position: relative;
        z-index: 1;
        width: 90%;
        max-width: 480px;
        background: #fff;
        border-radius: 20px;
        box-shadow: 0 25px 60px rgba(0,0,0,0.2);
        animation: modalSlide 0.25s ease;
        overflow: hidden;
    }
    @@keyframes modalSlide {
        from {
            opacity: 0;
            transform: translateY(20px) scale(0.98);
        }
        to {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
    }
    .modal-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 18px 24px;
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
    }
```

```
        .modal-head h5 {
            margin: 0;
            font-weight: 800;
            font-size: 1.1rem;
        }
    .modal-body {
        padding: 24px;
    }
    .modal-foot {
        padding: 0 24px 24px;
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        flex-wrap: wrap;
    }
    .form-label-custom {
        display: block;
        font-size: 0.82rem;
        font-weight: 700;
        color: #475569;
        margin-bottom: 6px;
    }
    .form-control-custom {
        width: 100%;
        padding: 10px 14px;
        border: 1.5px solid var(--sc-sky-100);
        border-radius: 10px;
        font-size: 0.95rem;
        color: var(--sc-ink-900);
        background: #fff;
        transition: all 0.2s;
        outline: none;
    }
        .form-control-custom:focus {
            border-color: var(--sc-teal-500);
            box-shadow: 0 0 0 3px rgba(20,184,166,0.1);
        }
    .btn-modal {
        padding: 10px 22px;
        border-radius: 10px;
        font-weight: 700;
        font-size: 0.9rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s;
    }
    .btn-modal-cancel {
        background: #f1f5f9;
        color: #475569;
    }
        .btn-modal-cancel:hover {
            background: #e2e8f0;
        }
```

```
    .btn-modal-save {
```

```
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
    }
        .btn-modal-save:hover {
            transform: translateY(-1px);
            box-shadow: 0 6px 16px rgba(13,148,136,0.3);
        }
    .btn-modal-danger {
        background: #fee2e2;
        color: #b91c1c;
    }
        .btn-modal-danger:hover {
            background: #fecaca;
        }
    .btn-modal-info {
        background: #dbeafe;
        color: #1e40af;
    }
        .btn-modal-info:hover {
            background: #bfdbfe;
        }
    /* ── Search Results Dropdown ── */
    .search-results-box {
        border: 1.5px solid var(--sc-sky-100);
        border-radius: 12px;
        margin-top: 6px;
        max-height: 180px;
        overflow-y: auto;
        display: none;
        background: #fff;
        box-shadow: 0 10px 30px rgba(0,0,0,0.08);
        animation: dropdownFade 0.2s ease;
    }
    @@keyframes dropdownFade {
        from {
            opacity: 0;
            transform: translateY(-5px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .search-result-item {
        padding: 10px 14px;
        cursor: pointer;
        border-bottom: 1px solid var(--sc-sky-100);
        transition: all 0.15s ease;
        font-size: 0.9rem;
        font-weight: 600;
        color: var(--sc-ink-900);
    }
        .search-result-item:hover {
```

```
            background: var(--brand-light);
            color: var(--brand-dark);
        }
        .search-result-item:last-child {
            border-bottom: none;
        }
    /* ── Responsive ── */
    @@media (max-width: 1200px) {
        .week-grid {
            grid-template-columns: repeat(4, 1fr);
        }
        .stats-bar {
            grid-template-columns: repeat(2, 1fr);
        }
    }
    @@media (max-width: 768px) {
        .week-grid {
            grid-template-columns: repeat(2, 1fr);
        }
        .stats-bar {
            grid-template-columns: repeat(2, 1fr);
        }
        .calendar-hero {
            flex-direction: column;
            text-align: center;
        }
            .calendar-hero .hero-content {
                flex-direction: column;
            }
        .calendar-header {
            justify-content: center;
        }
    }
    @@media (max-width: 480px) {
        .week-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
<!-- 📁 Hero -->
<div class="calendar-hero">
    <div class="hero-content">
        <div class="hero-icon"><i class="bi bi-calendar2-week-fill"></i></div>
        <div>
            <h1>المواعيدجدول</h1>
            <p>أسبوعي[االمرضىمواعيدوإدارةوحجزعرض</p>
        </div>
    </div>
    <button class="btn-hero-new" onclick="openCreateModal()">
        <i class="bi bi-plus-lg"></i> جديدموعد
    </button>
</div>
<!-- 📁 Stats -->
```

```
<div class="stats-bar" id="statsBar">
    <div class="stat-card-glass">
        <div class="stat-icon-box teal"><i class="bi
bi-calendar-check"></i></div>
        <div>
            <span class="stat-value-lg" id="statTotalWeek">0</span>
            <span class="stat-label-lg d-block">الأسبوعمواعيد</span>
        </div>
    </div>
    <div class="stat-card-glass">
        <div class="stat-icon-box green"><i class="bi
bi-check-circle"></i></div>
        <div>
            <span class="stat-value-lg" id="statConfirmed">0</span>
            <span class="stat-label-lg d-block">مؤكدة</span>
        </div>
    </div>
    <div class="stat-card-glass">
        <div class="stat-icon-box orange"><i class="bi bi-clock"></i></div>
        <div>
            <span class="stat-value-lg" id="statPending">0</span>
            <span class="stat-label-lg d-block">لنتظارقيد</span>
        </div>
    </div>
    <div class="stat-card-glass">
        <div class="stat-icon-box blue"><i class="bi bi-calendar-day"></i></div>
        <div>
            <span class="stat-value-lg" id="statToday">0</span>
            <span class="stat-label-lg d-block">اليوممواعيد</span>
        </div>
    </div>
</div>
<!-- 📁 Calendar -->
<div class="calendar-shell">
    <div class="calendar-header">
        <div class="cal-nav-group">
            <button class="cal-nav" onclick="shiftWeek(-1)" title=" الأسبوع
السابق"><i class="bi bi-chevron-right"></i></button>
            <button class="cal-today-btn" onclick="goToToday()">اليوم</button>
            <button class="cal-nav" onclick="shiftWeek(1)" title=" الأسبوع
الًقادم"><i class="bi bi-chevron-left"></i></button>
        </div>
        <h3 class="cal-title" id="weekRange">التحميلجاري...</h3>
    </div>
    <div class="week-grid" id="weekGrid">
        <!-- Loading Skeleton -->
        @for (int i = 0; i < 7; i++)
        {
            <div class="skeleton-day">
                <div class="skeleton-header"></div>
                <div class="skeleton-slots">
                    <div class="skeleton-slot"></div>
                    <div class="skeleton-slot"></div>
                </div>
            </div>
        }
    </div>
</div>
<!-- 📁 Create Modal -->
<div class="custom-modal" id="apptModal">
    <div class="modal-overlay" onclick="closeCreateModal()"></div>
    <div class="modal-box">
```

```
        <div class="modal-head">
            <h5><i class="bi bi-calendar-plus me-2"></i> جديدموعد</h5>
            <button type="button" class="btn-close btn-close-white"
onclick="closeCreateModal()"></button>
        </div>
        <div class="modal-body">
            <div class="mb-3">
                <label class="form-label-custom">المريض <span class="text-
danger">*</span></label>
                <input type="text" class="form-control-custom"
id="apptPatientSearch" placeholder="الموبايلرقمأوالمريضاسماكتب..."
autocomplete="off" />
                <input type="hidden" id="apptPatientId" />
                <div class="search-results-box" id="apptPatientResults"></div>
            </div>
            <div class="row g-3 mb-3">
                <div class="col-6">
                    <label class="form-label-custom">التاريخ <span class="text-
danger">*</span></label>
                    <input type="date" class="form-control-custom" id="apptDate"
/>
                </div>
                <div class="col-6">
                    <label class="form-label-custom">الوقت <span class="text-
danger">*</span></label>
                    <input type="time" class="form-control-custom" id="apptTime"
/>
                </div>
            </div>
            <div class="mb-3">
                <label class="form-label-custom">المدة</label>
                <select class="form-control-custom" id="apptDuration">
                    <option value="15">15 دقيًقة</option>
                    <option value="30" selected>30 دقيًقة</option>
                    <option value="45">45 دقيًقة</option>
                    <option value="60">ساعة</option>
                    <option value="90">ونصساعة</option>
                </select>
            </div>
            <div class="mb-3">
                <label class="form-label-custom">المالحظات / السبب</label>
                <textarea class="form-control-custom" id="apptReason" rows="2"
placeholder="متابعةتاج،تركيب :مثال..."></textarea>
            </div>
            <div id="apptConflictWarning" class="text-danger small"
style="display:none;">
                <i class="bi bi-exclamation-triangle-fill"></i>  آخرموعديوجد
التوقيتهذامعمتعارض.
            </div>
        </div>
        <div class="modal-foot">
            <button type="button" class="btn-modal btn-modal-cancel"
onclick="closeCreateModal()">إلغاء</button>
            <button type="button" class="btn-modal btn-modal-save"
onclick="saveAppointment()">
                <i class="bi bi-check-lg"></i> الموعدحفظ
            </button>
        </div>
    </div>
</div>
<!-- 📁 Details Modal -->
<div class="custom-modal" id="detailsModal">
    <div class="modal-overlay" onclick="closeDetailsModal()"></div>
```

```
    <div class="modal-box">
        <div class="modal-head">
            <h5><i class="bi bi-calendar-check me-2"></i> الموعدتفاصيل</h5>
            <button type="button" class="btn-close btn-close-white"
onclick="closeDetailsModal()"></button>
        </div>
        <div class="modal-body" id="detailsBody"></div>
        <div class="modal-foot" id="detailsActions"></div>
    </div>
</div>
@section Scripts {
    <script>
        let currentWeekStart = getStartOfWeek(new Date());
        let selectedAppointmentId = null;
        function getStartOfWeek(date) {
            const d = new Date(date);
            const day = d.getDay();
            d.setDate(d.getDate() - day);
            d.setHours(0, 0, 0, 0);
            return d;
        }
        function formatDateOnly(date) {
            return date.toISOString().split('T')[0];
        }
        function goToToday() {
            currentWeekStart = getStartOfWeek(new Date());
            loadWeek();
        }
        function shiftWeek(dir) {
            currentWeekStart.setDate(currentWeekStart.getDate() + (dir * 7));
            loadWeek();
        }
        const dayNames = [' ,'الخميس' ,'الأربعاء' ,'الثالثاء' ,'الإثنين' ,'الأحد
السبت' ,'الجمعة''];
        async function loadWeek() {
            const start = new Date(currentWeekStart);
            const end = new Date(currentWeekStart);
            end.setDate(end.getDate() + 7);
            document.getElementById('weekRange').textContent =
                `${start.toLocaleDateString('ar-EG', { day: 'numeric', month:
'short' })} — ${new Date(end - 1).toLocaleDateString('ar-EG', { day: 'numeric',
month: 'short' })}`;
```

```
            let events = [];
            try {
                const res = await fetch(`/Appointments/GetEvents?start=$
{start.toISOString()}&end=${end.toISOString()}`);
                const result = await res.json();
                if (result.success) events = result.data;
            } catch (err) {
                console.error('Failed to load appointments', err);
            }
            renderWeek(start, events);
            updateStats(events);
        }
```

```
        function updateStats(events) {
            const today = formatDateOnly(new Date());
            const totalWeek = events.length;
            const confirmed = events.filter(e => e.status === 2).length;
            const pending = events.filter(e => e.status === 1).length;
            const todayCount = events.filter(e => formatDateOnly(new
Date(e.start)) === today).length;
            animateCount(document.getElementById('statTotalWeek'), totalWeek);
            animateCount(document.getElementById('statConfirmed'), confirmed);
            animateCount(document.getElementById('statPending'), pending);
            animateCount(document.getElementById('statToday'), todayCount);
        }
        function animateCount(el, target) {
            const start = parseInt(el.textContent) || 0;
            const diff = target - start;
            if (diff === 0) return;
            const duration = 600;
            const startTime = performance.now();
            function step(currentTime) {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const easeProgress = 1 - Math.pow(1 - progress, 3); // ease-out
cubic
                el.textContent = Math.floor(start + (diff * easeProgress));
                if (progress < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        }
        function renderWeek(weekStart, events) {
            const grid = document.getElementById('weekGrid');
            const today = formatDateOnly(new Date());
            grid.innerHTML = '';
            for (let i = 0; i < 7; i++) {
                const day = new Date(weekStart);
                day.setDate(day.getDate() + i);
                const dayKey = formatDateOnly(day);
                const dayEvents = events.filter(e => formatDateOnly(new
Date(e.start)) === dayKey)
                    .sort((a, b) => new Date(a.start) - new Date(b.start));
                const col = document.createElement('div');
                col.className = 'day-column' + (dayKey === today ? ' is-today' :
'');
```

```
                const isToday = dayKey === today;
```

```
                col.innerHTML = `
                    <div class="day-header">
                        ${isToday ? '<div class="today-indicator"></div>' : ''}
                        <div class="day-name">${dayNames[day.getDay()]}</div>
                        <div class="day-number">${day.getDate()}</div>
                    </div>
                    <div class="day-slots">
                        ${dayEvents.length === 0 ? '<div class="empty-day"><i
class="bi bi-calendar-x"></i>مواعيدتوجدال</div>' : ''}
                        ${dayEvents.map((e, idx) => `
                            <div class="patient-slot" style="animation-delay: $
{idx * 0.05}s" onclick="openDetailsModal(${e.id})">
```

```
                                <div class="slot-time">${new
Date(e.start).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-
digit' })}</div>
                                <div class="slot-name">${e.patientName}</div>
                                <div class="slot-phone"><i class="bi bi-
telephone-fill"></i> ${e.patientMobile}</div>
                                <span class="slot-status $
{e.statusColorClass}">${e.statusText}</span>
                            </div>
                        `).join('')}
                    </div>`;
                grid.appendChild(col);
            }
        }
        // ── Create Modal ──
        function openCreateModal() {
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);
            document.getElementById('apptDate').value =
formatDateOnly(tomorrow);
            document.getElementById('apptTime').value = '09:00';
            document.getElementById('apptPatientId').value = '';
            document.getElementById('apptPatientSearch').value = '';
            document.getElementById('apptReason').value = '';
            document.getElementById('apptConflictWarning').style.display =
'none';
            document.getElementById('apptModal').classList.add('show');
            document.body.style.overflow = 'hidden';
        }
        function closeCreateModal() {
            document.getElementById('apptModal').classList.remove('show');
            document.body.style.overflow = '';
        }
        // ── Patient Search ──
        let patientSearchTimeout;
        document.addEventListener('DOMContentLoaded', () => {
            const searchInput = document.getElementById('apptPatientSearch');
            if (!searchInput) return;
            searchInput.addEventListener('input', () => {
                clearTimeout(patientSearchTimeout);
                const keyword = searchInput.value.trim();
                document.getElementById('apptPatientId').value = '';
                if (keyword.length < 2) {
                    document.getElementById('apptPatientResults').style.display
= 'none';
                    return;
                }
                patientSearchTimeout = setTimeout(() => searchPatients(keyword),
300);
            });
            loadWeek();
        });
        async function searchPatients(keyword) {
            try {
                const res = await fetch(`/Patients/Search?term=$
{encodeURIComponent(keyword)}`);
                const list = await res.json();
                const box = document.getElementById('apptPatientResults');
                if (!list.length) {
                    box.style.display = 'none';
```

```
                    return;
                }
                box.innerHTML = '';
                list.forEach(function (p) {
                    const item = document.createElement('div');
                    item.className = 'search-result-item';
                    item.dataset.id = p.id;
                    item.dataset.name = p.fullNameAr || '';
                    item.textContent = (p.fullNameAr || '') + ' — ' + (p.phone
|| '');
                    item.addEventListener('mousedown', function () {
                        selectPatient(parseInt(item.dataset.id),
item.dataset.name);
                    });
                    box.appendChild(item);
                });
                box.style.display = 'block';
            } catch (err) {
                console.error('Patient search failed', err);
            }
        }
        function selectPatient(id, name) {
            document.getElementById('apptPatientId').value = id;
            document.getElementById('apptPatientSearch').value = name;
            document.getElementById('apptPatientResults').style.display =
'none';
        }
        async function saveAppointment() {
            const patientId =
parseInt(document.getElementById('apptPatientId').value);
            const date = document.getElementById('apptDate').value;
            const time = document.getElementById('apptTime').value;
            const duration =
parseInt(document.getElementById('apptDuration').value);
            const reason = document.getElementById('apptReason').value;
            if (!patientId) {
                Swal.fire({ title: 'مطلوب', text: 'البحثنتائجمنالمريضاختر
icon: 'warning', confirmButtonColor: '#14b8a6' });
                return;
            }
            if (!date || !time) {
                Swal.fire({ title: 'مطلوب', text: 'والوقتالتاريخاختر', icon:
'warning', confirmButtonColor: '#14b8a6' });
                return;
            }
            const appointmentDate = new Date(`${date}T${time}`);
            const data = {
                PatientId: patientId,
                AppointmentDate: appointmentDate.toISOString(),
                DurationMinutes: duration,
                Reason: reason
            };
            try {
                const res = await fetch('/Appointments/Create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
```

```
                const result = await res.json();
                if (result.success) {
                    closeCreateModal();
                    Swal.fire({ title: 'تم!', text: result.message, icon:
'success', confirmButtonColor: '#14b8a6', timer: 1800, showConfirmButton:
false });
                    loadWeek();
                } else {
                    Swal.fire({ title: 'الحفظتعذر', text: result.message, icon:
'error', confirmButtonColor: '#14b8a6' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'بالسيرفرلتصالفيمشكلة',
icon: 'error', confirmButtonColor: '#14b8a6' });
            }
        }
        // ── Details Modal ──
        async function openDetailsModal(id) {
            selectedAppointmentId = id;
            const res = await fetch(`/Appointments/GetById?id=${id}`);
            const result = await res.json();
            if (!result.success) {
                Swal.fire({ title: 'خطأ', text: result.message, icon:
'error' });
                return;
            }
            const a = result.data;
            document.getElementById('detailsBody').innerHTML = `
                <div class="mb-2"><strong style="font-size:1.1rem; color:var(--
sc-ink-900);">${a.patientName}</strong> <span class="slot-status $
{a.statusColorClass}">${a.statusText}</span></div>
                <div class="mb-2" style="color:var(--sc-ink-600); font-
size:.85rem;"><i class="bi bi-telephone"></i> ${a.patientMobile}</div>
                <div class="mb-2" style="color:var(--sc-ink-600); font-
size:.85rem;"><i class="bi bi-clock"></i> ${new
Date(a.start).toLocaleString('ar-EG')} — ${a.durationMinutes} دقيًقة</div>
                ${a.reason ? `<div class="mb-2" style="color:var(--sc-
ink-900);"><strong>السبب:</strong> ${a.reason}</div>` : ''}
                ${a.notes ? `<div class="mb-2" style="color:var(--sc-
ink-600);"><strong>مالحظات:</strong> ${a.notes}</div>` : ''}
            `;
```

```
            const actions = [];
            if (a.status === 1) {
                actions.push(`<button class="btn-modal btn-modal-info"
onclick="doAction('Confirm')">تأكيد</button>`);
            }
            if (a.status === 1 || a.status === 2) {
                actions.push(`<button class="btn-modal btn-modal-save"
onclick="doAction('CheckIn')">حضورتسجيل</button>`);
                actions.push(`<button class="btn-modal btn-modal-danger"
onclick="doAction('Cancel')">إلغاء</button>`);
            }
            if (a.status === 3 || a.status === 4) {
                actions.push(`<button class="btn-modal btn-modal-save"
onclick="convertToVisit()">الزيارةبدء</button>`);
            }
            actions.push(`<button class="btn-modal btn-modal-cancel"
onclick="closeDetailsModal()">إغالق</button>`);
            document.getElementById('detailsActions').innerHTML =
actions.join('');
```

```
            document.getElementById('detailsModal').classList.add('show');
            document.body.style.overflow = 'hidden';
        }
        function closeDetailsModal() {
            document.getElementById('detailsModal').classList.remove('show');
            document.body.style.overflow = '';
        }
        async function doAction(action) {
            try {
                const res = await fetch(`/Appointments/${action}?id=$
{selectedAppointmentId}`, { method: 'POST' });
                const result = await res.json();
                if (result.success) {
                    closeDetailsModal();
                    Swal.fire({ title: 'تم!', text: result.message, icon:
'success', confirmButtonColor: '#14b8a6', timer: 1500, showConfirmButton:
false });
                    loadWeek();
                } else {
                    Swal.fire({ title: 'التنفيذتعذر', text: result.message,
icon: 'error' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'بالسيرفرلتصالفيمشكلة',
icon: 'error' });
            }
        }
        async function convertToVisit() {
            try {
                const res = await fetch(`/Appointments/ConvertToVisit?id=$
{selectedAppointmentId}&category=1`, { method: 'POST' });
                const result = await res.json();
                if (result.success) {
                    window.location.href = `/Clinical/Session?patientId=$
{result.patientId}&visitId=${result.visitId}`;
                } else {
                    Swal.fire({ title: 'التنفيذتعذر', text: result.message,
icon: 'error' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'بالسيرفرلتصالفيمشكلة',
icon: 'error' });
            }
        }
        // ── Keyboard Shortcuts ──
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') {
                closeCreateModal();
                closeDetailsModal();
            }
        });
    </script>
}
```

```
// View / Clinical /
```

```
@using Dental.Models.Patient
@using Dental.Modules.Clinical.ViewModels
@using Dental.Modules.Patients.Enums
```

```
@model SessionViewModel
@{
    ViewData["Title"] = $"سيشن — {Model.Patient.FullNameAr}";
    var initials = string.Join("", Model.Patient.FullNameAr.Split('
').Take(2).Select(x => x[0]));
    var sessionStatusClass = Model.Visit.Status switch
    {
        VisitStatus.Completed => "success",
        VisitStatus.Cancelled => "danger",
        _ => "warning"
    };
    var sessionStatusText = Model.Visit.Status switch
    {
        VisitStatus.Completed => "مكتمل",
        VisitStatus.Cancelled => "ملغي",
        _ => "التنفيذقيد"
    };
}
<style>
    :root {
        --sc-teal-900: #0a4d4f;
        --sc-teal-700: #0d7377;
        --sc-teal-600: #0f9b8e;
        --sc-teal-500: #14b8a6;
        --sc-teal-100: #c7f2ea;
        --sc-sky-50: #eafbfc;
        --sc-sky-100: #dcf5f7;
        --sc-coral-300: #f6c6bc;
        --sc-ink-900: #0b2e30;
        --sc-ink-600: #4b6b6c;
        --brand: #0d9488;
        --brand-dark: #0f766e;
        --brand-light: #f0fdfa;
        --radius-lg: 18px;
        --radius-md: 12px;
        --shadow-soft: 0 1px 3px rgba(15, 23, 42, 0.06);
        --shadow-hover: 0 10px 26px rgba(10, 77, 79, 0.12);
        --shadow-card: 0 20px 50px rgba(10, 77, 79, .08);
    }
    body {
        background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
transparent 26%), var(--sc-sky-50);
    }
    /* ── Hero ── */
    .session-hero {
        background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
        border-radius: var(--radius-lg);
        padding: 24px 28px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
        margin-bottom: 20px;
        flex-wrap: wrap;
        gap: 16px;
        position: relative;
        overflow: hidden;
    }
```

```
        .session-hero::after {
            content: '';
            position: absolute;
            inset: 0;
            background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
            pointer-events: none;
        }
        .session-hero .hero-content {
            display: flex;
            align-items: center;
            gap: 18px;
            position: relative;
            z-index: 1;
        }
        .session-hero .hero-avatar {
            width: 64px;
            height: 64px;
            border-radius: 16px;
            background: rgba(255,255,255,.18);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.6rem;
            font-weight: 800;
            flex-shrink: 0;
            backdrop-filter: blur(6px);
            border: 2px solid rgba(255,255,255,.2);
        }
        .session-hero h1 {
            font-size: 1.3rem;
            font-weight: 800;
            margin: 0;
            color: #fff;
        }
        .session-hero .hero-meta {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-top: 8px;
            align-items: center;
        }
    .hero-meta-tag {
        display: flex;
        align-items: center;
        gap: 6px;
        background: rgba(255,255,255,.15);
        color: #fff;
        padding: 4px 12px;
        border-radius: 20px;
        font-size: 0.78rem;
        font-weight: 600;
        backdrop-filter: blur(4px);
        border: 1px solid rgba(255,255,255,.1);
    }
```

```
    .session-status-badge {
```

```
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 5px 14px;
        border-radius: 20px;
        font-size: 0.8rem;
        font-weight: 800;
        border: 2px solid rgba(255,255,255,.3);
        backdrop-filter: blur(6px);
    }
        .session-status-badge.success {
            background: rgba(16, 185, 129, 0.25);
            color: #d1fae5;
        }
        .session-status-badge.warning {
            background: rgba(245, 158, 11, 0.25);
            color: #fef3c7;
        }
        .session-status-badge.danger {
            background: rgba(239, 68, 68, 0.25);
            color: #fee2e2;
        }
    .hero-actions {
        display: flex;
        gap: 10px;
        position: relative;
        z-index: 1;
        flex-wrap: wrap;
    }
    .btn-hero {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.88rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s ease;
        text-decoration: none;
        white-space: nowrap;
    }
    .btn-hero-back {
        background: rgba(255,255,255,.9);
        color: var(--sc-teal-700);
        box-shadow: 0 4px 12px rgba(10, 40, 42, .15);
    }
        .btn-hero-back:hover {
            background: #fff;
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(10, 40, 42, .22);
            color: var(--sc-teal-700);
        }
    .btn-hero-print {
        background: rgba(255,255,255,.15);
```

```
        color: #fff;
        border: 1px solid rgba(255,255,255,.25);
        backdrop-filter: blur(4px);
    }
        .btn-hero-print:hover {
            background: rgba(255,255,255,.25);
            transform: translateY(-2px);
        }
    .btn-hero-end {
        background: linear-gradient(135deg, #ef4444, #dc2626);
        color: #fff;
        box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
    }
        .btn-hero-end:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(239, 68, 68, 0.4);
            color: #fff;
        }
    /* ── Stats ── */
    .stats-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 14px;
        margin-bottom: 20px;
    }
    .stat-card-glass {
        background: rgba(255,255,255,.78);
        backdrop-filter: blur(14px);
        border-radius: 16px;
        padding: 18px 20px;
        display: flex;
        align-items: center;
        gap: 14px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: var(--shadow-soft);
        transition: all .25s ease;
    }
        .stat-card-glass:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow-hover);
        }
    .stat-icon-box {
        width: 46px;
        height: 46px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.15rem;
        flex-shrink: 0;
    }
        .stat-icon-box.teal {
            background: #f0fdfa;
            color: #0f766e;
        }
```

```
        .stat-icon-box.blue {
            background: #eff6ff;
            color: #1d4ed8;
        }
        .stat-icon-box.green {
            background: #f0fdf4;
            color: #166534;
        }
        .stat-icon-box.red {
            background: #fef2f2;
            color: #dc2626;
        }
    .stat-value-lg {
        display: block;
        font-size: 1.4rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        line-height: 1.1;
    }
    .stat-label-lg {
        font-size: 0.78rem;
        color: var(--sc-ink-600);
        font-weight: 600;
    }
    /* ── Tabs ── */
    .tabs-container {
        background: rgba(255,255,255,.82);
        backdrop-filter: blur(16px);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow-card);
        overflow: hidden;
        border: 1px solid rgba(255,255,255,.6);
        margin-bottom: 20px;
    }
    .tabs-pill-bar {
        display: flex;
        gap: 6px;
        padding: 14px 18px;
        border-bottom: 1px solid var(--sc-sky-100);
        overflow-x: auto;
        flex-wrap: nowrap;
    }
    .tab-pill {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 18px;
        border-radius: 999px;
        border: none;
        background: transparent;
        color: var(--sc-ink-600);
        font-weight: 700;
        font-size: 0.85rem;
        cursor: pointer;
        white-space: nowrap;
        transition: all 0.2s ease;
    }
```

```
        .tab-pill:hover {
            background: #fff;
            color: var(--brand-dark);
        }
        .tab-pill.active {
            background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
            color: #fff;
            box-shadow: 0 4px 10px rgba(13,148,136,0.28);
        }
        .tab-pill i {
            font-size: 0.95rem;
        }
    .tab-panels {
        padding: 20px;
    }
    .tab-panel {
        display: none;
        animation: panelFadeIn 0.35s ease;
    }
        .tab-panel.active {
            display: block;
        }
    @@keyframes panelFadeIn {
        from {
            opacity: 0;
            transform: translateY(10px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    /* ── Overview: 3-Column Compact Grid ── */
    .overview-grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 16px;
        margin-bottom: 16px;
    }
    .overview-card {
        background: rgba(255,255,255,.9);
        border-radius: 16px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: 0 1px 3px rgba(0,0,0,0.04);
        overflow: hidden;
        transition: all 0.25s ease;
        animation: cardFadeIn 0.5s ease forwards;
        opacity: 0;
    }
        .overview-card:hover {
            box-shadow: 0 8px 24px rgba(10, 77, 79, 0.08);
            transform: translateY(-2px);
```

```
        }
        .overview-card:nth-child(1) {
            animation-delay: .04s;
        }
        .overview-card:nth-child(2) {
            animation-delay: .08s;
        }
        .overview-card:nth-child(3) {
            animation-delay: .12s;
        }
    @@keyframes cardFadeIn {
        from {
            opacity: 0;
            transform: translateY(12px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .overview-header {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 12px 16px;
        background: var(--sc-sky-50);
        border-bottom: 1px solid var(--sc-sky-100);
        font-weight: 800;
        font-size: 0.9rem;
        color: var(--sc-ink-900);
    }
        .overview-header i {
            color: var(--sc-teal-600);
            font-size: 1rem;
        }
    .overview-body {
        padding: 12px 16px;
    }
    .overview-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 8px 0;
        border-bottom: 1px solid var(--sc-sky-100);
        font-size: 0.88rem;
    }
        .overview-row:last-child {
            border-bottom: none;
        }
    .overview-label {
        color: var(--sc-ink-600);
        font-weight: 600;
        font-size: 0.82rem;
```

```
    }
    .overview-value {
        color: var(--sc-ink-900);
        font-weight: 700;
        font-size: 0.88rem;
        text-align: left;
    }
        .overview-value.danger {
            color: #dc2626;
            font-weight: 800;
        }
    .overview-badge {
        background: #fee2e2;
        color: #dc2626;
        padding: 2px 8px;
        border-radius: 6px;
        font-size: 0.75rem;
        font-weight: 700;
    }
    /* ── Bottom Section: Notes + Timeline side by side ── */
    .overview-bottom {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
    }
    .overview-card.full-width {
        grid-column: 1 / -1;
    }
    /* ── Timeline Compact ── */
    .timeline-compact {
        position: relative;
        padding-right: 20px;
    }
        .timeline-compact::before {
            content: '';
            position: absolute;
            right: 6px;
            top: 0;
            bottom: 0;
            width: 2px;
            background: var(--sc-sky-100);
        }
    .timeline-node {
        position: relative;
        padding-bottom: 14px;
        padding-right: 16px;
    }
        .timeline-node::before {
            content: '';
            position: absolute;
            right: -16px;
            top: 4px;
            width: 8px;
            height: 8px;
            border-radius: 50%;
```

```
            background: var(--sc-teal-500);
            border: 2px solid #fff;
            box-shadow: 0 0 0 2px var(--sc-teal-500);
        }
    .timeline-date {
        font-size: 0.72rem;
        color: #94a3b8;
        font-weight: 700;
        margin-bottom: 2px;
    }
    .timeline-title {
        font-weight: 800;
        color: var(--sc-ink-900);
        font-size: 0.88rem;
    }
    .timeline-desc {
        color: var(--sc-ink-600);
        font-size: 0.82rem;
        margin-top: 2px;
    }
    /* ── Placeholder ── */
    .placeholder-glass {
        background: rgba(255,255,255,.9);
        border-radius: 16px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: 0 1px 3px rgba(0,0,0,0.04);
        padding: 48px;
        text-align: center;
    }
    .placeholder-icon-glass {
        width: 72px;
        height: 72px;
        border-radius: 20px;
        background: var(--brand-light);
        color: var(--sc-teal-500);
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
        margin: 0 auto 20px;
    }
    .placeholder-glass h3 {
        font-size: 1.2rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        margin: 0 0 8px 0;
    }
    .placeholder-glass p {
        color: var(--sc-ink-600);
        font-size: 0.92rem;
        margin: 0 0 24px 0;
    }
    /* ── Responsive ── */
    @@media (max-width: 1200px) {
        .overview-grid {
            grid-template-columns: repeat(2, 1fr);
```

```
        }
        .overview-bottom {
            grid-template-columns: 1fr;
        }
        .stats-grid {
            grid-template-columns: repeat(2, 1fr);
        }
    }
    @@media (max-width: 768px) {
        .session-hero {
            flex-direction: column;
            text-align: center;
        }
            .session-hero .hero-content {
                flex-direction: column;
            }
        .hero-actions {
            width: 100%;
            justify-content: center;
        }
        .overview-grid {
            grid-template-columns: 1fr;
        }
        .stats-grid {
            grid-template-columns: repeat(2, 1fr);
        }
        .tabs-pill-bar {
            flex-wrap: wrap;
        }
    }
</style>
<div class="container-fluid" dir="rtl" style="padding: 12px;">
    <!-- 📁 Hero Header -->
    <div class="session-hero">
        <div class="hero-content">
            <div class="hero-avatar">@initials</div>
            <div>
                <h1>@Model.Patient.FullNameAr</h1>
                <div class="hero-meta">
                    <span class="session-status-badge @sessionStatusClass">
                        <i class="bi bi-circle-fill" style="font-
size:0.5rem;"></i> @sessionStatusText
                    </span>
                    <span class="hero-meta-tag"><i class="bi bi-hash"></i>
@Model.Patient.PatientCode</span>
                    <span class="hero-meta-tag"><i class="bi bi-telephone"></i>
@Model.Patient.Mobile</span>
                    <span class="hero-meta-tag"><i class="bi bi-droplet"></i>
@(Model.Patient.BloodGroup?.Name ?? "-")</span>
                    <span class="hero-meta-tag"><i class="bi bi-calendar"></i>
@Model.Visit.VisitDate.ToString("dd/MM/yyyy")</span>
                </div>
            </div>
        </div>
```

```
        <div class="hero-actions">
            <a asp-controller="Patients" asp-action="Details" asp-route-
id="@Model.Patient.Id" class="btn-hero btn-hero-back">
                <i class="bi bi-arrow-right"></i> المريضلملفرجوع
            </a>
            <button type="button" class="btn-hero btn-hero-print"
onclick="window.print()">
                <i class="bi bi-printer"></i> طباعة
            </button>
            @if (Model.Visit.Status != VisitStatus.Completed)
            {
                <button type="button" class="btn-hero btn-hero-end"
onclick="endSession()">
                    <i class="bi bi-check-circle"></i> السيشنإنهاء
                </button>
            }
        </div>
    </div>
    <!-- 📁 Stats Bar -->
    <div class="stats-grid">
        <div class="stat-card-glass">
            <div class="stat-icon-box teal"><i class="bi bi-receipt"></i></div>
            <div>
                <span class="stat-value-lg"
id="sumTotal">@Model.Visit.TotalAmount.ToString("N0")</span>
                <span class="stat-label-lg d-block">(م.ج) الإجمالي</span>
            </div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-icon-box blue"><i class="bi bi-percent"></i></div>
            <div>
                <span class="stat-value-lg"
id="sumDiscount">@Model.Visit.Discount.ToString("N0")</span>
                <span class="stat-label-lg d-block">الخصم</span>
            </div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-icon-box green"><i class="bi
bi-cash-stack"></i></div>
            <div>
                <span class="stat-value-lg"
id="sumPaid">@Model.Visit.PaidAmount.ToString("N0")</span>
                <span class="stat-label-lg d-block">المدفوع</span>
            </div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-icon-box red"><i class="bi
bi-credit-card"></i></div>
            <div>
                <span class="stat-value-lg"
id="sumRemaining">@Model.Visit.Remaining.ToString("N0")</span>
                <span class="stat-label-lg d-block">المتبًقي</span>
            </div>
        </div>
    </div>
    <!-- 📁 Tabs -->
    <div class="tabs-container">
        <div class="tabs-pill-bar">
            <button class="tab-pill active" onclick="switchTab('overview',
this)">
                <i class="bi bi-clipboard-pulse"></i> عامةنظرة
            </button>
```

```
            <button class="tab-pill" onclick="switchTab('dentalchart', this)">
                <i class="bi bi-grid-3x3-gap"></i> Dental Chart
            </button>
            <button class="tab-pill" onclick="switchTab('diagnosis', this)">
                <i class="bi bi-activity"></i> التشخيص
            </button>
            <button class="tab-pill" onclick="switchTab('xrays', this)">
                <i class="bi bi-image"></i> الأشعة
            </button>
            <button class="tab-pill" onclick="switchTab('payments', this)">
                <i class="bi bi-credit-card"></i> المدفوعات
            </button>
            <button class="tab-pill" onclick="switchTab('prescription', this)">
                <i class="bi bi-file-medical"></i> الروشتة
            </button>
        </div>
        <div class="tab-panels">
            <!-- 📁 Tab: Overview (3-Column Compact) -->
            <div class="tab-panel active" id="overview">
                <!-- Top Row: 3 Cards -->
                <div class="overview-grid">
                    <!-- Card 1: Personal -->
                    <div class="overview-card">
                        <div class="overview-header"><i class="bi bi-person-
vcard"></i> الشخصيةالبيانات</div>
                        <div class="overview-body">
                            <div class="overview-row">
                                <span class="overview-label">لسم</span>
                                <span class="overview-
value">@Model.Patient.FullNameAr</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">العمر</span>
                                <span class="overview-value">@Model.Patient.Age
سنة</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">النوع</span>
                                <span class="overview-
value">@(Model.Patient.Gender?.NameAr ?? "-")</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">الدمفصيلة</span>
                                <span class="overview-value"><span
class="overview-badge">@(Model.Patient.BloodGroup?.Name ?? "-")</span></span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">الملفنوع</span>
                                <span class="overview-
value">@(Model.Patient.PatientType == PatientType.Orthodontics ? " : "تًقويم
عام"")</span>
```

```
                            </div>
```

```
                        </div>
                    </div>
                    <!-- Card 2: Contact -->
                    <div class="overview-card">
                        <div class="overview-header"><i class="bi bi-telephone-
plus"></i> التواصلبيانات</div>
                        <div class="overview-body">
                            <div class="overview-row">
                                <span class="overview-label">الموبايل</span>
```

```
                                <span class="overview-value"
dir="ltr">@Model.Patient.Mobile</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">التليفون</span>
                                <span class="overview-value"
dir="ltr">@(Model.Patient.Phone ?? "-")</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">البريد</span>
                                <span class="overview-
value">@(Model.Patient.Email ?? "-")</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">العنوان</span>
                                <span class="overview-
value">@(Model.Patient.Address ?? "-")</span>
                            </div>
                        </div>
                    </div>
                    <!-- Card 3: Medical -->
                    <div class="overview-card">
                        <div class="overview-header"><i class="bi bi-heart-
pulse"></i> الطبيالتاريخ</div>
                        <div class="overview-body">
                            <div class="overview-row">
                                <span class="overview-label">الحساسية</span>
                                <span class="overview-value
@(string.IsNullOrEmpty(Model.Patient.Allergies) ? "" : "danger")">
                                    @(Model.Patient.Allergies ?? "يوجدال")
                                </span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">مزمنةأمراض</span>
                                <span class="overview-
value">@(Model.Patient.ChronicDiseases ?? "يوجدال")</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">الأدوية</span>
                                <span class="overview-
value">@(Model.Patient.CurrentMedications ?? "يوجدال")</span>
                            </div>
                            <div class="overview-row">
                                <span class="overview-label">التدخين</span>
                                <span class="overview-
value">@(Model.Patient.SmokingStatus ?? "-")</span>
                            </div>
                        </div>
                    </div>
                </div>
                <!-- Bottom Row: Notes + Timeline (side by side) -->
                <div class="overview-bottom">
                    <div class="overview-card">
                        <div class="overview-header"><i class="bi bi-journal-
text"></i> السيشنمالحظات</div>
                        <div class="overview-body">
                            <p style="color: #475569; line-height: 1.6; font-
size: 0.88rem; margin: 0;">
```



```
                    </div>
                    <div class="overview-body">
                        <div class="timeline-compact">
                            <div class="timeline-node">
                                <div class="timeline-
date">@Model.Visit.VisitDate.ToString("dd MMM yyyy HH:mm")</div>
                                <div class="timeline-title">السيشنبدء</div>
                                <div class="timeline-desc"> السيشنفتحتم
للمريض</div>
                            </div>
                            @if (Model.Visit.VisitDate != default)
                            {
                                <div class="timeline-node">
                                    <div class="timeline-
date">@Model.Visit.VisitDate.ToString("dd MMM yyyy HH:mm")</div>
                                    <div class="timeline-title"> تسجيل
السيشن</div>
                                    <div class="timeline-desc"> السيشنإنشاءتم
النظامفي</div>
                                </div>
                            }
                        </div>
                    </div>
                </div>
            </div>
            <!-- 📁 Other Tabs -->
            <div class="tab-panel" id="dentalchart">
                @await Html.PartialAsync("_DentalChartInteractive", Model)
            </div>
            <div class="tab-panel" id="diagnosis">
                @await Html.PartialAsync("_DynamicDiagnosis", Model)
            </div>
            <div class="tab-panel" id="xrays">
                @await Html.PartialAsync("_SessionXRays", Model)
            </div>
            <div class="tab-panel" id="payments">
                @await Html.PartialAsync("_ProfessionalPayments", Model)
            </div>
            <div class="tab-panel" id="prescription">
                @await Html.PartialAsync("_SessionPrescription", Model)
            </div>
        </div>
    </div>
</div>
@section Scripts {
    <script>
        function switchTab(tabId, btn) {
            document.querySelectorAll('.tab-pill').forEach(b =>
b.classList.remove('active'));
            document.querySelectorAll('.tab-panel').forEach(c =>
c.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById(tabId).classList.add('active');
        }
        function endSession() {
            Swal.fire({
                title: 'السيشن؟إنهاء',
                text: 'السيشن؟هذاوإغالقإنهاءتريدهل',
```

```
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                cancelButtonColor: '#64748b',
                confirmButtonText: 'إنهاءنعم،',
                cancelButtonText: 'إلغاء',
                reverseButtons: true
            }).then((result) => {
                if (result.isConfirmed) {
                    // TODO: Submit to controller
                    window.location.reload();
                }
            });
        }
    </script>
}
// View / Home /
```

```
@{
    ViewData["Title"] = "index";
}
<h1>Test</h1>
```

```
// View / Patients / create.cshtml
```

```
@using Dental.Modules.Patients.DTOs
@model Dental.Modules.Patients.DTOs.CreatePatientDto
@{
    ViewData["Title"] = "جديدمريضإضافة";
    var genderList = ViewBag.Genders as SelectList ?? new SelectList(new
List<SelectListItem>());
    var bloodGroupList = ViewBag.BloodGroups as SelectList ?? new SelectList(new
List<SelectListItem>());
}
@section Styles {
    <style>
        :root {
            --sc-teal-900: #0a4d4f;
            --sc-teal-700: #0d7377;
            --sc-teal-600: #0f9b8e;
            --sc-teal-500: #14b8a6;
            --sc-teal-100: #c7f2ea;
            --sc-sky-50: #eafbfc;
            --sc-sky-100: #dcf5f7;
            --sc-coral-300: #f6c6bc;
            --sc-ink-900: #0b2e30;
            --sc-ink-600: #4b6b6c;
            --sc-radius-lg: 18px;
            --sc-radius-md: 12px;
        }
        body {
            background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
transparent 26%), var(--sc-sky-50);
        }
        /* ── Hero ── */
        .create-hero {
            background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
```

```
teal-500));
            border-radius: var(--sc-radius-lg);
            padding: 22px 28px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
            margin-bottom: 20px;
            flex-wrap: wrap;
            gap: 16px;
            position: relative;
            overflow: hidden;
        }
            .create-hero::after {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
                pointer-events: none;
            }
            .create-hero .hero-title {
                display: flex;
                align-items: center;
                gap: 16px;
                position: relative;
                z-index: 1;
            }
            .create-hero .hero-icon {
                width: 52px;
                height: 52px;
                background: rgba(255,255,255,.18);
                color: #fff;
                border-radius: 14px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 1.4rem;
                flex-shrink: 0;
                backdrop-filter: blur(6px);
            }
            .create-hero h1 {
                font-size: 1.25rem;
                font-weight: 800;
                margin: 0;
                color: #fff;
            }
            .create-hero p {
                font-size: 0.85rem;
                margin: 2px 0 0;
                color: rgba(255,255,255,.9);
                font-weight: 600;
            }
            .create-hero .hero-actions {
                display: flex;
                gap: 10px;
                position: relative;
                z-index: 1;
```

```
                flex-wrap: wrap;
            }
        .btn-hero {
            border: none;
            padding: 11px 22px;
            border-radius: 12px;
            font-weight: 800;
            display: flex;
            align-items: center;
            gap: 8px;
            text-decoration: none;
            transition: transform .2s ease, box-shadow .2s ease;
            cursor: pointer;
            font-size: 0.88rem;
        }
        .btn-hero-light {
            background: #fff;
            color: var(--sc-teal-700);
        }
            .btn-hero-light:hover {
                transform: translateY(-2px);
                box-shadow: 0 8px 18px rgba(10,40,42,.24);
                color: var(--sc-teal-700);
            }
        .btn-hero-ghost {
            background: rgba(255,255,255,.14);
            color: #fff;
            border: 1px solid rgba(255,255,255,.4);
        }
            .btn-hero-ghost:hover {
                background: rgba(255,255,255,.24);
                color: #fff;
                transform: translateY(-2px);
            }
        /* ── Layout: sticky profile panel + single flowing document card ──
                   (deliberately NOT a grid of separate equal cards) */
        .create-layout {
            display: grid;
            grid-template-columns: 300px 1fr;
            gap: 20px;
            align-items: start;
        }
        @@media (max-width: 992px) {
            .create-layout {
                grid-template-columns: 1fr;
            }
        }
        /* ── Profile panel (left, sticky) ── */
        .profile-panel {
            position: sticky;
            top: 20px;
            background: rgba(255,255,255,.8);
            backdrop-filter: blur(16px);
            border: 1px solid rgba(255,255,255,.6);
            border-radius: var(--sc-radius-lg);
            padding: 26px 22px;
```

```
            box-shadow: 0 20px 50px rgba(10,77,79,.08);
            text-align: center;
        }
        .avatar-preview {
            width: 76px;
            height: 76px;
            border-radius: 22px;
            background: linear-gradient(135deg, var(--sc-teal-500), var(--sc-
teal-700));
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.6rem;
            font-weight: 800;
            margin: 0 auto 14px;
            box-shadow: 0 10px 24px rgba(13,115,119,.28);
            transition: transform .2s ease;
        }
        #namePreview {
            font-size: 1.05rem;
            font-weight: 800;
            color: var(--sc-ink-900);
            min-height: 1.3em;
        }
        .code-chip {
            font-family: 'JetBrains Mono', monospace;
            font-size: 0.72rem;
            background: var(--sc-sky-100);
            color: var(--sc-teal-700);
            padding: 4px 12px;
            border-radius: 999px;
            font-weight: 700;
            display: inline-flex;
            align-items: center;
            gap: 6px;
            margin-top: 8px;
        }
        .profile-summary {
            margin-top: 22px;
            padding-top: 18px;
            border-top: 1px dashed var(--sc-sky-100);
            display: flex;
            flex-direction: column;
            gap: 10px;
            text-align: right;
        }
        .summary-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            font-size: 0.8rem;
        }
            .summary-row span.label {
                color: var(--sc-ink-600);
                font-weight: 600;
            }
```

```
            .summary-row span.value {
                font-weight: 800;
                color: var(--sc-ink-900);
                display: flex;
                align-items: center;
                gap: 5px;
            }
        /* ── Document card (right) ── */
        .form-doc {
            background: rgba(255,255,255,.82);
            backdrop-filter: blur(16px);
            border: 1px solid rgba(255,255,255,.6);
            border-radius: var(--sc-radius-lg);
            box-shadow: 0 20px 50px rgba(10,77,79,.08);
            overflow: hidden;
        }
        .form-section {
            padding: 8px 10px;
            opacity: 0;
            transform: translateY(8px);
            animation: sectionFadeIn .45s ease forwards;
        }
            .form-section:not(:last-child) {
                border-bottom: 1px solid var(--sc-sky-100);
            }
            .form-section:nth-of-type(1) {
                animation-delay: .03s;
            }
            .form-section:nth-of-type(2) {
                animation-delay: .08s;
            }
            .form-section:nth-of-type(3) {
                animation-delay: .13s;
            }
            .form-section:nth-of-type(4) {
                animation-delay: .18s;
            }
        @@keyframes sectionFadeIn {
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        .section-head {
            display: flex;
            align-items: center;
            gap: 10px;
            margin-bottom: 18px;
        }
            .section-head .section-icon {
                width: 34px;
                height: 34px;
                border-radius: 10px;
                display: flex;
```

```
                align-items: center;
                justify-content: center;
                background: var(--sc-sky-100);
                color: var(--sc-teal-700);
                font-size: 1rem;
                flex-shrink: 0;
            }
            .section-head h2 {
                font-size: 0.95rem;
                font-weight: 800;
                color: var(--sc-ink-900);
                margin: 0;
            }
            .section-head p {
                font-size: 0.76rem;
                color: var(--sc-ink-600);
                margin: 1px 0 0;
                font-weight: 600;
            }
        .fields-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 14px 16px;
        }
        @@media (max-width: 640px) {
            .fields-grid {
                grid-template-columns: 1fr;
            }
        }
        .fields-grid .span-2 {
            grid-column: 1 / -1;
        }
        .sc-field label {
            display: block;
            font-size: 0.76rem;
            font-weight: 700;
            color: var(--sc-ink-600);
            margin-bottom: 6px;
        }
            .sc-field label .required {
                color: #dc2626;
            }
        .sc-field .form-control,
        .sc-field .form-select {
            width: 100%;
            border: 1.5px solid var(--sc-sky-100);
            border-radius: 10px;
            padding: 10px 12px;
            font-size: 0.9rem;
            background: #fff;
            transition: border-color .2s, box-shadow .2s;
        }
            .sc-field .form-control:focus,
            .sc-field .form-select:focus {
                border-color: var(--sc-teal-500);
```

```
                box-shadow: 0 0 0 3px rgba(15,155,142,.12);
                outline: none;
            }
        .sc-field textarea.form-control {
            min-height: 90px;
            resize: vertical;
        }
        .sc-field .text-danger {
            font-size: 0.76rem;
            margin-top: 4px;
            display: block;
        }
        /* ── Patient type — selectable tiles (replaces the old vertical
                   radio chip list entirely) ── */
        .type-tiles {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 12px;
        }
        @@media (max-width: 640px) {
            .type-tiles {
                grid-template-columns: 1fr;
            }
        }
        .type-tile {
            position: relative;
            border: 2px solid var(--sc-sky-100);
            border-radius: 14px;
            padding: 16px 14px;
            text-align: center;
            cursor: pointer;
            transition: all .22s ease;
            background: #fff;
        }
            .type-tile input {
                display: none;
            }
            .type-tile .tile-icon {
                width: 42px;
                height: 42px;
                margin: 0 auto 10px;
                border-radius: 12px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 1.15rem;
                background: var(--sc-sky-100);
                color: var(--sc-teal-700);
                transition: all .22s ease;
            }
            .type-tile .tile-title {
                font-weight: 800;
                font-size: 0.88rem;
                color: var(--sc-ink-900);
            }
```

```
            .type-tile .tile-desc {
                font-size: 0.72rem;
                color: var(--sc-ink-600);
                margin-top: 3px;
                line-height: 1.4;
            }
            .type-tile .tile-check {
                position: absolute;
                top: 10px;
                left: 10px;
                width: 20px;
                height: 20px;
                border-radius: 50%;
                background: var(--sc-teal-600);
                color: #fff;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: .68rem;
                opacity: 0;
                transform: scale(.5);
                transition: all .2s ease;
            }
            .type-tile:hover {
                border-color: var(--sc-teal-100);
                transform: translateY(-2px);
            }
            .type-tile.selected {
                border-color: var(--sc-teal-600);
                background: linear-gradient(160deg, #fff, var(--sc-sky-50));
                box-shadow: 0 8px 20px rgba(15,155,142,.18);
                transform: translateY(-2px);
            }
                .type-tile.selected .tile-icon {
                    background: linear-gradient(135deg, var(--sc-teal-500),
var(--sc-teal-700));
                    color: #fff;
                }
                .type-tile.selected .tile-check {
                    opacity: 1;
                    transform: scale(1);
                }
        /* ── Gender — segmented control (replaces the two bordered boxes) ── */
        .segmented {
            position: relative;
            display: flex;
            background: var(--sc-sky-100);
            border-radius: 999px;
            padding: 4px;
        }
            .segmented .seg-thumb {
                position: absolute;
                top: 4px;
                bottom: 4px;
                width: calc(50% - 4px);
                background: linear-gradient(135deg, var(--sc-teal-600), var(--
sc-teal-700));
```

```
                border-radius: 999px;
                box-shadow: 0 4px 10px rgba(13,148,136,.28);
                transition: transform .28s cubic-bezier(.3,1.2,.4,1);
                z-index: 0;
            }
            .segmented.pos-1 .seg-thumb {
                transform: translateX(0);
            }
            .segmented.pos-2 .seg-thumb {
                transform: translateX(-100%);
            }
            .segmented label {
                position: relative;
                z-index: 1;
                flex: 1;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 7px;
                padding: 9px 0;
                font-size: 0.85rem;
                font-weight: 700;
                color: var(--sc-ink-600);
                cursor: pointer;
                transition: color .2s ease;
                border-radius: 999px;
            }
                .segmented label input {
                    display: none;
                }
                .segmented label.active {
                    color: #fff;
                }
        /* ── Save row ── */
        .form-save-row {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            padding: 18px 28px;
            background: var(--sc-sky-50);
        }
    </style>
}
<section class="" dir="rtl">
    <div class="row">
        <div class="col-md-12">
            <div class="create-hero">
                <div class="hero-title">
                    <div class="hero-icon"><i class="bi
bi-person-plus-fill"></i></div>
                    <div>
                        <h1>جديدمريضإضافة</h1>
                        <p>المريضتسجيلبياناتأكمل</p>
                    </div>
                </div>
                <div class="hero-actions">
                    <a asp-action="Index" class="btn-hero btn-hero-ghost">
                        <i class="bi bi-x-lg"></i> إلغاء
```

```
                    </a>
                    <button type="submit" form="patientForm" class="btn-hero
btn-hero-light">
                        <i class="bi bi-check-lg"></i> المريضحفظ
                    </button>
                </div>
            </div>
            <form id="patientForm" asp-action="Create" method="post">
                @Html.AntiForgeryToken()
                <div asp-validation-summary="ModelOnly" class="text-danger
mb-3"></div>
```

```
                <div class="row">
```

```
                    <!-- Sticky profile preview -->
                    <div class=" col-3">
                        <div class="avatar-preview" id="avatarPreview">؟</div>
                        <div id="namePreview" class="text-center fs-5"> مريض
جديد</div>
                        <div class="profile-summary" hidden>
                            <div class="summary-row">
                                <span class="label">الملفنوع</span>
                                <span class="value" id="summaryType"><i
class="bi bi-app-indicator"></i> عام</span>
                            </div>
                            <div class="summary-row">
                                <span class="label">النوع</span>
                                <span class="value" id="summaryGender"><i
class="bi bi-dash-circle"></i> محددغير</span>
                            </div>
                        </div>
                       @*  <span class="code-chip"><i class="bi bi-magic"></i>
الحفظعندتلًقائيكود</span> *@
                        <div class="form-section">
                            @* <div class="section-head">
                                <div class="section-icon"><i class="bi bi-
person-fill"></i></div>
                                <div>
                                    <h2>الأساسيةالمعلومات</h2>
                                    <p>الملففيسيظهركماالمريضاسم</p>
                                </div>
                            </div> *@
                            <div class="fields-grid px-1">
                                <div class="text-end sc-field span-2 mx-0">
                                    <label>الكامللسم <span
```

```
class="required">*</span></label>
                                    <input asp-for="FullNameAr"
id="fullNameInput" class="form-control text-center" placeholder=" اسمادخل
المريض" autocomplete="off" />
```

```
                                </div>
                                    <span asp-validation-for="FullNameAr"
class="text-danger"></span>
```

```
                                <div class="sc-field span-2 " hidden>
                                    <label>بالإنجليزيةلسم</label>
                                    <input asp-for="FullNameEn" class="form-
control" placeholder="Optional" autocomplete="off" dir="ltr" />
                                </div>
                                <div class="text-end sc-field span-2 mx-0">
                                    <label>الموبايل <span
class="required">*</span></label>
```

```
                                    <input asp-for="Mobile" class="form-control
text-center" placeholder="01xxxxxxxxx" dir="ltr" />
```

```
                                </div>
                                    <span asp-validation-for="Mobile"
class="text-danger"></span>
```

```
                                <div class="text-end sc-field span-2 mx-0">
                                    <label>الميالدتاريخ</label>
                                    <input asp-for="DateOfBirth" type="date"
class="form-control text-center" />
```

```
                                </div>
                                <div class="text-end sc-field span-2 mx-0">
                                    <label>الدمفصيلة</label>
                                    <select asp-for="BloodGroupId" asp-
items="@bloodGroupList" class="form-select text-center">
```

```
                                        <option value="">-- اختر --</option>
                                    </select>
                                </div>
                            </div>
                        </div>
```

```
                    </div>
                    <!-- Single flowing document with clear sections -->
                    <div class="form-doc col-6">
```

```
                        <div class="form-section">
                            <div class="section-head">
                                <div class="section-icon"><i class="bi bi-card-
checklist"></i></div>
                                <div>
                                    <h2>الملفنوع </h2>
                                    <p> المريضوبياناتالمطلوبالعالجنوعحدد
الأساسية</p>
                                </div>
                            </div>
                            <div class="sc-field span-2" style="margin-
bottom:16px;">
                                <label>الملفنوع <span
class="required">*</span></label>
                                <div class="type-tiles" id="typeTiles">
                                    <label class="type-tile selected" data-
type="general">
                                        <input type="radio" name="PatientType"
value="1" checked />
                                        <span class="tile-check"><i class="bi
bi-check-lg"></i></span>
                                        <div class="tile-icon"><i class="bi bi-
app-indicator"></i></div>
                                        <div class="tile-title">عام</div>
                                        <div class="tile-desc">العامالأسنانعالج
والوقائي</div>
                                    </label>
                                    <label class="type-tile" data-type="ortho">
                                        <input type="radio" name="PatientType"
value="2" />
                                        <span class="tile-check"><i class="bi
bi-check-lg"></i></span>
                                        <div class="tile-icon"><i class="bi bi-
braces"></i></div>
                                        <div class="tile-title">تًقويم</div>
                                        <div class="tile-desc"> دوريةمتابعة
الأسنانلتًقويم</div>
                                    </label>
                                    <label class="type-tile" data-type="both">
                                        <input type="radio" name="PatientType"
value="3" />
```

```
                                        <span class="tile-check"><i class="bi
bi-check-lg"></i></span>
                                        <div class="tile-icon"><i class="bi bi-
heart-pulse"></i></div>
                                        <div class="tile-title">الملفينكال</div>
                                        <div class="tile-desc"> ومتابعةعامعالج
مع[اتًقويم</div>
                                    </label>
                                </div>
                            </div>
                            <div class="fields-grid">
                                <div class="sc-field span-2">
                                    <label>النوع</label>
                                    <div class="segmented pos-1"
id="genderSegmented">
                                        <div class="seg-thumb"></div>
                                        <label data-gender="male">
                                            <input type="radio" name="GenderId"
value="1" />
                                            <i class="bi bi-gender-male"></i>
ذكر
                                        </label>
                                        <label data-gender="female">
                                            <input type="radio" name="GenderId"
value="2" />
                                            <i class="bi bi-gender-female"></i>
أنثى
                                        </label>
                                    </div>
                                    <div class="sc-field span-2 mt-3">
                                        <label>العنوان</label>
                                        <input asp-for="Address" class="form-
control" placeholder="العنوانادخل" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="form-section" hidden>
                            <div class="section-head">
                                <div class="section-icon"><i class="bi bi-
telephone-fill"></i></div>
                                <div>
                                    <h2>التواصلبيانات</h2>
                                    <p>المريضمعالتواصلوسائل</p>
                                </div>
                            </div>
                            <div class="fields-grid">
                                <div class="sc-field">
                                    <label>الًقوميالرقم</label>
                                    <input asp-for="NationalId" class="form-
control" placeholder="14 رقم" maxlength="14" dir="ltr" />
                                </div>
                                <div class="sc-field">
                                    <label>تليفون</label>
                                    <input asp-for="Phone" class="form-control"
placeholder="اختياري" dir="ltr" />
                                </div>
                                <div class="sc-field">
                                    <label>الإلكترونيالبريد</label>
                                    <input asp-for="Email" class="form-control"
placeholder="patient@email.com" dir="ltr" />
```

```
                                </div>
                            </div>
                        </div>
                        <div class="form-save-row" hidden>
                            <a asp-action="Index" class="btn-hero"
style="background:var(--sc-sky-100);color:var(--sc-ink-600);">
                                <i class="bi bi-x-lg"></i> إلغاء
                            </a>
                            <button type="submit" class="btn-hero"
style="background:linear-gradient(135deg,var(--sc-teal-600),var(--sc-
teal-700));color:#fff;">
                                <i class="bi bi-check-lg"></i> المريضحفظ
                            </button>
                        </div>
                    </div>
                    <div class="col-3">
                        <div class="form-section">
                            <div class="section-head">
                                <div class="section-icon"><i class="bi bi-heart-
pulse-fill"></i></div>
                                <div>
                                    <h2>الطبيالتاريخ</h2>
                                    <p>عالجأيبدءقبلمهمةمعلومات</p>
                                </div>
                            </div>
                            <div class="fields-grid">
                                <div class="sc-field span-2">
                                    <label>الحساسية</label>
                                    <input name="MedicalInfo.Allergies"
class="form-control" placeholder="ليدوكايينبنسلين، :مثال..." />
                                </div>
                                <div class="sc-field span-2">
                                    <label>مزمنةأمراض</label>
                                    <input name="MedicalInfo.ChronicDiseases"
class="form-control" placeholder="ضغطسكري،..." />
                                </div>
                                <div class="sc-field span-2">
                                    <label>الحاليةالأدوية</label>
                                    <input name="MedicalInfo.CurrentMedications"
class="form-control" placeholder="المريضلديالمستخدمةالأدوية ..." />
                                </div>
                                <div class="sc-field span-2">
                                    <label>التدخينحالة</label>
                                    <select asp-for="SmokingStatus" class="form-
select">
                                        <option value="">-- اختر --</option>
                                        <option value="NonSmoker"> لا
يدخن</option>
                                        <option value="FormerSmoker"> مدخن
سابق</option>
                                        <option value="LightSmoker"> مدخن
خفيف</option>
                                        <option value="HeavySmoker"> مدخن
شره</option>
                                    </select>
                                </div>
                                <div class="sc-field span-2">
                                    <label>عاجلةطبيةمالحظات</label>
                                    <textarea name="MedicalInfo.EmergencyNotes"
class="form-control" placeholder="للدكتورتحذيراتأي..."></textarea>
                                </div>
                            </div>
```

```
                        </div>
                    </div>
                </div>
                <div class="create-layout">
                </div>
            </form>
        </div>
    </div>
</section>
@section Scripts {
    @{
        await Html.RenderPartialAsync("_ValidationScriptsPartial");
    }
    <script>
        // Live avatar + name preview while typing
        const nameInput = document.getElementById('fullNameInput');
        const namePreview = document.getElementById('namePreview');
        const avatarPreview = document.getElementById('avatarPreview');
        nameInput.addEventListener('input', function () {
            const name = this.value.trim();
            namePreview.textContent = name || 'جديدمريض';
            const initials = name.split(' ').filter(Boolean).slice(0, 2).map(p
=> p[0]).join('');
            avatarPreview.textContent = initials || '؟';
        });
        // Patient type tiles
        const typeLabels = {
            general: { icon: 'bi-app-indicator', text: 'عام' },
            ortho: { icon: 'bi-braces', text: 'تًقويم' },
            both: { icon: 'bi-heart-pulse', text: 'الملفينكال' }
        };
        document.querySelectorAll('#typeTiles
input[type=radio]').forEach(function (radio) {
            radio.addEventListener('change', function () {
                document.querySelectorAll('.type-tile').forEach(t =>
t.classList.remove('selected'));
                const tile = this.closest('.type-tile');
                tile.classList.add('selected');
                const info = typeLabels[tile.dataset.type];
                document.getElementById('summaryType').innerHTML = `<i class="bi
${info.icon}"></i> ${info.text}`;
            });
        });
        // Gender segmented control
        const genderSegmented = document.getElementById('genderSegmented');
        const genderLabels = { male: { icon: 'bi-gender-male', text: 'ذكر' },
female: { icon: 'bi-gender-female', text: 'أنثى' } };
        genderSegmented.querySelectorAll('input[type=radio]').forEach(function
(radio, idx) {
            radio.addEventListener('change', function () {
                genderSegmented.querySelectorAll('label[data-gender]').forEach(l
=> l.classList.remove('active'));
                const lbl = this.closest('label');
                lbl.classList.add('active');
                genderSegmented.classList.toggle('pos-1', idx === 0);
                genderSegmented.classList.toggle('pos-2', idx === 1);
                const info = genderLabels[lbl.dataset.gender];
                document.getElementById('summaryGender').innerHTML = `<i
class="bi ${info.icon}"></i> ${info.text}`;
            });
        });
```

```
    </script>
}
```

```
//View / Patients / edit.cshtml
```

```
@using Dental.Models.Patient
@using Dental.Modules.Patients.DTOs
@using System.Linq
@model Dental.Modules.Patients.DTOs.UpdatePatientDto
@{
    ViewData["Title"] = "المريضبياناتتعديل";
    var genderList = ViewBag.Genders as SelectList ?? new SelectList(new
List<SelectListItem>());
    var bloodGroupList = ViewBag.BloodGroups as SelectList ?? new SelectList(new
List<SelectListItem>());
```

```
    var initialName = string.IsNullOrWhiteSpace(Model.FullNameAr) ? "مريض" :
Model.FullNameAr;
    var initialInitials = string.IsNullOrWhiteSpace(Model.FullNameAr)
        ? "؟"
        : string.Join("", Model.FullNameAr.Split(' ',
StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0]));
}
@section Styles {
    <style>
        :root {
            --sc-teal-900: #0a4d4f;
            --sc-teal-700: #0d7377;
            --sc-teal-600: #0f9b8e;
            --sc-teal-500: #14b8a6;
            --sc-teal-100: #c7f2ea;
            --sc-sky-50: #eafbfc;
            --sc-sky-100: #dcf5f7;
            --sc-coral-300: #f6c6bc;
            --sc-ink-900: #0b2e30;
            --sc-ink-600: #4b6b6c;
            --sc-radius-lg: 18px;
            --sc-radius-md: 12px;
        }
        body {
            background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
transparent 26%), var(--sc-sky-50);
        }
        /* ── Hero ── */
        .create-hero {
            background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
            border-radius: var(--sc-radius-lg);
            padding: 22px 28px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
            margin-bottom: 20px;
            flex-wrap: wrap;
            gap: 16px;
            position: relative;
            overflow: hidden;
        }
            .create-hero::after {
```

```
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
                pointer-events: none;
            }
            .create-hero .hero-title {
                display: flex;
                align-items: center;
                gap: 16px;
                position: relative;
                z-index: 1;
            }
            .create-hero .hero-icon {
                width: 52px;
                height: 52px;
                background: rgba(255,255,255,.18);
                color: #fff;
                border-radius: 14px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 1.4rem;
                flex-shrink: 0;
                backdrop-filter: blur(6px);
            }
            .create-hero h1 {
                font-size: 1.25rem;
                font-weight: 800;
                margin: 0;
                color: #fff;
            }
            .create-hero p {
                font-size: 0.85rem;
                margin: 2px 0 0;
                color: rgba(255,255,255,.9);
                font-weight: 600;
            }
            .create-hero .hero-actions {
                display: flex;
                gap: 10px;
                position: relative;
                z-index: 1;
                flex-wrap: wrap;
            }
        .btn-hero {
            border: none;
            padding: 11px 22px;
            border-radius: 12px;
            font-weight: 800;
            display: flex;
            align-items: center;
            gap: 8px;
            text-decoration: none;
            transition: transform .2s ease, box-shadow .2s ease;
            cursor: pointer;
            font-size: 0.88rem;
```

```
        }
        .btn-hero-light {
            background: #fff;
            color: var(--sc-teal-700);
        }
            .btn-hero-light:hover {
                transform: translateY(-2px);
                box-shadow: 0 8px 18px rgba(10,40,42,.24);
                color: var(--sc-teal-700);
            }
        .btn-hero-ghost {
            background: rgba(255,255,255,.14);
            color: #fff;
            border: 1px solid rgba(255,255,255,.4);
        }
            .btn-hero-ghost:hover {
                background: rgba(255,255,255,.24);
                color: #fff;
                transform: translateY(-2px);
            }
        /* ── Profile panel (right col-3) ── */
        .profile-panel-inline {
            background: rgba(255,255,255,.8);
            backdrop-filter: blur(16px);
            border: 1px solid rgba(255,255,255,.6);
            border-radius: var(--sc-radius-lg);
            padding: 22px 18px;
            box-shadow: 0 20px 50px rgba(10,77,79,.08);
        }
        .avatar-preview {
            width: 72px;
            height: 72px;
            border-radius: 20px;
            background: linear-gradient(135deg, var(--sc-teal-500), var(--sc-
teal-700));
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            font-weight: 800;
            margin: 0 auto 12px;
            box-shadow: 0 10px 24px rgba(13,115,119,.28);
        }
        #namePreview {
            font-size: 1rem;
            font-weight: 800;
            color: var(--sc-ink-900);
            min-height: 1.3em;
            margin-bottom: 16px;
        }
        .code-chip {
            font-family: 'JetBrains Mono', monospace;
            font-size: 0.72rem;
            background: var(--sc-sky-100);
            color: var(--sc-teal-700);
```

```
            padding: 4px 12px;
            border-radius: 999px;
            font-weight: 700;
            display: inline-flex;
            align-items: center;
            gap: 6px;
        }
        .profile-summary {
            margin-top: 22px;
            padding-top: 18px;
            border-top: 1px dashed var(--sc-sky-100);
            display: flex;
            flex-direction: column;
            gap: 10px;
            text-align: right;
        }
        .summary-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            font-size: 0.8rem;
        }
            .summary-row span.label {
                color: var(--sc-ink-600);
                font-weight: 600;
            }
            .summary-row span.value {
                font-weight: 800;
                color: var(--sc-ink-900);
                display: flex;
                align-items: center;
                gap: 5px;
            }
        /* ── Document card ── */
        .form-doc {
            background: rgba(255,255,255,.82);
            backdrop-filter: blur(16px);
            border: 1px solid rgba(255,255,255,.6);
            border-radius: var(--sc-radius-lg);
            box-shadow: 0 20px 50px rgba(10,77,79,.08);
            overflow: hidden;
            height: 100%;
        }
        .form-section {
            padding: 20px 22px;
            opacity: 0;
            transform: translateY(8px);
            animation: sectionFadeIn .45s ease forwards;
        }
            .form-section:not(:last-child) {
                border-bottom: 1px solid var(--sc-sky-100);
            }
            .form-section:nth-of-type(1) {
                animation-delay: .03s;
            }
```

```
            .form-section:nth-of-type(2) {
                animation-delay: .08s;
            }
            .form-section:nth-of-type(3) {
                animation-delay: .13s;
            }
        @@keyframes sectionFadeIn {
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        .section-head {
            display: flex;
            align-items: center;
            gap: 10px;
            margin-bottom: 18px;
        }
            .section-head .section-icon {
                width: 34px;
                height: 34px;
                border-radius: 10px;
                display: flex;
                align-items: center;
                justify-content: center;
                background: var(--sc-sky-100);
                color: var(--sc-teal-700);
                font-size: 1rem;
                flex-shrink: 0;
            }
            .section-head h2 {
                font-size: 0.95rem;
                font-weight: 800;
                color: var(--sc-ink-900);
                margin: 0;
            }
            .section-head p {
                font-size: 0.76rem;
                color: var(--sc-ink-600);
                margin: 1px 0 0;
                font-weight: 600;
            }
        .fields-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 14px 16px;
        }
        @@media (max-width: 640px) {
            .fields-grid {
                grid-template-columns: 1fr;
            }
        }
        .fields-grid .span-2 {
            grid-column: 1 / -1;
        }
```

```
        .sc-field label {
            display: block;
            font-size: 0.76rem;
            font-weight: 700;
            color: var(--sc-ink-600);
            margin-bottom: 6px;
        }
            .sc-field label .required {
                color: #dc2626;
            }
        .sc-field .form-control,
        .sc-field .form-select {
            width: 100%;
            border: 1.5px solid var(--sc-sky-100);
            border-radius: 10px;
            padding: 10px 12px;
            font-size: 0.9rem;
            background: #fff;
            transition: border-color .2s, box-shadow .2s;
        }
            .sc-field .form-control:focus,
            .sc-field .form-select:focus {
                border-color: var(--sc-teal-500);
                box-shadow: 0 0 0 3px rgba(15,155,142,.12);
                outline: none;
            }
        .sc-field textarea.form-control {
            min-height: 90px;
            resize: vertical;
        }
        .sc-field .text-danger {
            font-size: 0.76rem;
            margin-top: 4px;
            display: block;
        }
        .sc-checkbox {
            display: flex;
            align-items: center;
            gap: 8px;
            cursor: pointer;
            font-size: .85rem;
            font-weight: 700;
            color: var(--sc-ink-900);
            padding-top: 6px;
        }
            .sc-checkbox input {
                width: 18px;
                height: 18px;
                accent-color: var(--sc-teal-600);
                cursor: pointer;
            }
        /* ── Patient type — selectable tiles ── */
        .type-tiles {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
```

```
            gap: 12px;
        }
        @@media (max-width: 640px) {
            .type-tiles {
                grid-template-columns: 1fr;
            }
        }
        .type-tile {
            position: relative;
            border: 2px solid var(--sc-sky-100);
            border-radius: 14px;
            padding: 16px 14px;
            text-align: center;
            cursor: pointer;
            transition: all .22s ease;
            background: #fff;
        }
            .type-tile input {
                display: none;
            }
            .type-tile .tile-icon {
                width: 42px;
                height: 42px;
                margin: 0 auto 10px;
                border-radius: 12px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 1.15rem;
                background: var(--sc-sky-100);
                color: var(--sc-teal-700);
                transition: all .22s ease;
            }
```

```
            .type-tile .tile-title {
                font-weight: 800;
                font-size: 0.88rem;
                color: var(--sc-ink-900);
            }
```

```
            .type-tile .tile-desc {
                font-size: 0.72rem;
                color: var(--sc-ink-600);
                margin-top: 3px;
                line-height: 1.4;
            }
            .type-tile .tile-check {
                position: absolute;
                top: 10px;
                left: 10px;
                width: 20px;
                height: 20px;
                border-radius: 50%;
                background: var(--sc-teal-600);
                color: #fff;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: .68rem;
```

```
                opacity: 0;
                transform: scale(.5);
                transition: all .2s ease;
            }
            .type-tile:hover {
                border-color: var(--sc-teal-100);
                transform: translateY(-2px);
            }
            .type-tile.selected {
                border-color: var(--sc-teal-600);
                background: linear-gradient(160deg, #fff, var(--sc-sky-50));
                box-shadow: 0 8px 20px rgba(15,155,142,.18);
                transform: translateY(-2px);
            }
                .type-tile.selected .tile-icon {
                    background: linear-gradient(135deg, var(--sc-teal-500),
var(--sc-teal-700));
                    color: #fff;
                }
                .type-tile.selected .tile-check {
                    opacity: 1;
                    transform: scale(1);
                }
        /* ── Gender — segmented control ── */
        .segmented {
            position: relative;
            display: flex;
            background: var(--sc-sky-100);
            border-radius: 999px;
            padding: 4px;
        }
            .segmented .seg-thumb {
                position: absolute;
                top: 4px;
                bottom: 4px;
                width: calc(50% - 4px);
                background: linear-gradient(135deg, var(--sc-teal-600), var(--
sc-teal-700));
                border-radius: 999px;
                box-shadow: 0 4px 10px rgba(13,148,136,.28);
                transition: transform .28s cubic-bezier(.3,1.2,.4,1);
                z-index: 0;
            }
            .segmented.pos-1 .seg-thumb {
                transform: translateX(0);
            }
            .segmented.pos-2 .seg-thumb {
                transform: translateX(-100%);
            }
            .segmented label {
                position: relative;
                z-index: 1;
                flex: 1;
                display: flex;
                align-items: center;
```

```
                justify-content: center;
                gap: 7px;
                padding: 9px 0;
                font-size: 0.85rem;
                font-weight: 700;
                color: var(--sc-ink-600);
                cursor: pointer;
                transition: color .2s ease;
                border-radius: 999px;
            }
                .segmented label input {
                    display: none;
                }
                .segmented label.active {
                    color: #fff;
                }
    </style>
}
<section class="" dir="rtl">
    <div class="row">
        <div class="col-md-12">
            <div class="create-hero">
                <div class="hero-title">
                    <div class="hero-icon"><i class="bi
bi-pencil-square"></i></div>
                    <div>
                        <h1>المريضبياناتتعديل</h1>
                        <p>الطبيوالتاريخالمريضبياناتتحديث</p>
                    </div>
                </div>
                <div class="hero-actions">
                    <a asp-action="Details" asp-route-id="@Model.Id" class="btn-
hero btn-hero-ghost">
                        <i class="bi bi-x-lg"></i> إلغاء
                    </a>
                    <button type="submit" form="editForm" class="btn-hero btn-
hero-light">
                        <i class="bi bi-check-lg"></i> التعديالتحفظ
                    </button>
                </div>
            </div>
            <form id="editForm" asp-action="Edit" method="post">
                @Html.AntiForgeryToken()
                <input type="hidden" asp-for="Id" />
                <div asp-validation-summary="ModelOnly" class="text-danger
mb-3"></div>
                <div class="row g-3">
                    <!-- Identity + basic fields -->
                    <div class="col-12 col-lg-3">
                            <div class="avatar-preview"
id="avatarPreview">@initialInitials</div>
                            <div id="namePreview" class="text-
center">@initialName</div>
                        <div class="profile-summary" hidden>
                            <div class="summary-row">
                                <span class="label">الملفنوع</span>
                                <span class="value" id="summaryType"><i
```

```
class="bi bi-app-indicator"></i> عام</span>
                            </div>
                            <div class="summary-row">
                                <span class="label">النوع</span>
                                <span class="value" id="summaryGender"><i
class="bi bi-dash-circle"></i> محددغير</span>
                            </div>
                        </div>
                            <div class="fields-grid">
                                <div class="sc-field span-2">
                                    <label>الكامللسم <span
class="required">*</span></label>
```

```
                                    <input asp-for="FullNameAr"
id="fullNameInput" class="form-control text-center" placeholder=" اسمأدخل
المريض" autocomplete="off" />
```

```
                                </div>
                                    <span asp-validation-for="FullNameAr"
class="text-danger"></span>
```

```
                                <div class="sc-field span-2"hidden>
                                    <label>بالإنجليزيةلسم</label>
                                    <input asp-for="FullNameEn" class="form-
control" placeholder="Optional" autocomplete="off" dir="ltr" />
                                </div>
                                <div class="sc-field span-2">
                                    <label>الموبايل <span
class="required">*</span></label>
```

```
                                <input asp-for="Mobile" class="form-control
text-center" placeholder="01xxxxxxxxx" dir="ltr" />
                                </div>
```

```
                                    <span asp-validation-for="Mobile"
class="text-danger"></span>
```

```
                                <div class="sc-field span-2">
                                    <label>الميالدتاريخ</label>
                                <input asp-for="DateOfBirth" type="date"
class="form-control text-center" />
                                </div>
                                <div class="sc-field span-2">
                                    <label>الدمفصيلة</label>
                                    <select asp-for="BloodGroupId" asp-
items="@bloodGroupList" class="form-select text-center">
                                        <option value="">-- اختر --</option>
                                    </select>
                                </div>
                                <div class="sc-field span-2" hidden>
                                    <label>الًقوميالرقم</label>
                                    <input asp-for="NationalId" class="form-
control" placeholder="14 رقم" maxlength="14" dir="ltr" />
                                </div>
```

```
                        </div>
                    </div>
```



```
                            </div>
```

```
                            <div class="sc-field span-2" style="margin-
bottom:16px;">
```

```
                                <label>الملفنوع <span
class="required">*</span></label>
                                <div class="type-tiles" id="typeTiles">
                                    <label class="type-tile @(Model.PatientType
== PatientType.General ? "selected" : "")" data-type="general">
                                        <input type="radio" name="PatientType"
value="1" @(Model.PatientType == PatientType.General ? "checked" : "") />
                                        <span class="tile-check"><i class="bi
bi-check-lg"></i></span>
```

```
                                        <div class="tile-icon"><i class="bi bi-
app-indicator"></i></div>
                                        <div class="tile-title">عام</div>
                                        <div class="tile-desc">العامالأسنانعالج
والوقائي</div>
                                    </label>
                                    <label class="type-tile @(Model.PatientType
== PatientType.Orthodontics ? "selected" : "")" data-type="ortho">
                                        <input type="radio" name="PatientType"
value="2" @(Model.PatientType == PatientType.Orthodontics ? "checked" : "") />
                                        <span class="tile-check"><i class="bi
bi-check-lg"></i></span>
```

```
                                        <div class="tile-icon"><i class="bi bi-
braces"></i></div>
                                        <div class="tile-title">تًقويم</div>
                                        <div class="tile-desc"> دوريةمتابعة
الأسنانلتًقويم</div>
```

```
                                    </label>
```

```
                                    <label class="type-tile @(Model.PatientType
== PatientType.Both ? "selected" : "")" data-type="both">
                                        <input type="radio" name="PatientType"
value="3" @(Model.PatientType == PatientType.Both ? "checked" : "") />
                                        <span class="tile-check"><i class="bi
bi-check-lg"></i></span>
```

```
                                        <div class="tile-icon"><i class="bi bi-
heart-pulse"></i></div>
```

```
                                        <div class="tile-title">الملفينكال</div>
                                        <div class="tile-desc"> ومتابعةعامعالج
مع[اتًقويم</div>
```



```
                            <div class="fields-grid">
                                <div class="sc-field span-2">
                                    <label>النوع</label>
                                    <div class="segmented @(Model.GenderId ==
2 ? "pos-2" : "pos-1")" id="genderSegmented">
                                        <div class="seg-thumb"></div>
                                        <label data-gender="male"
class="@(Model.GenderId == 1 ? "active" : "")">
                                            <input type="radio" name="GenderId"
value="1" @(Model.GenderId == 1 ? "checked" : "") />
                                            <i class="bi bi-gender-male"></i>
ذكر
```

```
                                        </label>
                                        <label data-gender="female"
class="@(Model.GenderId == 2 ? "active" : "")">
                                            <input type="radio" name="GenderId"
value="2" @(Model.GenderId == 2 ? "checked" : "") />
                                            <i class="bi bi-gender-female"></i>
```

```
أنثى
```

```
                                        </label>
                                    </div>
                                </div>
                                <div class="sc-field span-2 mt-3">
                                    <label>العنوان</label>
```

```
                                    <input asp-for="Address" class="form-
control" placeholder="العنوانأدخل" />
                                </div>
                            </div>
                        </div>
                        <div class="form-section" hidden>
                            <div class="section-head">
                                <div class="section-icon"><i class="bi bi-
telephone-fill"></i></div>
                                <div>
                                    <h2>التواصلبيانات</h2>
                                    <p>المريضمعالتواصلوسائل</p>
                                </div>
                            </div>
                            <div class="fields-grid">
                                <div class="sc-field">
                                    <label>تليفون</label>
                                    <input asp-for="Phone" class="form-control"
placeholder="اختياري" dir="ltr" />
                                </div>
                                <div class="sc-field">
                                    <label>الإلكترونيالبريد</label>
                                    <input asp-for="Email" class="form-control"
placeholder="patient@email.com" dir="ltr" />
                                </div>
                                <div class="sc-field span-2">
                                    <label>المهنة</label>
                                    <input asp-for="Occupation" class="form-
control" placeholder="اختياري" />
                                </div>
                            </div>
                        </div>
                    </div>
```



```
                                    <div class="sc-field span-2">
                                        <label>الحاليةالأدوية</label>
                                        <input asp-for="CurrentMedications"
class="form-control" />
```

```
                                    </div>
```

```
                                    <div class="sc-field span-2">
                                        <label>التدخينحالة</label>
                                        <select asp-for="SmokingStatus"
class="form-select">
```

```
                                            <option value="">-- اختر --</option>
                                            <option value="NonSmoker"> لا
يدخن</option>
                                            <option value="FormerSmoker"> مدخن
سابق</option>
                                            <option value="LightSmoker"> مدخن
خفيف</option>
                                            <option value="HeavySmoker"> مدخن
شره</option>
```

```
                                        </select>
```

```
                                    </div>
```

```
                                    <div class="sc-field span-2"hidden>
                                        <label class="sc-checkbox">
```

```
                                            <input type="checkbox" asp-
for="IsPregnant" />
```

```
                                            <span>حامل</span>
                                        </label>
                                    </div>
```

```
                                    <div class="sc-field span-2">
                                        <label>عاجلةطبيةمالحظات</label>
                                        <textarea asp-for="EmergencyNotes"
class="form-control" placeholder="للدكتورتحذيرات..."></textarea>
                                    </div>
                                </div>
                            </div>
                    </div>
```

```
                    <!-- General notes (full width) -->
                    <div class="col-12"hidden>
                        <div class="form-doc">
                            <div class="form-section">
                                <div class="section-head">
                                    <div class="section-icon"><i class="bi bi-
journal-text"></i></div>
                                    <div>
                                        <h2>إضافيةمالحظات</h2>
                                        <p>بالمريضخاصةتعليماتأومالحظاتأي</p>
                                    </div>
                                </div>
                                <div class="fields-grid">
                                    <div class="sc-field span-2">
```

```
                                        <textarea asp-for="Notes" class="form-
control" placeholder="بالمريضخاصةتعليماتأومالحظاتأي..."></textarea>
                                    </div>
```

```
                                </div>
                            </div>
                        </div>
                    </div>
```

```
                </div>
```

```
            </form>
```

```
        </div>
    </div>
```

```
</section>
@section Scripts {
    @{
        await Html.RenderPartialAsync("_ValidationScriptsPartial");
    }
    <script>
        // Live avatar + name preview while typing (initial values are already
        // rendered server-side above, this just keeps them in sync on edit)
        const nameInput = document.getElementById('fullNameInput');
        const namePreview = document.getElementById('namePreview');
        const avatarPreview = document.getElementById('avatarPreview');
        nameInput.addEventListener('input', function () {
            const name = this.value.trim();
            namePreview.textContent = name || 'مريض';
            const initials = name.split(' ').filter(Boolean).slice(0, 2).map(p
=> p[0]).join('');
            avatarPreview.textContent = initials || '؟';
        });
        // Patient type tiles
        document.querySelectorAll('#typeTiles
input[type=radio]').forEach(function (radio) {
            radio.addEventListener('change', function () {
                document.querySelectorAll('.type-tile').forEach(t =>
t.classList.remove('selected'));
                this.closest('.type-tile').classList.add('selected');
            });
        });
        // Gender segmented control
        const genderSegmented = document.getElementById('genderSegmented');
        genderSegmented.querySelectorAll('input[type=radio]').forEach(function
(radio, idx) {
            radio.addEventListener('change', function () {
                genderSegmented.querySelectorAll('label[data-gender]').forEach(l
=> l.classList.remove('active'));
                this.closest('label').classList.add('active');
                genderSegmented.classList.toggle('pos-1', idx === 0);
                genderSegmented.classList.toggle('pos-2', idx === 1);
            });
        });
    </script>
}
```

```
//View / Patients / index.cshtml
```

```
@model Dental.Modules.Patients.ViewModels.PatientIndexViewModel
@{
    ViewData["Title"] = "المرضىقائمة";
    Layout = "~/Views/Shared/_Layout.cshtml";
}
@section Styles {
    <link rel="stylesheet"
href="https://cdn.datatables.net/1.13.7/css/jquery.dataTables.min.css" />
    <link rel="stylesheet"
href="https://cdn.datatables.net/responsive/2.5.0/css/responsive.dataTables.min.
css" />
    <style>
        :root {
            --sc-teal-900: #0a4d4f;
            --sc-teal-700: #0d7377;
            --sc-teal-600: #0f9b8e;
            --sc-teal-500: #14b8a6;
```

```
            --sc-teal-100: #c7f2ea;
            --sc-sky-50: #eafbfc;
            --sc-sky-100: #dcf5f7;
            --sc-coral-300: #f6c6bc;
            --sc-ink-900: #0b2e30;
            --sc-ink-600: #4b6b6c;
            --brand: #0d9488;
            --brand-dark: #0f766e;
            --brand-light: #f0fdfa;
            --radius-lg: 18px;
            --radius-md: 12px;
            --shadow-soft: 0 1px 3px rgba(15, 23, 42, 0.06);
            --shadow-hover: 0 10px 26px rgba(10, 77, 79, 0.12);
        }
        body {
            background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
transparent 26%), var(--sc-sky-50);
        }
        /* ── Hero ── */
        .patients-hero {
            background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
            border-radius: var(--radius-lg);
            padding: 22px 28px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
            margin-bottom: 18px;
            flex-wrap: wrap;
            gap: 16px;
            position: relative;
            overflow: hidden;
        }
            .patients-hero::after {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
                pointer-events: none;
            }
            .patients-hero .hero-title {
                display: flex;
                align-items: center;
                gap: 16px;
                position: relative;
                z-index: 1;
            }
            .patients-hero .hero-icon {
                width: 52px;
                height: 52px;
                background: rgba(255,255,255,.18);
                color: #fff;
                border-radius: 14px;
                display: flex;
                align-items: center;
                justify-content: center;
```

```
                font-size: 1.4rem;
                flex-shrink: 0;
                backdrop-filter: blur(6px);
            }
            .patients-hero h1 {
                font-size: 1.25rem;
                font-weight: 800;
                margin: 0;
                color: #fff;
            }
            .patients-hero p {
                font-size: 0.85rem;
                margin: 2px 0 0;
                color: rgba(255,255,255,.9);
                font-weight: 600;
            }
        .btn-add-patient {
            background: #fff;
            color: var(--sc-teal-700);
            border: none;
            padding: 11px 22px;
            border-radius: 12px;
            font-weight: 800;
            display: flex;
            align-items: center;
            gap: 8px;
            text-decoration: none;
            box-shadow: 0 8px 20px rgba(10, 40, 42, .18);
            transition: all .2s ease;
            font-size: 0.9rem;
            position: relative;
            z-index: 1;
        }
            .btn-add-patient:hover {
                transform: translateY(-2px);
                box-shadow: 0 12px 26px rgba(10, 40, 42, .24);
                color: var(--sc-teal-700);
            }
        /* ── Stats ── */
        .stat-card {
            background: rgba(255,255,255,.78);
            backdrop-filter: blur(14px);
            border-radius: 16px;
            padding: 18px 20px;
            display: flex;
            align-items: center;
            gap: 14px;
            border: 1px solid rgba(255,255,255,.6);
            box-shadow: var(--shadow-soft);
            transition: all .25s ease;
            height: 100%;
        }
            .stat-card:hover {
                transform: translateY(-3px);
                box-shadow: var(--shadow-hover);
            }
```

```
        .stat-icon {
```

```
            width: 46px;
            height: 46px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.15rem;
            flex-shrink: 0;
        }
        .stat-total .stat-icon {
            background: #f0fdfa;
            color: #0f766e;
        }
        .stat-general .stat-icon {
            background: #eff6ff;
            color: #1d4ed8;
        }
        .stat-ortho .stat-icon {
            background: #fffbeb;
            color: #b45309;
        }
        .stat-both .stat-icon {
            background: #f5f3ff;
            color: #6d28d9;
        }
        .stat-value {
            display: block;
            font-size: 1.4rem;
            font-weight: 800;
            color: var(--sc-ink-900);
            line-height: 1.1;
        }
        .stat-label {
            font-size: 0.78rem;
            color: var(--sc-ink-600);
            font-weight: 600;
        }
        /* ── Card + Filters ── */
        .patients-card {
            background: rgba(255,255,255,.82);
            backdrop-filter: blur(16px);
            border-radius: var(--radius-lg);
            box-shadow: 0 20px 50px rgba(10, 77, 79, .08);
            overflow: hidden;
            border: 1px solid rgba(255,255,255,.6);
        }
        .filter-bar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 14px 20px;
            border-bottom: 1px solid var(--sc-sky-100);
            flex-wrap: wrap;
            gap: 12px;
        }
```

```
        .pill-toggle {
            display: inline-flex;
            background: var(--sc-sky-100);
            border-radius: 999px;
            padding: 4px;
            gap: 2px;
        }
            .pill-toggle .pill-btn {
                border: none;
                background: transparent;
                padding: 8px 18px;
                border-radius: 999px;
                font-size: 0.85rem;
                font-weight: 700;
                color: var(--sc-ink-600);
                display: flex;
                align-items: center;
                gap: 6px;
                transition: all .2s ease;
                cursor: pointer;
            }
                .pill-toggle .pill-btn.active {
                    background: linear-gradient(135deg, var(--sc-teal-600),
var(--sc-teal-700));
                    color: #fff;
                    box-shadow: 0 4px 10px rgba(13,148,136,0.28);
                }
                .pill-toggle .pill-btn:hover:not(.active) {
                    color: var(--brand-dark);
                    background: #fff;
                }
        /* ── Table ── */
        #tablePatients thead th {
            background: var(--sc-sky-50) !important;
            color: var(--sc-ink-600) !important;
            font-size: 0.78rem;
            font-weight: 800;
            border: none !important;
            border-bottom: 2px solid var(--sc-sky-100) !important;
            padding: 14px 10px;
            text-transform: uppercase;
            letter-spacing: 0.4px;
        }
        #tablePatients tbody td {
            padding: 12px 10px;
            vertical-align: middle;
            border-color: var(--sc-sky-100);
            font-size: 0.88rem;
        }
        #tablePatients tbody tr {
            transition: background-color .2s ease, transform .15s ease;
            opacity: 0;
            animation: rowFadeIn .4s ease forwards;
        }
            #tablePatients tbody tr:hover {
                background-color: var(--brand-light) !important;
            }
```

```
        @@keyframes rowFadeIn {
            from {
                opacity: 0;
                transform: translateY(6px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        #tablePatients tbody tr:nth-child(1) {
            animation-delay: .02s;
        }
        #tablePatients tbody tr:nth-child(2) {
            animation-delay: .05s;
        }
        #tablePatients tbody tr:nth-child(3) {
            animation-delay: .08s;
        }
        #tablePatients tbody tr:nth-child(4) {
            animation-delay: .11s;
        }
        #tablePatients tbody tr:nth-child(5) {
            animation-delay: .14s;
        }
        #tablePatients tbody tr:nth-child(6) {
            animation-delay: .17s;
        }
        #tablePatients tbody tr:nth-child(7) {
            animation-delay: .20s;
        }
        #tablePatients tbody tr:nth-child(8) {
            animation-delay: .23s;
        }
```

```
        .patient-avatar {
            width: 38px;
            height: 38px;
            border-radius: 10px;
            background: linear-gradient(135deg, var(--sc-teal-500), var(--sc-
teal-700));
```

```
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.8rem;
            font-weight: 800;
            flex-shrink: 0;
        }
        .patient-name-cell {
            display: flex;
            align-items: center;
            gap: 10px;
```

```
        }
        .action-icon-btn {
            width: 34px;
            height: 34px;
            border-radius: 9px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            border: none;
            background: transparent;
            color: var(--sc-ink-600);
            transition: all .2s ease;
        }
            .action-icon-btn:hover {
                background: var(--brand-light);
                color: var(--brand-dark);
                transform: translateY(-1px);
            }
            .action-icon-btn.danger:hover {
                background: #fee2e2;
                color: #dc2626;
            }
        /* New: per-row "start session" button — visually distinct from the
                   plain icon buttons since it's the primary action for this
screen */
        .btn-row-session {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 7px 12px;
            border-radius: 9px;
            border: none;
            background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
            color: #fff;
            font-size: 0.76rem;
            font-weight: 800;
            text-decoration: none;
            box-shadow: 0 4px 10px rgba(13,148,136,0.25);
            transition: all .2s ease;
            white-space: nowrap;
        }
            .btn-row-session:hover {
                transform: translateY(-1px);
                box-shadow: 0 6px 16px rgba(13,148,136,0.35);
                color: #fff;
            }
            .btn-row-session i {
                font-size: 0.85rem;
            }
        .row-actions-cell {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 4px;
            flex-wrap: nowrap;
        }
```

```
        .table-loading-overlay {
            position: absolute;
            inset: 0;
            background: rgba(255,255,255,0.8);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 5;
            backdrop-filter: blur(2px);
            opacity: 0;
            pointer-events: none;
            transition: opacity .2s ease;
        }
            .table-loading-overlay.show {
                opacity: 1;
                pointer-events: all;
            }
        .loading-pulse {
            display: flex;
            align-items: center;
            gap: 10px;
            color: var(--brand-dark);
            font-weight: 700;
            font-size: 0.9rem;
        }
            .loading-pulse i {
                animation: spin 0.9s linear infinite;
                font-size: 1.3rem;
            }
        @@keyframes spin {
            from {
                transform: rotate(0deg);
            }
            to {
                transform: rotate(360deg);
            }
        }
        #tablePatients_filter input {
            border-radius: 10px;
            border: 1px solid var(--sc-sky-100);
            padding: 6px 12px;
        }
            #tablePatients_filter input:focus {
                border-color: var(--brand);
                box-shadow: 0 0 0 3px rgba(13,148,136,0.1);
                outline: none;
            }
        /* ── DataTables search / length / info / pagination ──
                   DataTables 1.13.7 (classic, non-Bootstrap build) ignores the
                   `layout:` option — these elements render with the library's
own
                   default markup. We relocate them into our filter-bar /
footer-bar
                   via initComplete (see script below) and style them here so
they
```

```
                   match the rest of the screen instead of looking like a
different
                   product bolted on. */
        .filter-bar-end {
            display: flex;
            align-items: center;
            gap: 10px;
            flex-wrap: wrap;
        }
        #tablePatients_filter {
            margin: 0;
        }
            #tablePatients_filter label {
                display: flex;
                align-items: center;
                gap: 8px;
                margin: 0;
                background: #fff;
                border: 1.5px solid var(--sc-sky-100);
                border-radius: 999px;
                padding: 7px 16px;
                transition: border-color .2s ease, box-shadow .2s ease;
            }
                #tablePatients_filter label:focus-within {
                    border-color: var(--sc-teal-500);
                    box-shadow: 0 0 0 3px rgba(15,155,142,.12);
                }
                #tablePatients_filter label i {
                    font-size: .9rem;
                    color: var(--sc-teal-600);
                    flex-shrink: 0;
                }
            #tablePatients_filter input {
                border: none !important;
                background: transparent !important;
                padding: 0 !important;
                font-size: .85rem;
                width: 200px;
                color: var(--sc-ink-900);
            }
                #tablePatients_filter input:focus {
                    box-shadow: none !important;
                }
        #tablePatients_length {
            margin: 0;
        }
            #tablePatients_length label {
                display: flex;
                align-items: center;
                gap: 8px;
                margin: 0;
                font-size: .78rem;
                font-weight: 700;
                color: var(--sc-ink-600);
                white-space: nowrap;
            }
```

```
            #tablePatients_length select {
                border: 1.5px solid var(--sc-sky-100);
                border-radius: 10px;
                padding: 6px 10px;
                font-size: .82rem;
                background: #fff;
                outline: none;
                cursor: pointer;
                transition: border-color .2s ease, box-shadow .2s ease;
            }
                #tablePatients_length select:focus {
                    border-color: var(--sc-teal-500);
                    box-shadow: 0 0 0 3px rgba(15,155,142,.12);
                }
        .table-footer-bar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 10px;
            padding: 12px 20px 16px;
            border-top: 1px solid var(--sc-sky-100);
        }
        #tablePatients_info {
            margin: 0;
            font-size: .78rem;
            color: var(--sc-ink-600);
            font-weight: 600;
        }
        #tablePatients_paginate {
            margin: 0;
            display: flex;
            align-items: center;
            gap: 2px;
        }
            #tablePatients_paginate .paginate_button {
                padding: 5px 12px;
                margin: 0 2px;
                border-radius: 8px;
                border: 1px solid transparent !important;
                background: transparent !important;
                color: var(--sc-ink-600) !important;
                cursor: pointer;
            }
                #tablePatients_paginate .paginate_button.current {
                    background: var(--brand) !important;
                    color: #fff !important;
                    border: 1px solid var(--brand) !important;
                }
#tablePatients_paginate .paginate_button:hover:not(.current):not(.disabled) {
                    background: var(--brand-light) !important;
                    border: 1px solid #99f6e4 !important;
                }
```

```
                #tablePatients_paginate .paginate_button.disabled {
```

```
                    opacity: .4;
                    cursor: default;
                }
        @@media (max-width: 640px) {
            #tablePatients_filter input {
                width: 140px;
            }
        }
    </style>
}
<section class=" mt-0" dir="rtl">
    <div class="row">
        <div class="col-md-12">
            <div class="patients-hero">
                <div class="hero-title">
                    <div class="hero-icon"><i class="bi
bi-people-fill"></i></div>
                    <div>
                        <h1>المرضىقائمة</h1>
                        <p>واحدمكانفيعاموتًقويمالمرضىملفاتإدارة</p>
                    </div>
                </div>
                <a asp-action="Create" asp-controller="Patients" class="btn-add-
patient">
                    <i class="bi bi-person-plus-fill"></i> جديدمريضإضافة
                </a>
            </div>
            <div class="row g-3 mb-3" id="statsRow">
                <div class="col-6 col-md-3">
                    <div class="stat-card stat-total">
                        <div class="stat-icon"><i class="bi
bi-people-fill"></i></div>
                        <div>
                            <span class="stat-value" data-target="0"
id="statTotal">0</span>
                            <span class="stat-label d-block fs-6"> إجمالي
المرضى</span>
                        </div>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="stat-card stat-general">
                        <div class="stat-icon"><i class="bi bi-app-
indicator"></i></div>
                        <div>
                            <span class="stat-value" data-target="0"
id="statGeneral">0</span>
                            <span class="stat-label d-block fs-6">عامعالج</span>
                        </div>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="stat-card stat-ortho">
                        <div class="stat-icon"><i class="bi
bi-braces"></i></div>
                        <div>
                            <span class="stat-value" data-target="0"
id="statOrtho">0</span>
                            <span class="stat-label d-block fs-6"> تًقويم
أسنان</span>
                        </div>
```

```
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="stat-card stat-both">
                        <div class="stat-icon"><i class="bi bi-heart-pulse-
fill"></i></div>
                        <div>
                            <span class="stat-value" data-target="0"
id="statBoth">0</span>
                            <span class="stat-label d-block fs-6"> كال
الملفين</span>
                        </div>
                    </div>
                </div>
            </div>
            <div class="card patients-card p-0">
                <div class="filter-bar">
                    <div class="pill-toggle" id="categoryFilterGroup">
                        <button type="button" class="pill-btn active" data-
category="">
                            <i class="bi bi-grid-fill"></i> الكل
                        </button>
                        <button type="button" class="pill-btn" data-
category="General">
                            <i class="bi bi-app-indicator"></i> عام
                        </button>
                        <button type="button" class="pill-btn" data-
category="Orthodontics">
                            <i class="bi bi-braces"></i> تًقويم
                        </button>
                    </div>
                    <div class="filter-bar-end" id="filterBarEnd"></div>
                </div>
                <div class="card-body p-0 position-relative">
                    <div class="table-loading-overlay" id="tableLoadingOverlay">
                        <div class="loading-pulse">
                            <i class="bi bi-arrow-repeat"></i>  تحميلجاري
البيانات...
                        </div>
                    </div>
                    <div class="table-responsive px-3 pb-3">
                        <table class="table table-hover align-middle display
nowrap" id="tablePatients" style="width:100%">
                            <thead>
                                <tr class="text-center">
                                    <th class="text-center fs-6">الكود</th>
                                    <th class="text-center fs-6">لسم</th>
                                    <th class="text-center fs-6">الهاتف</th>
                                    <th class="text-center fs-6">النوع</th>
                                    <th class="text-center fs-6">العمر</th>
                                    <th class="text-center fs-6">الملفنوع</th>
                                    <th class="text-center fs-6">الحالة</th>
                                    <th class="text-center fs-6">إجراءات</th>
                                </tr>
                            </thead>
                            <tbody></tbody>
                        </table>
                    </div>
                    <div class="table-footer-bar" id="tableFooterBar"></div>
                </div>
```

```
            </div>
```

```
        </div>
```

```
       </div>
</section>
@section Scripts {
    <script
src="https://cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js"></script>
    <script
src="https://cdn.datatables.net/responsive/2.5.0/js/dataTables.responsive.min.js
"></script>
    <script>
        $(document).ready(function () {
        var currentCategoryFilter = '';
        function animateCount(el, target) {
        var startTime = null;
        var duration = 900;
        function step(ts) {
        if (!startTime) startTime = ts;
        var progress = Math.min((ts - startTime) / duration, 1);
        el.text(Math.floor(progress * target));
        if (progress < 1) requestAnimationFrame(step);
        else el.text(target);
        }
        requestAnimationFrame(step);
        }
        function loadStats() {
        $.get('/Patients/GetPatientsStats', function (res) {
        animateCount($('#statTotal'), res.total || 0);
        animateCount($('#statGeneral'), res.general || 0);
        animateCount($('#statOrtho'), res.ortho || 0);
        animateCount($('#statBoth'), res.both || 0);
        });
        }
        loadStats();
        var table = $('#tablePatients').DataTable({
        processing: true,
        serverSide: true,
        responsive: true,
        autoWidth: false,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100, 200, 400, 1000],
        ordering: true,
        ajax: {
        url: '/Patients/GetPatientsData',
        type: 'POST',
        contentType: 'application/json',
        data: function (d) {
        d.categoryFilter = currentCategoryFilter;
        return JSON.stringify(d);
        },
        error: function (xhr, error, thrown) {
        console.error('DataTables error:', error, thrown);
        Swal.fire({ title: 'خطأ', text: 'الخادممنالبياناتتحميلفشل', icon:
'error', confirmButtonColor: '#0d9488' });
        }
        },
        columns: [
        {
        data: 'patientCode', name: 'patientCode', className: 'text-center fs-6',
        render: function (data) { return '<span class="badge bg-secondary"
style="font-family: Candara;">' + (data || '-') + '</span>'; }
        },
        {
```

```
        data: null, name: 'fullNameAr', className: 'fs-6',
        render: function (data) {
        var name = data.fullNameAr || '-';
        var initials = name.split(' ').filter(Boolean).slice(0, 2).map(function
(p) { return p[0]; }).join('');
        return '<div class="patient-name-cell">' +
        '<div class="patient-avatar">' + (initials || '?') + '</div>' +
        '<span class="link-dark fw-semibold">' + name + '</span>' +
        '</div>';
        }
        },
        {
        data: 'phone', name: 'phone', className: 'fs-6 text-center',
        render: function (data) { return data ? '<span style="font-family:
Candara; direction: ltr;">' + data + '</span>' : '-'; }
        },
        { data: 'genderBadge', name: 'genderName', className: 'text-center',
searchable: false },
        {
        data: 'age', name: 'age', className: 'text-center fs-6',
        render: function (data) { return data || '<span
class="text-muted">-</span>'; }
        },
        { data: 'categoryBadge', name: 'category', className: 'text-center',
orderable: false, searchable: false },
        { data: 'statusBadge', name: 'isActive', className: 'text-center',
searchable: false },
        {
        data: null, orderable: false, searchable: false, className: 'text-
center',
        render: function (data) {
        return `
        <div class="row-actions-cell">
        <a class="btn-row-session" title="المريضهذامعجديدسيشنبدء"
href="/Patients/NewSession?patientId=${data.id}">
        <i class="bi bi-clipboard-pulse"></i> سيشنبدء
        </a>
        <a class="action-icon-btn fs-5" title="التفاصيلعرض"
href="/Patients/Details/${data.id}">
        <i class="bi bi-eye"></i>
        </a>
        <a class="action-icon-btn fs-5" title="تعديل" href="/Patients/Edit/$
{data.id}">
        <i class="bi bi-pencil-square"></i>
        </a>
        <button type="button" class="action-icon-btn danger fs-5" title="حذف"
onclick="deletePatient(${data.id})">
        <i class="bi bi-trash3-fill"></i>
        </button>
        </div>`;
        }
        }
        ],
        layout: {
        topStart: { search: { placeholder: ' أوالكود،لسم،بابحث
الهاتف...' } },
        topEnd: { pageLength: { menu: [10, 25, 50, 100, 200, 400, 1000] } },
        bottomEnd: { paging: { buttons: 6 } }
        },
        language: {
        search: '<i class="bi bi-search"></i>',
        searchPlaceholder: 'هناابحث...',
        lengthMenu: '_MENU_',
        info: 'إظهار _START_ إلى _END_ أصلمن _TOTAL_ سجل',
```

```
        zeroRecords: 'سجلأيعلىيعثرلم',
        paginate: { first: 'الأول', previous: 'السابق', next: 'التالي', last:
'الأخير' },
        processing: ''
        },
        deferRender: true,
        drawCallback: function () {
        $('#tablePatients tbody tr').each(function () {
        $(this).css('animation', 'none');
        void this.offsetWidth;
        $(this).css('animation', '');
        });
        },
        initComplete: function () {
        // DataTables 1.13.7 (classic build) ignores the `layout:`
        // option above — it always renders search/length/info/
        // pagination using its own default markup, with default
        // IDs like #tablePatients_filter. We physically relocate
        // those elements into our own toolbar containers so they
        // sit next to the category pills / under the table
        // instead of floating in DataTables' default positions.
        $('#tablePatients_filter').appendTo('#filterBarEnd');
        $('#tablePatients_length').appendTo('#filterBarEnd');
        $('#tablePatients_info').appendTo('#tableFooterBar');
        $('#tablePatients_paginate').appendTo('#tableFooterBar');
        }
        });
        $('#tablePatients').on('processing.dt', function (e, settings,
processing) {
        $('#tableLoadingOverlay').toggleClass('show', processing);
        });
        $('.pill-btn').on('click', function () {
        $('.pill-btn').removeClass('active');
        $(this).addClass('active');
        currentCategoryFilter = $(this).data('category');
        table.ajax.reload();
        });
        });
        function deletePatient(id) {
        Swal.fire({
        title: 'متأكد؟أنتهل',
        text: 'النظاممنالمريضتعطيلسيتم',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#0d9488',
        cancelButtonColor: '#64748b',
        confirmButtonText: 'احذفنعم،!',
        cancelButtonText: 'إلغاء',
        reverseButtons: true
        }).then((result) => {
        if (result.isConfirmed) {
        $.ajax({
        url: '/Patients/Delete',
        type: 'POST',
        data: { id: id },
        headers: { 'RequestVerificationToken': $
('input[name="__RequestVerificationToken"]').val() },
        success: function (response) {
        if (response.success) {
        $('#tablePatients').DataTable().ajax.reload();
        Swal.fire({ title: 'الحذفتم!', text: response.message || ' تعطيلتم
```

```
بنجاحالمريض.', icon: 'success', confirmButtonColor: '#0d9488',
confirmButtonText: 'حسن[ا', timer: 2000, timerProgressBar: true });
        } else {
        Swal.fire({ title: 'خطأ', text: response.message || ' أثناءخطأحدث
الحذف', icon: 'error', confirmButtonColor: '#0d9488', confirmButtonText:
'حسن[ا' });
        }
        },
        error: function () {
        Swal.fire({ title: 'خطأ', text: 'بالخادملتصالفشل', icon: 'error',
confirmButtonColor: '#0d9488', confirmButtonText: 'حسن[ا' });
        }
        });
        }
        });
        }
    </script>
    @if (TempData[Helper.MsgType] != null)
    {
        <script>
            Swal.fire({
            title: '@TempData[Helper.Title]',
            text: '@Html.Raw(TempData[Helper.Msg])',
            confirmButtonText: 'حسن[ا',
            icon: '@(TempData[Helper.MsgType]?.ToString() == Helper.Success ?
"success" : "error")',
            confirmButtonColor: '#0d9488'
            });
        </script>
    }
}
```

```
//View / Patients / details.cshtml
@using Dental.Models.Patient
@using Dental.Modules.Patients.DTOs
@model Dental.Modules.Patients.ViewModels.PatientProfileViewModel
@{
    ViewData["Title"] = $"المريضملف — {Model.Patient.FullNameAr}";
    var patient = Model.Patient;
    var initials = "";
    if (!string.IsNullOrEmpty(patient.FullNameAr))
    {
        var parts = patient.FullNameAr.Split(' ',
StringSplitOptions.RemoveEmptyEntries);
        initials = string.Join("", parts.Take(2).Select(p => p[0]));
    }
    var genderIconClass = patient.Gender?.NameAr == "ذكر" ? "bi-gender-male" :
"bi-gender-female";
    var patientTypeClass = patient.PatientType switch
    {
        PatientType.Orthodontics => "ortho",
        PatientType.Both => "both",
        _ => "general"
    };
    var patientTypeLabel = patient.PatientType switch
    {
        PatientType.Orthodontics => "تًقويم",
        PatientType.Both => "تًقويم + عام",
        _ => "عام"
    };
}
```

```
<style>
    :root {
        --sc-teal-900: #0a4d4f;
        --sc-teal-700: #0d7377;
        --sc-teal-600: #0f9b8e;
        --sc-teal-500: #14b8a6;
        --sc-teal-100: #c7f2ea;
        --sc-sky-50: #eafbfc;
        --sc-sky-100: #dcf5f7;
        --sc-coral-300: #f6c6bc;
        --sc-ink-900: #0b2e30;
        --sc-ink-600: #4b6b6c;
        --brand: #0d9488;
        --brand-dark: #0f766e;
        --brand-light: #f0fdfa;
        --radius-lg: 18px;
        --radius-md: 12px;
        --shadow-soft: 0 1px 3px rgba(15, 23, 42, 0.06);
        --shadow-hover: 0 10px 26px rgba(10, 77, 79, 0.12);
        --shadow-card: 0 20px 50px rgba(10, 77, 79, .08);
    }
    body {
        background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
transparent 26%), var(--sc-sky-50);
    }
    /* ── Hero ── */
    .patient-hero {
        background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
        border-radius: var(--radius-lg);
        padding: 24px 28px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
        margin-bottom: 20px;
        flex-wrap: wrap;
        gap: 16px;
        position: relative;
        overflow: hidden;
    }
        .patient-hero::after {
            content: '';
            position: absolute;
            inset: 0;
            background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
            pointer-events: none;
        }
        .patient-hero .hero-content {
            display: flex;
            align-items: center;
            gap: 18px;
            position: relative;
            z-index: 1;
        }
        .patient-hero .hero-avatar {
            width: 64px;
```

```
            height: 64px;
            border-radius: 16px;
            background: rgba(255,255,255,.18);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.6rem;
            font-weight: 800;
            flex-shrink: 0;
            backdrop-filter: blur(6px);
            border: 2px solid rgba(255,255,255,.2);
        }
        .patient-hero h1 {
            font-size: 1.4rem;
            font-weight: 800;
            margin: 0;
            color: #fff;
        }
        .patient-hero .hero-meta {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-top: 8px;
        }
    .hero-meta-tag {
        display: flex;
        align-items: center;
        gap: 6px;
        background: rgba(255,255,255,.15);
        color: #fff;
        padding: 4px 12px;
        border-radius: 20px;
        font-size: 0.8rem;
        font-weight: 600;
        backdrop-filter: blur(4px);
        border: 1px solid rgba(255,255,255,.1);
    }
    .hero-actions {
        display: flex;
        gap: 10px;
        position: relative;
        z-index: 1;
        flex-wrap: wrap;
    }
    .btn-hero {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.88rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s ease;
        text-decoration: none;
        white-space: nowrap;
    }
```

```
    .btn-hero-edit {
        background: rgba(255,255,255,.9);
        color: var(--sc-teal-700);
        box-shadow: 0 4px 12px rgba(10, 40, 42, .15);
    }
        .btn-hero-edit:hover {
            background: #fff;
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(10, 40, 42, .22);
            color: var(--sc-teal-700);
        }
    .btn-hero-appt {
        background: rgba(255,255,255,.15);
        color: #fff;
        border: 1px solid rgba(255,255,255,.25);
        backdrop-filter: blur(4px);
    }
        .btn-hero-appt:hover {
            background: rgba(255,255,255,.25);
            transform: translateY(-2px);
        }
    .btn-hero-session {
        background: linear-gradient(135deg, #6366f1, #4f46e5);
        color: #fff;
        box-shadow: 0 4px 12px rgba(99,102,241,0.3);
    }
        .btn-hero-session:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(99,102,241,0.4);
            color: #fff;
        }
    /* ── Stats ── */
    .stats-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 14px;
        margin-bottom: 20px;
    }
    .stat-card-glass {
        background: rgba(255,255,255,.78);
        backdrop-filter: blur(14px);
        border-radius: 16px;
        padding: 18px 20px;
        display: flex;
        align-items: center;
        gap: 14px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: var(--shadow-soft);
        transition: all .25s ease;
    }
        .stat-card-glass:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow-hover);
        }
```

```
    .stat-icon-box {
        width: 46px;
        height: 46px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.15rem;
        flex-shrink: 0;
    }
        .stat-icon-box.teal {
            background: #f0fdfa;
            color: #0f766e;
        }
        .stat-icon-box.green {
            background: #f0fdf4;
            color: #166534;
        }
        .stat-icon-box.red {
            background: #fef2f2;
            color: #dc2626;
        }
        .stat-icon-box.blue {
            background: #eff6ff;
            color: #1d4ed8;
        }
    .stat-value-lg {
        display: block;
        font-size: 1.4rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        line-height: 1.1;
    }
    .stat-label-lg {
        font-size: 0.78rem;
        color: var(--sc-ink-600);
        font-weight: 600;
    }
    /* ── Tabs ── */
    .tabs-container {
        background: rgba(255,255,255,.82);
        backdrop-filter: blur(16px);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow-card);
        overflow: hidden;
        border: 1px solid rgba(255,255,255,.6);
        margin-bottom: 20px;
    }
    .tabs-pill-bar {
        display: flex;
        gap: 6px;
        padding: 14px 18px;
        border-bottom: 1px solid var(--sc-sky-100);
        overflow-x: auto;
        flex-wrap: nowrap;
    }
```

```
    .tab-pill {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 18px;
        border-radius: 999px;
        border: none;
        background: transparent;
        color: var(--sc-ink-600);
        font-weight: 700;
        font-size: 0.85rem;
        cursor: pointer;
        white-space: nowrap;
        transition: all 0.2s ease;
    }
        .tab-pill:hover {
            background: #fff;
            color: var(--brand-dark);
        }
        .tab-pill.active {
            background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
            color: #fff;
            box-shadow: 0 4px 10px rgba(13,148,136,0.28);
        }
        .tab-pill i {
            font-size: 0.95rem;
        }
    .tab-panels {
        padding: 20px;
    }
    .tab-panel {
        display: none;
        animation: panelFadeIn 0.35s ease;
    }
        .tab-panel.active {
            display: block;
        }
    @@keyframes panelFadeIn {
        from {
            opacity: 0;
            transform: translateY(10px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    /* ── Info Cards Grid ── */
    .info-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 16px;
    }
```

```
    .info-card-glass {
        background: rgba(255,255,255,.9);
        border-radius: 16px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: 0 1px 3px rgba(0,0,0,0.04);
        overflow: hidden;
        transition: all 0.25s ease;
        animation: cardFadeIn 0.5s ease forwards;
        opacity: 0;
    }
        .info-card-glass:hover {
            box-shadow: 0 8px 24px rgba(10, 77, 79, 0.08);
            transform: translateY(-2px);
        }
        .info-card-glass.full-width {
            grid-column: 1 / -1;
        }
    @@keyframes cardFadeIn {
        from {
            opacity: 0;
            transform: translateY(12px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .info-card-glass:nth-child(1) {
        animation-delay: .05s;
    }
    .info-card-glass:nth-child(2) {
        animation-delay: .1s;
    }
    .info-card-glass:nth-child(3) {
        animation-delay: .15s;
    }
    .info-card-glass:nth-child(4) {
        animation-delay: .2s;
    }
    .info-card-glass:nth-child(5) {
        animation-delay: .25s;
    }
    .card-header-glass {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 14px 18px;
        background: var(--sc-sky-50);
        border-bottom: 1px solid var(--sc-sky-100);
        font-weight: 800;
        font-size: 0.95rem;
        color: var(--sc-ink-900);
    }
```

```
        .card-header-glass i {
            color: var(--sc-teal-600);
            font-size: 1.1rem;
        }
    .card-body-glass {
        padding: 16px 18px;
    }
    .info-row-flex {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 10px 0;
        border-bottom: 1px solid var(--sc-sky-100);
    }
        .info-row-flex:last-child {
            border-bottom: none;
        }
    .info-label-sm {
        font-size: 0.85rem;
        color: var(--sc-ink-600);
        font-weight: 600;
    }
    .info-value-sm {
        font-size: 0.92rem;
        color: var(--sc-ink-900);
        font-weight: 700;
    }
        .info-value-sm.text-danger {
            color: #dc2626;
            font-weight: 800;
        }
    .blood-badge-new {
        background: #fee2e2;
        color: #dc2626;
        padding: 3px 10px;
        border-radius: 6px;
        font-size: 0.8rem;
        font-weight: 700;
    }
    .notes-text-new {
        color: #475569;
        line-height: 1.7;
        font-size: 0.92rem;
        margin: 0;
    }
    /* ── Timeline ── */
    .timeline-container {
        position: relative;
        padding-right: 24px;
    }
        .timeline-container::before {
            content: '';
            position: absolute;
```

```
            right: 8px;
            top: 0;
            bottom: 0;
            width: 2px;
            background: var(--sc-sky-100);
        }
    .timeline-node {
        position: relative;
        padding-bottom: 20px;
        padding-right: 20px;
    }
        .timeline-node::before {
            content: '';
            position: absolute;
            right: -20px;
            top: 4px;
            width: 10px;
            height: 10px;
            border-radius: 50%;
            background: var(--sc-teal-500);
            border: 2px solid #fff;
            box-shadow: 0 0 0 2px var(--sc-teal-500);
        }
    .timeline-date {
        font-size: 0.75rem;
        color: #94a3b8;
        font-weight: 700;
        margin-bottom: 4px;
    }
    .timeline-title {
        font-weight: 800;
        color: var(--sc-ink-900);
        font-size: 0.95rem;
    }
    .timeline-desc {
        color: var(--sc-ink-600);
        font-size: 0.85rem;
        margin-top: 2px;
    }
    /* ── Placeholder ── */
    .placeholder-glass {
        background: rgba(255,255,255,.9);
        border-radius: 16px;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: 0 1px 3px rgba(0,0,0,0.04);
        padding: 48px;
        text-align: center;
    }
    .placeholder-icon-glass {
        width: 72px;
        height: 72px;
        border-radius: 20px;
        background: var(--brand-light);
        color: var(--sc-teal-500);
        display: flex;
        align-items: center;
        justify-content: center;
```

```
        font-size: 2rem;
        margin: 0 auto 20px;
    }
    .placeholder-glass h3 {
        font-size: 1.2rem;
        font-weight: 800;
        color: var(--sc-ink-900);
        margin: 0 0 8px 0;
    }
    .placeholder-glass p {
        color: var(--sc-ink-600);
        font-size: 0.92rem;
        margin: 0 0 24px 0;
    }
    /* ── Payments ── */
    .pmt-summary-grid-new {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 14px;
        margin-bottom: 20px;
    }
    .pmt-summary-card-new {
        background: rgba(255,255,255,.9);
        border-radius: 14px;
        padding: 16px;
        border: 1px solid rgba(255,255,255,.6);
        text-align: center;
        box-shadow: 0 1px 3px rgba(0,0,0,0.04);
        transition: all 0.2s;
    }
        .pmt-summary-card-new:hover {
            transform: translateY(-2px);
            box-shadow: var(--shadow-hover);
        }
        .pmt-summary-card-new .pmt-label {
            font-size: .75rem;
            color: var(--sc-ink-600);
            font-weight: 700;
            margin-bottom: 6px;
        }
        .pmt-summary-card-new .pmt-value {
            font-size: 1.15rem;
            font-weight: 800;
            color: var(--sc-ink-900);
        }
        .pmt-summary-card-new.is-paid .pmt-value {
            color: #047857;
        }
        .pmt-summary-card-new.is-due .pmt-value {
            color: #b91c1c;
        }
    table.pmt-table-new {
        width: 100%;
        border-collapse: collapse;
```

```
        font-size: .87rem;
    }
        table.pmt-table-new th {
            text-align: right;
            color: var(--sc-ink-600);
            font-weight: 700;
            padding: 10px 8px;
            border-bottom: 2px solid var(--sc-sky-100);
        }
        table.pmt-table-new td {
            padding: 10px 8px;
            border-bottom: 1px solid var(--sc-sky-100);
        }
    .pmt-badge-paid-new {
        background: #d1fae5;
        color: #047857;
        padding: 2px 10px;
        border-radius: 6px;
        font-size: .72rem;
        font-weight: 700;
    }
    .pmt-badge-due-new {
        background: #fee2e2;
        color: #b91c1c;
        padding: 2px 10px;
        border-radius: 6px;
        font-size: .72rem;
        font-weight: 700;
    }
    .btn-pmt-void-new {
        background: none;
        border: none;
        color: #b91c1c;
        cursor: pointer;
        font-size: .78rem;
        transition: all 0.2s;
    }
        .btn-pmt-void-new:hover {
            color: #dc2626;
            transform: scale(1.05);
        }
    /* ── X-Rays Grid ── */
    .xray-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 16px;
    }
    .xray-card {
        background: rgba(255,255,255,.9);
        border-radius: 14px;
        overflow: hidden;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: 0 1px 3px rgba(0,0,0,0.04);
        transition: all 0.25s ease;
    }
```

```
        .xray-card:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow-hover);
        }
        .xray-card img {
            width: 100%;
            height: 160px;
            object-fit: cover;
            display: block;
        }
    .xray-card-body {
        padding: 12px;
    }
        .xray-card-body small {
            color: #94a3b8;
            font-size: 0.75rem;
            font-weight: 600;
        }
        .xray-card-body p {
            color: var(--sc-ink-900);
            font-weight: 700;
            font-size: 0.85rem;
            margin: 4px 0 0 0;
        }
    /* ── Documents ── */
    .doc-table-container {
        background: rgba(255,255,255,.9);
        border-radius: 16px;
        border: 1px solid rgba(255,255,255,.6);
        overflow: hidden;
    }
    /* ── Modals ── */
    .custom-modal-new {
        display: none;
        position: fixed;
        inset: 0;
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }
        .custom-modal-new.show {
            display: flex;
        }
    .modal-overlay-new {
        position: absolute;
        inset: 0;
        background: rgba(15,23,42,0.45);
        backdrop-filter: blur(4px);
    }
    .modal-box-new {
        position: relative;
        z-index: 1;
        width: 90%;
        max-width: 480px;
        background: #fff;
```

```
        border-radius: 20px;
        box-shadow: 0 25px 60px rgba(0,0,0,0.2);
        animation: modalSlideNew 0.25s ease;
        overflow: hidden;
    }
    @@keyframes modalSlideNew {
        from {
            opacity: 0;
            transform: translateY(20px) scale(0.98);
        }
        to {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
    }
    .modal-head-new {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 18px 24px;
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
    }
        .modal-head-new h5 {
            margin: 0;
            font-weight: 800;
            font-size: 1.1rem;
        }
    .modal-body-new {
        padding: 24px;
    }
    .modal-foot-new {
        padding: 0 24px 24px;
        display: flex;
        justify-content: flex-end;
        gap: 10px;
    }
    .form-label-custom-new {
        display: block;
        font-size: 0.82rem;
        font-weight: 700;
        color: #475569;
        margin-bottom: 6px;
    }
    .form-control-custom-new {
        width: 100%;
        padding: 10px 14px;
        border: 1.5px solid var(--sc-sky-100);
        border-radius: 10px;
        font-size: 0.95rem;
        color: var(--sc-ink-900);
        background: #fff;
        transition: all 0.2s;
        outline: none;
    }
```

```
        .form-control-custom-new:focus {
            border-color: var(--sc-teal-500);
            box-shadow: 0 0 0 3px rgba(20,184,166,0.1);
        }
    .btn-modal-new {
        padding: 10px 22px;
        border-radius: 10px;
        font-weight: 700;
        font-size: 0.9rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s;
    }
    .btn-modal-cancel-new {
        background: #f1f5f9;
        color: #475569;
    }
        .btn-modal-cancel-new:hover {
            background: #e2e8f0;
        }
    .btn-modal-save-new {
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
    }
        .btn-modal-save-new:hover {
            transform: translateY(-1px);
            box-shadow: 0 6px 16px rgba(13,148,136,0.3);
        }
    /* ── Responsive ── */
    @@media (max-width: 1200px) {
        .info-grid {
            grid-template-columns: repeat(2, 1fr);
        }
        .stats-grid {
            grid-template-columns: repeat(2, 1fr);
        }
        .pmt-summary-grid-new {
            grid-template-columns: repeat(2, 1fr);
        }
        .xray-grid {
            grid-template-columns: repeat(2, 1fr);
        }
    }
    @@media (max-width: 768px) {
        .patient-hero {
            flex-direction: column;
            text-align: center;
        }
            .patient-hero .hero-content {
                flex-direction: column;
            }
```

```
        .hero-actions {
            width: 100%;
            justify-content: center;
        }
        .info-grid {
            grid-template-columns: 1fr;
        }
        .stats-grid {
            grid-template-columns: repeat(2, 1fr);
        }
        .tabs-pill-bar {
            flex-wrap: wrap;
        }
        .xray-grid {
            grid-template-columns: 1fr;
        }
        .pmt-summary-grid-new {
            grid-template-columns: 1fr;
        }
    }
</style>
<div class="container-fluid" dir="rtl" style="padding: 12px;">
    <!-- 📁 Hero Header -->
    <div class="patient-hero">
        <div class="hero-content">
            <div class="hero-avatar">@initials</div>
            <div>
                <h1>@patient.FullNameAr</h1>
                <div class="hero-meta">
                    <span class="hero-meta-tag"><i class="bi bi-hash"></i>
@patient.PatientCode</span>
                    <span class="hero-meta-tag"><i class="bi
@genderIconClass"></i> @patient.Gender?.NameAr</span>
                    <span class="hero-meta-tag"><i class="bi bi-calendar"></i>
@(patient.Age) سنة</span>
                    <span class="hero-meta-tag"><i class="bi bi-droplet"></i>
@(patient.BloodGroup?.Name ?? "-")</span>
                    <span class="hero-meta-tag" style="background:
rgba(255,255,255,.25);"><i class="bi bi-app-indicator"></i>
@patientTypeLabel</span>
                </div>
            </div>
        </div>
        <div class="hero-actions">
            <a asp-action="Edit" asp-route-id="@patient.Id" class="btn-hero btn-
hero-edit">
                <i class="bi bi-pencil-square"></i> تعديل
            </a>
            <button type="button" class="btn-hero btn-hero-appt"
onclick="openApptModal()">
                <i class="bi bi-calendar-plus"></i> جديدموعد
            </button>
            @if (patient.PatientType == Dental.Models.Patient.PatientType.Both)
            {
                <div class="dropdown d-inline-block">
                    <button class="btn-hero btn-hero-session dropdown-toggle"
```

```
type="button" data-bs-toggle="dropdown">
                        <i class="bi bi-clipboard-pulse"></i> جديدسيشنبدء
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end" style="border-
radius: 12px; border: 1px solid var(--sc-sky-100); box-shadow: var(--shadow-
hover);">
```

```
                        <li><a class="dropdown-item" asp-action="NewSession"
asp-route-patientId="@patient.Id" asp-route-category="General">عامسيشن</a></li>
                        <li><a class="dropdown-item" asp-action="NewSession"
asp-route-patientId="@patient.Id" asp-route-category="Orthodontics"> سيشن
تًقويم</a></li>
```

```
                    </ul>
                </div>
            }
            else
            {
                <a asp-action="NewSession" asp-route-patientId="@patient.Id"
class="btn-hero btn-hero-session">
```

```
                    <i class="bi bi-clipboard-pulse"></i> جديدسيشنبدء
                </a>
            }
        </div>
    </div>
    <!-- 📁 Stats Bar -->
    <div class="stats-grid">
        <div class="stat-card-glass">
            <div class="stat-icon-box teal"><i class="bi
bi-calendar-check"></i></div>
            <div>
                <span class="stat-value-lg">@Model.VisitsCount</span>
                <span class="stat-label-lg d-block">الزيارات</span>
            </div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-icon-box green"><i class="bi
bi-cash-stack"></i></div>
            <div>
                <span
class="stat-value-lg">@Model.TotalPaid.ToString("N0")</span>
                <span class="stat-label-lg d-block">(م.ج) المدفوع</span>
            </div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-icon-box red"><i class="bi
bi-credit-card"></i></div>
            <div>
                <span
class="stat-value-lg">@Model.TotalDue.ToString("N0")</span>
                <span class="stat-label-lg d-block">المتبًقي</span>
            </div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-icon-box blue"><i class="bi
bi-clock-history"></i></div>
            <div>
                <span class="stat-value-lg">@Model.AppointmentsCount</span>
                <span class="stat-label-lg d-block">الًقادمةالمواعيد</span>
            </div>
        </div>
    </div>
    <!-- 📁 Tabs Container -->
    <div class="tabs-container">
```

```
        <div class="tabs-pill-bar">
            <button class="tab-pill active" onclick="switchTab('overview',
this)">
                <i class="bi bi-person-lines-fill"></i> عامةنظرة
            </button>
            <button class="tab-pill" onclick="switchTab('timeline', this)">
                <i class="bi bi-clock-history"></i> الطبيالتاريخ
            </button>
            <button class="tab-pill" onclick="switchTab('teeth', this)">
                <i class="bi bi-grid-3x3-gap"></i> Dental Chart
            </button>
            <button class="tab-pill" onclick="switchTab('plans', this)">
                <i class="bi bi-clipboard-check"></i> العالجخطط
            </button>
            <button class="tab-pill" onclick="switchTab('xrays', this)">
                <i class="bi bi-image"></i> الأشعة
            </button>
            <button class="tab-pill" onclick="switchTab('prescriptions', this)">
                <i class="bi bi-file-medical"></i> الروشتات
            </button>
            <button class="tab-pill" onclick="switchTab('payments', this)">
                <i class="bi bi-cash-coin"></i> المدفوعات
            </button>
            <button class="tab-pill" onclick="switchTab('docs', this)">
                <i class="bi bi-folder"></i> المستندات
            </button>
        </div>
```

```
        <div class="tab-panels">
```

```
            <!-- 📁 Tab: Overview -->
            <div class="tab-panel active" id="overview">
                <div class="info-grid">
                    <div class="info-card-glass">
                        <div class="card-header-glass"><i class="bi bi-person-
vcard"></i> الشخصيةالبيانات</div>
                        <div class="card-body-glass">
                            <div class="info-row-flex"><span class="info-label-
sm">لسم</span><span class="info-value-sm">@patient.FullNameAr</span></div>
                            <div class="info-row-flex"><span class="info-label-
sm">الميالدتاريخ</span><span class="info-value-
sm">@(patient.DateOfBirth?.ToString("dd/MM/yyyy") ?? "-")</span></div>
                            <div class="info-row-flex"><span class="info-label-
sm">الًقوميالرقم</span><span class="info-value-sm">@(patient.NationalId ??
"-")</span></div>
```

```
                            <div class="info-row-flex"><span class="info-label-
sm">الدمفصيلة</span><span class="info-value-sm"><span class="blood-badge-
new">@(patient.BloodGroup?.Name ?? "-")</span></span></div>
```

```
                            @* <div class="info-row-flex"><span class="info-
label-sm">المهنة</span><span class="info-value-sm">@(patient.Occupation ??
"-")</span></div> *@
```

```
                        </div>
                    </div>
                    <div class="info-card-glass">
                        <div class="card-header-glass"><i class="bi bi-
telephone-plus"></i> التواصلبيانات</div>
                        <div class="card-body-glass">
                            <div class="info-row-flex"><span class="info-label-
sm">الموبايل</span><span class="info-value-sm" dir="ltr">@(patient.Mobile ??
"-")</span></div>
                           @*  <div class="info-row-flex"><span class="info-
label-sm">التليفون</span><span class="info-value-sm"
dir="ltr">@(patient.Phone ?? "-")</span></div>
                            <div class="info-row-flex"><span class="info-label-
```

```
sm">البريد</span><span class="info-value-sm">@(patient.Email ??
"-")</span></div> *@
                            <div class="info-row-flex"><span class="info-label-
sm">العنوان</span><span class="info-value-sm">@(patient.Address ??
"-")</span></div>
```

```
                        </div>
                    </div>
                    <div class="info-card-glass">
                        <div class="card-header-glass"><i class="bi bi-heart-
pulse"></i> الطبيالتاريخ</div>
                        <div class="card-body-glass">
                            <div class="info-row-flex"><span class="info-label-
sm">الحساسية</span><span class="info-value-sm
@(string.IsNullOrEmpty(patient.Allergies) ? "" : "text-
danger")">@(patient.Allergies ?? "يوجدال")</span></div>
                            <div class="info-row-flex"><span class="info-label-
sm">مزمنةأمراض</span><span class="info-value-sm">@(patient.ChronicDiseases ??
"يوجدال")</span></div>
                            <div class="info-row-flex"><span class="info-label-
sm">الحاليةالأدوية</span><span class="info-value-
sm">@(patient.CurrentMedications ?? "يوجدال")</span></div>
                            <div class="info-row-flex"><span class="info-label-
sm">التدخين</span><span class="info-value-sm">@(patient.SmokingStatus ??
"-")</span></div>
```

```
                        </div>
                    </div>
                    <div class="info-card-glass">
                        <div class="card-header-glass"><i class="bi bi-journal-
text"></i> مالحظات</div>
                        <div class="card-body-glass">
                            <p class="notes-text-new">@(patient.Notes ?? "توجدال
مالحظات.")</p>
                        </div>
                    </div>
                    <div class="info-card-glass full-width">
                        <div class="card-header-glass"><i class="bi bi-
activity"></i> النشاطاتآخر</div>
                        <div class="card-body-glass">
                            <div class="timeline-container">
                                <div class="timeline-node">
                                    <div class="timeline-
date">@DateTime.Now.ToString("dd MMM yyyy")</div>
                                    <div class="timeline-title"> ملففتح
المريض</div>
                                    <div class="timeline-desc"> منالملففتحتم
التحكملوحة</div>
                                </div>
                                @if (patient.LastVisit.HasValue)
                                {
                                    <div class="timeline-node">
                                        <div class="timeline-
date">@patient.LastVisit.Value.ToString("dd MMM yyyy")</div>
                                        <div class="timeline-title"> آخر
زيارة</div>
                                        <div class="timeline-desc"> زيارة
عالجية</div>
                                    </div>
                                }
                            </div>
                        </div>
                    </div>
                </div>
```

```
            </div>
```

```
            <!-- 📁 Tab: Timeline -->
            <div class="tab-panel" id="timeline">
                <div class="info-card-glass">
                    <div class="card-header-glass"><i class="bi bi-clock-
history"></i> الكاملالطبيالتاريخ</div>
                    <div class="card-body-glass">
                        <div class="timeline-container">
                            @foreach (var visit in Model.Sessions ?? new
List<SessionDto>())
                            {
                                <div class="timeline-node">
                                    <div class="timeline-
date">@visit.SessionDate.ToString("dd MMM yyyy")</div>
                                    <div class="timeline-
title">@visit.ProcedureName</div>
                                    <div
class="timeline-desc">@visit.Notes</div>
                                </div>
                            }
                            @if ((Model.Sessions?.Count ?? 0) == 0)
                            {
                                <div class="placeholder-glass"
style="border:none; box-shadow:none; padding:32px;">
                                    <div class="placeholder-icon-glass"><i
class="bi bi-clock-history"></i></div>
                                    <h3>مسجلةنشاطاتتوجدال</h3>
                                    <p> والإجراءاتالزياراتجميعهناستظهر
والمدفوعات</p>
```

```
                                </div>
                            }
                        </div>
                    </div>
                </div>
            </div>
```

```
            <!-- 📁 Tab: Dental Chart -->
            <div class="tab-panel" id="teeth">
                @if (Model.DentalChart != null && Model.DentalChart.Teeth.Any())
                {
                    @await Html.PartialAsync("_DentalChartReadOnly",
Model.DentalChart)
                }
                else
                {
                    <div class="placeholder-glass">
                        <div class="placeholder-icon-glass"><i class="bi bi-
grid-3x3-gap"></i></div>
                        <h3>بعدمسجلأسنانرسميوجدال</h3>
                        <a asp-action="NewSession" asp-route-
patientId="@patient.Id" class="btn-hero btn-hero-session" style="display:inline-
flex;">
```

```
                            <i class="bi bi-clipboard-pulse"></i>  سيشنبدء
الرسملتسجيل
                        </a>
                    </div>
                }
            </div>
            <!-- 📁 Tab: Treatment Plans -->
            <div class="tab-panel" id="plans">
                <div class="placeholder-glass">
                    <div class="placeholder-icon-glass"><i class="bi bi-
clipboard-check"></i></div>
                    <h3>العالجخطط</h3>
```

```
                    <p>والمكتملةالمستًقبليةالعالجيةالخطط</p>
                </div>
            </div>
            <!-- 📁 Tab: X-Rays -->
            <div class="tab-panel" id="xrays">
                @if (Model.XRays.Any())
                {
                    <div class="xray-grid">
                        @foreach (var xray in Model.XRays)
                        {
                            <div class="xray-card">
                                <a href="@xray.ImageUrl" target="_blank">
                                    <img src="@xray.ImageUrl"
alt="@xray.FileName" />
                                </a>
                                <div class="xray-card-body">
<small>@xray.TakenDate.ToString("dd/MM/yyyy")</small>
                                    <p class="mb-0">@xray.Description</p>
                                </div>
                            </div>
                        }
                    </div>
                }
                else
                {
                    <div class="placeholder-glass">
                        <div class="placeholder-icon-glass"><i class="bi bi-
image"></i></div>
                        <h3>بعدمرفوعةأشعةتوجدال</h3>
                    </div>
                }
            </div>
            <!-- 📁 Tab: Prescriptions -->
            <div class="tab-panel" id="prescriptions">
                <div class="placeholder-glass">
                    <div class="placeholder-icon-glass"><i class="bi bi-file-
medical"></i></div>
```

```
                    <h3>الروشتات</h3>
                    <p>المكتوبةوالأدويةالروشتاتجميع</p>
                </div>
            </div>
```

```
            <!-- 📁 Tab: Payments -->
            <div class="tab-panel" id="payments">
                <div class="pmt-summary-grid-new">
                    <div class="pmt-summary-card-new">
                        <div class="pmt-label">المطلوبإجمالي</div>
                        <div class="pmt-value" id="pmtSumTotal">—</div>
                    </div>
                    <div class="pmt-summary-card-new">
                        <div class="pmt-label">الخصمإجمالي</div>
                        <div class="pmt-value" id="pmtSumDiscount">—</div>
                    </div>
                    <div class="pmt-summary-card-new is-paid">
                        <div class="pmt-label">المدفوعإجمالي</div>
                        <div class="pmt-value" id="pmtSumPaid">—</div>
                    </div>
                    <div class="pmt-summary-card-new is-due">
                        <div class="pmt-label">المتبًقي</div>
                        <div class="pmt-value" id="pmtSumRemaining">—</div>
                    </div>
```

```
                </div>
                <div class="info-card-glass full-width">
                    <div class="card-header-glass" style="display:flex; justify-
content:space-between; align-items:center;">
```

```
                        <span><i class="bi bi-cash-stack"></i>  سجل
الدفعات</span>
```

```
                        <button class="btn-hero btn-hero-session"
style="display:inline-flex; padding:8px 16px; font-size:0.8rem;"
onclick="openTabPaymentModal()">
                            <i class="bi bi-plus-lg"></i> جديدةدفعة
                        </button>
                    </div>
                    <div class="card-body-glass">
                        <table class="pmt-table-new">
                            <thead>
```

```
<tr><th>التاريخ</th><th>الزيارة</th><th>المبلغ</th><th>الطريًقة</th><th> رقم
الإيصال</th><th>مالحظات</th><th></th></tr>
```

```
                            </thead>
                            <tbody id="pmtPaymentsBody"></tbody>
                        </table>
                    </div>
                </div>
                <div class="info-card-glass full-width mb-3">
                    <div class="card-header-glass"><i class="bi bi-clipboard2-
data"></i> الزيارات</div>
                    <div class="card-body-glass">
                        <table class="pmt-table-new">
                            <thead>
```

```
<tr><th>التاريخ</th><th>النوع</th><th>الإجمالي</th><th>الخصم</th><th>المدفوع</
th><th>المتبًقي</th><th>الحالة</th></tr>
```

```
                            </thead>
```

```
                            <tbody id="pmtVisitsBody"></tbody>
                        </table>
                    </div>
                </div>
```

```
            </div>
```



```
<td>@doc.UploadedDate.ToString("dd/MM/yyyy")</td>
                                        <td>@doc.Description</td>
                                        <td><a href="@doc.FilePath"
```

```
target="_blank" class="btn-hero btn-hero-session" style="display:inline-
flex;padding:6px 12px; font-size:0.8rem;"><i class="bi
bi-download"></i></a></td>
```



```
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <div class="placeholder-glass">
                        <div class="placeholder-icon-glass"><i class="bi bi-
folder"></i></div>
                        <h3>بعدمرفوعةمستنداتتوجدال</h3>
                    </div>
                }
            </div>
        </div>
    </div>
</div>
<!-- 📁 Add Payment Modal -->
<div class="custom-modal-new" id="tabPayModal">
    <div class="modal-overlay-new" onclick="closeTabPaymentModal()"></div>
    <div class="modal-box-new">
        <div class="modal-head-new">
            <h5><i class="bi bi-cash-coin me-2"></i> جديدةدفعةتسجيل</h5>
            <button type="button" class="btn-close btn-close-white"
onclick="closeTabPaymentModal()"></button>
        </div>
        <div class="modal-body-new">
            <div class="mb-3">
                <label class="form-label-custom-new">الزيارة <span class="text-
danger">*</span></label>
                <select class="form-control-custom-new"
id="tabPayVisitId"></select>
            </div>
            <div class="mb-3">
                <label class="form-label-custom-new">المبلغ <span class="text-
danger">*</span></label>
                <input type="number" step="0.01" class="form-control-custom-new"
id="tabPayAmount" />
            </div>
            <div class="mb-3">
                <label class="form-label-custom-new">الدفعطريًقة</label>
                <select class="form-control-custom-new" id="tabPayMethod">
                    <option value="0">نًقدي</option>
                    <option value="1">ائتمانبطاقة</option>
                    <option value="2">خصمبطاقة</option>
                    <option value="3">بنكيتحويل</option>
                    <option value="4">تأمين</option>
                    <option value="5">تًقسيط</option>
                </select>
            </div>
            <div class="mb-3">
                <label class="form-label-custom-new">مالحظات</label>
                <textarea class="form-control-custom-new" id="tabPayDesc"
rows="2"></textarea>
            </div>
        </div>
        <div class="modal-foot-new">
            <button type="button" class="btn-modal-new btn-modal-cancel-new"
onclick="closeTabPaymentModal()">إلغاء</button>
            <button type="button" class="btn-modal-new btn-modal-save-new"
onclick="submitTabPayment()">الدفعةحفظ</button>
        </div>
    </div>
```

```
</div>
```

```
<!-- 📁 Appointment Modal -->
<div class="custom-modal-new" id="apptModal">
    <div class="modal-overlay-new" onclick="closeApptModal()"></div>
    <div class="modal-box-new">
        <div class="modal-head-new">
            <h5><i class="bi bi-calendar-plus me-2"></i> جديدموعد</h5>
            <button type="button" class="btn-close btn-close-white"
onclick="closeApptModal()"></button>
        </div>
        <div class="modal-body-new">
            <input type="hidden" id="apptPatientId" value="@patient.Id" />
            <div class="mb-3">
                <label class="form-label-custom-new">المريض</label>
                <input type="text" class="form-control-custom-new"
value="@patient.FullNameAr" disabled />
            </div>
            <div class="row g-3 mb-3">
                <div class="col-6">
                    <label class="form-label-custom-new">التاريخ <span
class="text-danger">*</span></label>
                    <input type="date" class="form-control-custom-new"
id="apptDate" />
                </div>
                <div class="col-6">
                    <label class="form-label-custom-new">الوقت <span
class="text-danger">*</span></label>
                    <input type="time" class="form-control-custom-new"
id="apptTime" />
                </div>
            </div>
            <div class="mb-3">
                <label class="form-label-custom-new">المالحظات / السبب</label>
                <textarea class="form-control-custom-new" id="apptReason"
rows="2" placeholder="متابعةتاج،تركيب :مثال..."></textarea>
            </div>
            <div class="mb-3">
                <label class="form-label-custom-new">المدة</label>
                <select class="form-control-custom-new" id="apptDuration">
                    <option value="15">15 دقيًقة</option>
                    <option value="30" selected>30 دقيًقة</option>
                    <option value="45">45 دقيًقة</option>
                    <option value="60">ساعة</option>
                    <option value="90">ونصساعة</option>
                </select>
            </div>
        </div>
        <div class="modal-foot-new">
            <button type="button" class="btn-modal-new btn-modal-cancel-new"
onclick="closeApptModal()">إلغاء</button>
            <button type="button" class="btn-modal-new btn-modal-save-new"
onclick="saveAppt()">
                <i class="bi bi-check-lg"></i> الموعدحفظ
            </button>
        </div>
    </div>
</div>
@section Scripts {
    <script>
        function switchTab(tabId, btn) {
            document.querySelectorAll('.tab-pill').forEach(b =>
b.classList.remove('active'));
```

```
            document.querySelectorAll('.tab-panel').forEach(c =>
c.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById(tabId).classList.add('active');
        }
        // ── Payments Tab Logic ──
        let pmtLoaded = false;
        let pmtVisits = [];
        function money(v) {
            return Number(v).toLocaleString('ar-EG', { minimumFractionDigits: 2,
maximumFractionDigits: 2 });
        }
        async function loadPaymentsTab() {
            try {
                const res = await fetch(`/Payments/GetStatement?
patientId=@patient.Id`);
                const result = await res.json();
                if (!result.success) return;
                renderPaymentsTab(result.data);
                pmtLoaded = true;
            } catch (err) {
                console.error('Failed to load payments tab', err);
            }
        }
        function renderPaymentsTab(data) {
            pmtVisits = data.visits;
```

```
            document.getElementById('pmtSumTotal').textContent =
money(data.totalAmount);
            document.getElementById('pmtSumDiscount').textContent =
money(data.totalDiscount);
            document.getElementById('pmtSumPaid').textContent =
money(data.totalPaid);
            document.getElementById('pmtSumRemaining').textContent =
money(data.totalRemaining);
```

```
            document.getElementById('pmtVisitsBody').innerHTML =
data.visits.length ? data.visits.map(v => `
                <tr>
                    <td>${new
Date(v.visitDate).toLocaleDateString('ar-EG')}</td>
                    <td>${v.category === 2 ? 'عام' : 'تًقويم'}</td>
                    <td>${money(v.totalAmount)}</td>
                    <td>${money(v.discount)}</td>
                    <td>${money(v.paidAmount)}</td>
                    <td>${money(v.remaining)}</td>
                    <td>${v.isFullyPaid ? '<span class="pmt-badge-paid-
new">مسدد</span>' : '<span class="pmt-badge-due-new">متبًقي</span>'}</td>
                </tr>`).join('') : '<tr><td colspan="7" style="text-
align:center; color:#94a3b8; padding:16px;">زياراتتوجدال</td></tr>';
```

```
            document.getElementById('pmtPaymentsBody').innerHTML =
data.payments.length ? data.payments.map(p => `
                <tr>
                    <td>${new Date(p.paymentDate).toLocaleDateString('ar-EG')}
</td>
                    <td>${new
Date(p.visitDate).toLocaleDateString('ar-EG')}</td>
                    <td>${money(p.amount)}</td>
                    <td>${p.methodText}</td>
```

```
                    <td style="direction:ltr; text-align:right;">${p.receiptNo
|| '—'}</td>
                    <td>${p.description || ''}</td>
                    <td><button class="btn-pmt-void-new"
onclick="voidTabPayment(${p.id})"><i class="bi bi-x-circle"></i>
إلغاء</button></td>
                </tr>`).join('') : '<tr><td colspan="7" style="text-
align:center; color:#94a3b8; padding:16px;">دفعاتتوجدال</td></tr>';
```

```
            const dueVisits = data.visits.filter(v => v.remaining > 0);
            document.getElementById('tabPayVisitId').innerHTML =
dueVisits.length
                ? dueVisits.map(v => `<option value="${v.visitId}">${new
Date(v.visitDate).toLocaleDateString('ar-EG')} — متبًقي
${money(v.remaining)}</option>`).join('')
                : '<option value="">متبًقيمبلغعليهازياراتتوجدال</option>';
        }
        function openTabPaymentModal() {
            document.getElementById('tabPayAmount').value = '';
            document.getElementById('tabPayDesc').value = '';
            document.getElementById('tabPayModal').classList.add('show');
        }
        function closeTabPaymentModal() {
            document.getElementById('tabPayModal').classList.remove('show');
        }
        async function submitTabPayment() {
            const visitId =
parseInt(document.getElementById('tabPayVisitId').value);
            const amount =
parseFloat(document.getElementById('tabPayAmount').value);
            const method =
parseInt(document.getElementById('tabPayMethod').value);
            const description = document.getElementById('tabPayDesc').value;
            if (!visitId || !amount || amount <= 0) {
                Swal.fire({ title: 'مطلوب', text: ' مبلغوادخلالزيارةاختر
صحيح', icon: 'warning', confirmButtonColor: '#14b8a6' });
                return;
            }
            try {
                const res = await fetch('/Payments/AddPayment', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ VisitId: visitId, Amount: amount,
Method: method, Description: description })
                });
                const result = await res.json();
                if (result.success) {
                    closeTabPaymentModal();
                    Swal.fire({ title: 'تم!', text: result.message, icon:
'success', confirmButtonColor: '#14b8a6', timer: 1500, showConfirmButton:
false });
                    loadPaymentsTab();
                } else {
                    Swal.fire({ title: 'الحفظتعذر', text: result.message, icon:
'error' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'بالسيرفرلتصالفيمشكلة',
icon: 'error' });
            }
```

```
        }
        async function voidTabPayment(paymentId) {
            const confirmResult = await Swal.fire({
                title: 'الإلغاءتأكيد',
                text: ' .تلًقائي[االزيارةرصيدوتصحيحديالدفعةإلغاءهيتم
متأكد؟',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'إلغاءأيوه،',
                cancelButtonText: 'رجوع',
                confirmButtonColor: '#b91c1c'
            });
            if (!confirmResult.isConfirmed) return;
            const res = await fetch(`/Payments/VoidPayment?paymentId=$
{paymentId}`, { method: 'POST' });
            const result = await res.json();
            if (result.success) {
                Swal.fire({ title: 'تم', text: result.message, icon: 'success',
timer: 1500, showConfirmButton: false });
                loadPaymentsTab();
            } else {
                Swal.fire({ title: 'التنفيذتعذر', text: result.message, icon:
'error' });
            }
        }
        document.addEventListener('DOMContentLoaded', loadPaymentsTab);
        // ── Appointment Modal ──
        function openApptModal() {
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);
            document.getElementById('apptDate').value =
tomorrow.toISOString().split('T')[0];
            document.getElementById('apptTime').value = '09:00';
            document.getElementById('apptModal').classList.add('show');
            document.body.style.overflow = 'hidden';
        }
        function closeApptModal() {
            document.getElementById('apptModal').classList.remove('show');
            document.body.style.overflow = '';
        }
        async function saveAppt() {
            const patientId =
parseInt(document.getElementById('apptPatientId').value);
            const date = document.getElementById('apptDate').value;
            const time = document.getElementById('apptTime').value;
            const reason = document.getElementById('apptReason').value;
            const duration =
parseInt(document.getElementById('apptDuration').value);
            if (!date || !time) {
                Swal.fire({ title: 'مطلوب', text: 'والوقتالتاريخاختر', icon:
'warning', confirmButtonColor: '#14b8a6' });
                return;
            }
            const appointmentDate = new Date(`${date}T${time}`);
            try {
                const res = await fetch('/Appointments/Create', {
```

```
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        PatientId: patientId,
                        AppointmentDate: appointmentDate.toISOString(),
                        DurationMinutes: duration,
                        Reason: reason
                    })
                });
                const result = await res.json();
                if (result.success) {
                    closeApptModal();
                    Swal.fire({ title: 'تم!', text: 'بنجاحالموعدإنشاءتم',
icon: 'success', confirmButtonColor: '#14b8a6', timer: 2000, showConfirmButton:
false });
                    setTimeout(() => location.reload(), 1500);
                } else {
                    Swal.fire({ title: 'خطأ', text: result.message || ' فشل
الحفظ', icon: 'error', confirmButtonColor: '#14b8a6' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'لتصالفيمشكلة', icon:
'error', confirmButtonColor: '#14b8a6' });
            }
        }
        document.addEventListener('keydown', e => { if (e.key === 'Escape')
{ closeApptModal(); closeTabPaymentModal(); }});
    </script>
}
```

```
// View / Payments / index
```

```
@{
    ViewData["Title"] = "والمدفوعاتالفواتير";
    Layout = "~/Views/Shared/_Layout.cshtml";
}
<style>
    :root {
        --sc-teal-900: #0a4d4f;
        --sc-teal-700: #0d7377;
        --sc-teal-600: #0f9b8e;
        --sc-teal-500: #14b8a6;
        --sc-teal-100: #c7f2ea;
        --sc-sky-50: #eafbfc;
        --sc-sky-100: #dcf5f7;
        --sc-coral-300: #f6c6bc;
        --sc-ink-900: #0b2e30;
        --sc-ink-600: #4b6b6c;
        --brand: #0d9488;
        --brand-dark: #0f766e;
        --brand-light: #f0fdfa;
        --radius-lg: 18px;
        --radius-md: 12px;
        --shadow-soft: 0 1px 3px rgba(15, 23, 42, 0.06);
        --shadow-hover: 0 10px 26px rgba(10, 77, 79, 0.12);
        --shadow-card: 0 20px 50px rgba(10, 77, 79, .08);
    }
```

```
    body {
        background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
```

```
transparent 26%), var(--sc-sky-50);
    }
    /* ── Hero ── */
    .payments-hero {
        background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
        border-radius: var(--radius-lg);
        padding: 28px 32px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
        margin-bottom: 28px;
        flex-wrap: wrap;
        gap: 16px;
        position: relative;
        overflow: hidden;
    }
        .payments-hero::after {
            content: '';
            position: absolute;
            inset: 0;
            background: radial-gradient(circle at 92% -20%,
rgba(255,255,255,.32), transparent 55%);
            pointer-events: none;
        }
        .payments-hero .hero-content {
            display: flex;
            align-items: center;
            gap: 18px;
            position: relative;
            z-index: 1;
        }
        .payments-hero .hero-icon {
            width: 56px;
            height: 56px;
            background: rgba(255,255,255,.18);
            color: #fff;
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.6rem;
            flex-shrink: 0;
            backdrop-filter: blur(6px);
            border: 1px solid rgba(255,255,255,.15);
        }
        .payments-hero h1 {
            font-size: 1.3rem;
            font-weight: 800;
            margin: 0;
            color: #fff;
        }
        .payments-hero p {
            font-size: 0.85rem;
            margin: 4px 0 0;
            color: rgba(255,255,255,.9);
            font-weight: 600;
```

```
        }
    /* ── Search Shell ── */
    .search-shell-glass {
        max-width: 600px;
        margin: 0 auto;
        background: rgba(255,255,255,.82);
        backdrop-filter: blur(16px);
        border-radius: var(--radius-lg);
        border: 1px solid rgba(255,255,255,.6);
        padding: 40px;
        box-shadow: var(--shadow-card);
        text-align: center;
        animation: shellFadeIn 0.6s ease forwards;
        opacity: 0;
        transform: translateY(20px);
    }
    @@keyframes shellFadeIn {
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .search-shell-glass .shell-icon {
        width: 72px;
        height: 72px;
        border-radius: 20px;
        background: linear-gradient(135deg, var(--sc-teal-500), var(--sc-
teal-700));
        color: #fff;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
        margin: 0 auto 20px;
        box-shadow: 0 8px 20px rgba(13,148,136,0.25);
    }
    .search-shell-glass h3 {
        font-weight: 800;
        color: var(--sc-ink-900);
        margin-bottom: 8px;
        font-size: 1.3rem;
    }
    .search-shell-glass p {
        color: var(--sc-ink-600);
        font-size: .9rem;
        margin-bottom: 28px;
        line-height: 1.6;
    }
    /* ── Search Input ── */
    .search-input-wrapper {
        position: relative;
        margin-bottom: 16px;
    }
    .search-input-glass {
        width: 100%;
        border: 1.5px solid var(--sc-sky-100);
        border-radius: 14px;
```

```
        padding: 14px 18px 14px 50px;
        font-size: 1rem;
        color: var(--sc-ink-900);
        background: #fff;
        transition: all 0.25s ease;
        outline: none;
        font-weight: 600;
    }
        .search-input-glass:focus {
            border-color: var(--sc-teal-500);
            box-shadow: 0 0 0 4px rgba(20,184,166,0.1);
        }
        .search-input-glass::placeholder {
            color: #94a3b8;
            font-weight: 500;
        }
    .search-input-icon {
        position: absolute;
        left: 18px;
        top: 50%;
        transform: translateY(-50%);
        color: var(--sc-teal-600);
        font-size: 1.2rem;
        pointer-events: none;
    }
    /* ── Results Box ── */
    .results-box-glass {
        text-align: right;
        margin-top: 12px;
        border-radius: 14px;
        overflow: hidden;
        border: 1.5px solid var(--sc-sky-100);
        display: none;
        background: #fff;
        box-shadow: 0 10px 30px rgba(0,0,0,0.06);
        animation: resultsSlideDown 0.25s ease;
    }
    @@keyframes resultsSlideDown {
        from {
            opacity: 0;
            transform: translateY(-8px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .result-item-glass {
        padding: 14px 18px;
        cursor: pointer;
        border-bottom: 1px solid var(--sc-sky-100);
        transition: all 0.15s ease;
        display: flex;
        align-items: center;
        justify-content: space-between;
        font-weight: 700;
        color: var(--sc-ink-900);
```

```
        font-size: 0.95rem;
    }
        .result-item-glass:hover {
            background: var(--brand-light);
            color: var(--brand-dark);
            padding-right: 24px;
        }
        .result-item-glass:last-child {
            border-bottom: none;
        }
        .result-item-glass .result-meta {
            font-size: 0.8rem;
            color: var(--sc-ink-600);
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 6px;
        }
        .result-item-glass:hover .result-meta {
            color: var(--sc-teal-600);
        }
    .result-empty {
        padding: 20px;
        text-align: center;
        color: #94a3b8;
        font-weight: 600;
    }
        .result-empty i {
            display: block;
            font-size: 2rem;
            margin-bottom: 8px;
            color: var(--sc-sky-100);
        }
    /* ── Loading State ── */
    .search-loading {
        display: none;
        align-items: center;
        justify-content: center;
        gap: 10px;
        padding: 16px;
        color: var(--sc-teal-600);
        font-weight: 700;
        font-size: 0.9rem;
    }
        .search-loading.show {
            display: flex;
        }
        .search-loading i {
            animation: spin 0.9s linear infinite;
            font-size: 1.2rem;
        }
    @@keyframes spin {
        from {
            transform: rotate(0deg);
```

```
        }
        to {
            transform: rotate(360deg);
        }
    }
    /* ── Quick Stats (Optional decorative) ── */
    .quick-stats-row {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 14px;
        max-width: 600px;
        margin: 0 auto 24px;
    }
    .quick-stat-card {
        background: rgba(255,255,255,.78);
        backdrop-filter: blur(14px);
        border-radius: 14px;
        padding: 16px;
        text-align: center;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: var(--shadow-soft);
        transition: all .25s ease;
    }
        .quick-stat-card:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow-hover);
        }
    .quick-stat-value {
        font-size: 1.3rem;
        font-weight: 800;
        color: var(--sc-ink-900);
    }
    .quick-stat-label {
        font-size: 0.75rem;
        color: var(--sc-ink-600);
        font-weight: 600;
        margin-top: 4px;
    }
    /* ── Responsive ── */
    @@media (max-width: 768px) {
        .payments-hero {
            flex-direction: column;
            text-align: center;
            padding: 22px;
        }
            .payments-hero .hero-content {
                flex-direction: column;
            }
        .search-shell-glass {
            padding: 28px 20px;
            margin: 0 12px;
        }
        .quick-stats-row {
            grid-template-columns: 1fr;
```

```
        }
    }
</style>
<!-- 📁 Hero -->
<div class="payments-hero">
    <div class="hero-content">
        <div class="hero-icon"><i class="bi bi-receipt-cutoff"></i></div>
        <div>
            <h1>والمدفوعاتالفواتير</h1>
            <p>والدفعاتالكاملالحسابكشفواستعرضالمريضعنابحث</p>
        </div>
    </div>
</div>
<!-- 📁 Quick Stats (Placeholder / Static for visual balance) -->
<div class="quick-stats-row">
    <div class="quick-stat-card">
        <div class="quick-stat-value"><i class="bi bi-receipt" style="color:
var(--sc-teal-600);"></i></div>
        <div class="quick-stat-label">مفصلحسابكشف</div>
    </div>
    <div class="quick-stat-card">
        <div class="quick-stat-value"><i class="bi bi-cash-stack" style="color:
#166534;"></i></div>
        <div class="quick-stat-label">المدفوعاتتتبع</div>
    </div>
    <div class="quick-stat-card">
        <div class="quick-stat-value"><i class="bi bi-graph-up" style="color:
#1d4ed8;"></i></div>
        <div class="quick-stat-label">والمتبًقيالأرصدة</div>
    </div>
</div>
<!-- 📁 Search Shell -->
<div class="search-shell-glass">
    <div class="shell-icon"><i class="bi bi-search-heart"></i></div>
    <h3>مريضحسابكشف</h3>
    <p>بتاعهالكاملالحسابكشفتشوفعشانالتليفونرقمأوالمريضباسمابحث</p>
    <div class="search-input-wrapper">
        <i class="bi bi-search search-input-icon"></i>
        <input type="text" id="patientSearch" class="search-input-glass"
placeholder="التليفونرقمأوالمريضاسماكتب..." autocomplete="off" />
    </div>
    <div class="search-loading" id="searchLoading">
        <i class="bi bi-arrow-repeat"></i> البحثجاري...
    </div>
    <div class="results-box-glass" id="resultsBox"></div>
</div>
@section Scripts {
    <script>
        let searchTimeout;
        const searchInput = document.getElementById('patientSearch');
        const resultsBox = document.getElementById('resultsBox');
        const searchLoading = document.getElementById('searchLoading');
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const term = this.value.trim();
```

```
            resultsBox.style.display = 'none';
            searchLoading.classList.remove('show');
            if (term.length < 2) {
                return;
            }
            searchLoading.classList.add('show');
            searchTimeout = setTimeout(() => doSearch(term), 400);
        });
        // برهيدوسلماالنتائجإخفاء
        document.addEventListener('click', function(e) {
            if (!e.target.closest('.search-shell-glass')) {
                resultsBox.style.display = 'none';
            }
        });
        async function doSearch(term) {
            try {
                const res = await fetch(`/Patients/Search?term=$
{encodeURIComponent(term)}`);
                const list = await res.json();
                searchLoading.classList.remove('show');
                if (!list.length) {
                    resultsBox.innerHTML = `
                        <div class="result-empty">
                            <i class="bi bi-inbox"></i>
مطابًقةنتائجتوجدال
                        </div>`;
                    resultsBox.style.display = 'block';
                    return;
                }
                resultsBox.innerHTML = '';
                list.forEach(function (p, index) {
                    const item = document.createElement('div');
                    item.className = 'result-item-glass';
                    item.style.animationDelay = `${index * 0.03}s`;
                    item.innerHTML = `
                        <span>${p.fullNameAr}</span>
                        <span class="result-meta"><i class="bi bi-telephone-
fill"></i> ${p.phone || '—'}</span>
                    `;
                    item.addEventListener('click', function () {
                        window.location.href = `/Payments/Statement?patientId=$
{p.id}`;
                    });
                    resultsBox.appendChild(item);
                });
                resultsBox.style.display = 'block';
            } catch (err) {
                searchLoading.classList.remove('show');
                console.error('Search failed', err);
                resultsBox.innerHTML = `
                    <div class="result-empty">
                        <i class="bi bi-wifi-off"></i>
لتصالفيخطأحدث
                    </div>`;
                resultsBox.style.display = 'block';
            }
```

```
        }
    </script>
}
```

```
// View / PAyments / statement
```

```
@{
    ViewData["Title"] = "المريضحسابكشف";
    Layout = "~/Views/Shared/_Layout.cshtml";
    var patientId = ViewBag.PatientId;
}
<style>
    :root {
        --sc-teal-900: #0a4d4f;
        --sc-teal-700: #0d7377;
        --sc-teal-600: #0f9b8e;
        --sc-teal-500: #14b8a6;
        --sc-teal-100: #c7f2ea;
        --sc-sky-50: #eafbfc;
        --sc-sky-100: #dcf5f7;
        --sc-coral-300: #f6c6bc;
        --sc-ink-900: #0b2e30;
        --sc-ink-600: #4b6b6c;
        --brand: #0d9488;
        --brand-dark: #0f766e;
        --brand-light: #f0fdfa;
        --radius-lg: 18px;
        --radius-md: 12px;
        --shadow-soft: 0 1px 3px rgba(15, 23, 42, 0.06);
        --shadow-hover: 0 10px 26px rgba(10, 77, 79, 0.12);
        --shadow-card: 0 20px 50px rgba(10, 77, 79, .08);
    }
    body {
        background: radial-gradient(circle at 6% 4%, var(--sc-teal-100) 0%,
transparent 38%), radial-gradient(circle at 97% 92%, var(--sc-coral-300) 0%,
transparent 26%), var(--sc-sky-50);
    }
    /* ── Hero ── */
    .statement-hero {
        background: linear-gradient(135deg, var(--sc-teal-700), var(--sc-
teal-500));
        border-radius: var(--radius-lg);
        padding: 24px 28px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        box-shadow: 0 14px 34px rgba(13, 115, 119, .28);
        margin-bottom: 20px;
        flex-wrap: wrap;
        gap: 16px;
        position: relative;
        overflow: hidden;
    }
        .statement-hero::after {
            content: '';
            position: absolute;
            inset: 0;
            background: radial-gradient(circle at 92% -20%,
```

```
rgba(255,255,255,.32), transparent 55%);
            pointer-events: none;
        }
        .statement-hero .hero-content {
            display: flex;
            align-items: center;
            gap: 18px;
            position: relative;
            z-index: 1;
        }
        .statement-hero .hero-avatar {
            width: 56px;
            height: 56px;
            border-radius: 16px;
            background: rgba(255,255,255,.18);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            font-weight: 800;
            flex-shrink: 0;
            backdrop-filter: blur(6px);
            border: 2px solid rgba(255,255,255,.2);
        }
        .statement-hero h1 {
            font-size: 1.25rem;
            font-weight: 800;
            margin: 0;
            color: #fff;
        }
        .statement-hero .hero-meta {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-top: 6px;
        }
    .hero-meta-tag {
        display: flex;
        align-items: center;
        gap: 6px;
        background: rgba(255,255,255,.15);
        color: #fff;
        padding: 4px 12px;
        border-radius: 20px;
        font-size: 0.78rem;
        font-weight: 600;
        backdrop-filter: blur(4px);
        border: 1px solid rgba(255,255,255,.1);
    }
    .hero-actions {
        display: flex;
        gap: 10px;
        position: relative;
        z-index: 1;
        flex-wrap: wrap;
    }
```

```
    .btn-hero {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.88rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s ease;
        text-decoration: none;
        white-space: nowrap;
    }
    .btn-hero-back {
        background: rgba(255,255,255,.9);
        color: var(--sc-teal-700);
        box-shadow: 0 4px 12px rgba(10, 40, 42, .15);
    }
        .btn-hero-back:hover {
            background: #fff;
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(10, 40, 42, .22);
            color: var(--sc-teal-700);
        }
    .btn-hero-print {
        background: rgba(255,255,255,.15);
        color: #fff;
        border: 1px solid rgba(255,255,255,.25);
        backdrop-filter: blur(4px);
    }
        .btn-hero-print:hover {
            background: rgba(255,255,255,.25);
            transform: translateY(-2px);
        }
    /* ── Stats ── */
    .stats-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 14px;
        margin-bottom: 20px;
    }
    .stat-card-glass {
        background: rgba(255,255,255,.78);
        backdrop-filter: blur(14px);
        border-radius: 16px;
        padding: 20px;
        text-align: center;
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: var(--shadow-soft);
        transition: all .25s ease;
        animation: cardPopIn 0.5s ease forwards;
        opacity: 0;
        transform: translateY(15px);
    }
        .stat-card-glass:hover {
            transform: translateY(-3px);
```

```
            box-shadow: var(--shadow-hover);
        }
        .stat-card-glass:nth-child(1) {
            animation-delay: .05s;
        }
        .stat-card-glass:nth-child(2) {
            animation-delay: .1s;
        }
        .stat-card-glass:nth-child(3) {
            animation-delay: .15s;
        }
        .stat-card-glass:nth-child(4) {
            animation-delay: .2s;
        }
    @@keyframes cardPopIn {
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .stat-card-glass .stat-label {
        font-size: .78rem;
        color: var(--sc-ink-600);
        font-weight: 700;
        margin-bottom: 8px;
    }
    .stat-card-glass .stat-value {
        font-size: 1.5rem;
        font-weight: 800;
        color: var(--sc-ink-900);
    }
    .stat-card-glass.paid .stat-value {
        color: #047857;
    }
    .stat-card-glass.due .stat-value {
        color: #b91c1c;
    }
    /* ── Section Shells ── */
    .section-shell {
        background: rgba(255,255,255,.82);
        backdrop-filter: blur(16px);
        border-radius: var(--radius-lg);
        border: 1px solid rgba(255,255,255,.6);
        box-shadow: var(--shadow-card);
        overflow: hidden;
        margin-bottom: 20px;
        animation: sectionFadeIn 0.5s ease forwards;
        opacity: 0;
        transform: translateY(12px);
    }
        .section-shell:nth-of-type(1) {
            animation-delay: .25s;
        }
```

```
        .section-shell:nth-of-type(2) {
            animation-delay: .35s;
        }
    @@keyframes sectionFadeIn {
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .section-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 16px 20px;
        background: var(--sc-sky-50);
        border-bottom: 1px solid var(--sc-sky-100);
    }
        .section-head h5 {
            font-weight: 800;
            font-size: 0.95rem;
            color: var(--sc-ink-900);
            margin: 0;
            display: flex;
            align-items: center;
            gap: 10px;
        }
            .section-head h5 i {
                color: var(--sc-teal-600);
                font-size: 1.1rem;
            }
    .section-body {
        padding: 0;
    }
    /* ── Tables ── */
    .stmt-table {
        width: 100%;
        border-collapse: collapse;
        font-size: .88rem;
        margin: 0;
    }
        .stmt-table thead th {
            background: var(--sc-sky-50);
            color: var(--sc-ink-600);
            font-weight: 800;
            padding: 14px 16px;
            border: none;
            border-bottom: 2px solid var(--sc-sky-100);
            text-align: right;
            font-size: 0.8rem;
            text-transform: uppercase;
            letter-spacing: 0.4px;
        }
        .stmt-table tbody tr {
            transition: background-color .2s ease;
            animation: rowFadeIn 0.4s ease forwards;
```

```
            opacity: 0;
        }
            .stmt-table tbody tr:hover {
                background-color: var(--brand-light) !important;
            }
    @@keyframes rowFadeIn {
        from {
            opacity: 0;
            transform: translateY(6px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .stmt-table tbody td {
        padding: 12px 16px;
        border-bottom: 1px solid var(--sc-sky-100);
        color: var(--sc-ink-900);
        font-weight: 600;
    }
    .stmt-table tbody tr:last-child td {
        border-bottom: none;
    }
    .badge-paid {
        background: #d1fae5;
        color: #047857;
        padding: 3px 12px;
        border-radius: 8px;
        font-size: .75rem;
        font-weight: 800;
    }
    .badge-due {
        background: #fee2e2;
        color: #b91c1c;
        padding: 3px 12px;
        border-radius: 8px;
        font-size: .75rem;
        font-weight: 800;
    }
    .btn-void {
        background: none;
        border: none;
        color: #b91c1c;
        cursor: pointer;
        font-size: .8rem;
        font-weight: 700;
        transition: all .2s;
        padding: 4px 8px;
        border-radius: 6px;
    }
        .btn-void:hover {
            background: #fee2e2;
            transform: scale(1.05);
        }
```

```
    .btn-add-payment {
        display: flex;
        align-items: center;
        gap: 8px;
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
        border: none;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 800;
        font-size: 0.85rem;
        cursor: pointer;
        box-shadow: 0 4px 12px rgba(13,148,136,0.25);
        transition: all .2s ease;
    }
        .btn-add-payment:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(13,148,136,0.35);
        }
    /* ── Empty State ── */
    .empty-state {
        text-align: center;
        padding: 40px;
        color: #94a3b8;
        font-weight: 600;
    }
        .empty-state i {
            display: block;
            font-size: 2.5rem;
            margin-bottom: 12px;
            color: var(--sc-sky-100);
        }
    /* ── Modal ── */
    .custom-modal {
        display: none;
        position: fixed;
        inset: 0;
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }
        .custom-modal.show {
            display: flex;
        }
    .modal-overlay {
        position: absolute;
        inset: 0;
        background: rgba(15,23,42,0.45);
        backdrop-filter: blur(4px);
    }
    .modal-box {
        position: relative;
        z-index: 1;
        width: 90%;
        max-width: 480px;
```

```
        background: #fff;
        border-radius: 20px;
        box-shadow: 0 25px 60px rgba(0,0,0,0.2);
        overflow: hidden;
        animation: modalSlide 0.25s ease;
    }
    @@keyframes modalSlide {
        from {
            opacity: 0;
            transform: translateY(20px) scale(0.98);
        }
        to {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
    }
    .modal-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 18px 24px;
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
    }
        .modal-head h5 {
            margin: 0;
            font-weight: 800;
            font-size: 1.1rem;
        }
    .modal-body {
        padding: 24px;
    }
    .modal-foot {
        padding: 0 24px 24px;
        display: flex;
        justify-content: flex-end;
        gap: 10px;
    }
    .form-label-custom {
        display: block;
        font-size: 0.82rem;
        font-weight: 700;
        color: #475569;
        margin-bottom: 6px;
    }
    .form-control-custom {
        width: 100%;
        padding: 10px 14px;
        border: 1.5px solid var(--sc-sky-100);
        border-radius: 10px;
        font-size: 0.95rem;
        color: var(--sc-ink-900);
        background: #fff;
        transition: all 0.2s;
        outline: none;
```

```
    }
        .form-control-custom:focus {
            border-color: var(--sc-teal-500);
            box-shadow: 0 0 0 3px rgba(20,184,166,0.1);
        }
    .btn-modal {
        padding: 10px 22px;
        border-radius: 10px;
        font-weight: 700;
        font-size: 0.9rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s;
    }
    .btn-modal-cancel {
        background: #f1f5f9;
        color: #475569;
    }
        .btn-modal-cancel:hover {
            background: #e2e8f0;
        }
    .btn-modal-save {
        background: linear-gradient(135deg, var(--sc-teal-600), var(--sc-
teal-700));
        color: #fff;
    }
        .btn-modal-save:hover {
            transform: translateY(-1px);
            box-shadow: 0 6px 16px rgba(13,148,136,0.3);
        }
    /* ── Loading Overlay ── */
    .loading-overlay {
        position: absolute;
        inset: 0;
        background: rgba(255,255,255,0.85);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10;
        backdrop-filter: blur(4px);
        border-radius: var(--radius-lg);
    }
    .loading-pulse {
        display: flex;
        align-items: center;
        gap: 10px;
        color: var(--brand-dark);
        font-weight: 700;
        font-size: 0.9rem;
    }
        .loading-pulse i {
            animation: spin 0.9s linear infinite;
            font-size: 1.3rem;
        }
```

```
    @@keyframes spin {
        from {
            transform: rotate(0deg);
        }
        to {
            transform: rotate(360deg);
        }
    }
    /* ── Responsive ── */
    @@media (max-width: 1100px) {
        .stats-grid {
            grid-template-columns: repeat(2, 1fr);
        }
    }
    @@media (max-width: 768px) {
        .statement-hero {
            flex-direction: column;
            text-align: center;
            padding: 20px;
        }
            .statement-hero .hero-content {
                flex-direction: column;
            }
        .hero-actions {
            width: 100%;
            justify-content: center;
        }
        .stats-grid {
            grid-template-columns: repeat(2, 1fr);
        }
        .section-head {
            flex-direction: column;
            gap: 12px;
            align-items: flex-start;
        }
        .stmt-table {
            font-size: 0.8rem;
        }
            .stmt-table thead th, .stmt-table tbody td {
                padding: 10px 8px;
            }
    }
    @@media (max-width: 480px) {
        .stats-grid {
            grid-template-columns: 1fr;
        }
    }
    @@media print {
        .statement-hero, .btn-hero, .btn-add-payment, .btn-void, .custom-modal {
            display: none !important;
        }
        body {
```

```
            background: #fff;
        }
        .section-shell {
            box-shadow: none;
            border: 1px solid #ddd;
        }
    }
</style>
<div class="container-fluid" dir="rtl" style="padding: 12px; position:
relative;">
    <!-- 📁 Loading Overlay -->
    <div class="loading-overlay" id="pageLoading">
        <div class="loading-pulse">
            <i class="bi bi-arrow-repeat"></i> الحسابكشفتحميلجاري...
        </div>
    </div>
    <!-- 📁 Hero -->
    <div class="statement-hero">
        <div class="hero-content">
            <div class="hero-avatar"><i class="bi bi-receipt-cutoff"></i></div>
            <div>
                <h1 id="stmtPatientName">التحميلجاري...</h1>
                <div class="hero-meta">
                    <span class="hero-meta-tag" id="stmtPatientCode"><i
class="bi bi-hash"></i> —</span>
                    <span class="hero-meta-tag" id="stmtPatientMobile"><i
class="bi bi-telephone"></i> —</span>
                </div>
            </div>
        </div>
        <div class="hero-actions">
            <a href="/Payments" class="btn-hero btn-hero-back">
                <i class="bi bi-arrow-right"></i> رجوع
            </a>
            <button type="button" class="btn-hero btn-hero-print"
onclick="window.print()">
                <i class="bi bi-printer"></i> طباعة
            </button>
        </div>
    </div>
    <!-- 📁 Stats -->
    <div class="stats-grid">
        <div class="stat-card-glass">
            <div class="stat-label">المطلوبإجمالي</div>
            <div class="stat-value" id="sumTotal">—</div>
        </div>
        <div class="stat-card-glass">
            <div class="stat-label">الخصمإجمالي</div>
            <div class="stat-value" id="sumDiscount">—</div>
        </div>
        <div class="stat-card-glass paid">
            <div class="stat-label">المدفوعإجمالي</div>
            <div class="stat-value" id="sumPaid">—</div>
        </div>
        <div class="stat-card-glass due">
            <div class="stat-label">المتبًقي</div>
            <div class="stat-value" id="sumRemaining">—</div>
        </div>
    </div>
```

```
    <!-- 📁 Payments Section -->
    <div class="section-shell">
        <div class="section-head">
            <h5><i class="bi bi-cash-stack"></i> الدفعاتسجل</h5>
            <button class="btn-add-payment" onclick="openAddPaymentModal()">
                <i class="bi bi-plus-lg"></i> جديدةدفعة
            </button>
        </div>
        <div class="section-body">
            <table class="stmt-table">
                <thead>
                    <tr>
                        <th>التاريخ</th>
                        <th>الزيارة</th>
                        <th>المبلغ</th>
                        <th>الطريًقة</th>
                        <th>الإيصالرقم</th>
                        <th>مالحظات</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody id="paymentsBody">
                    <tr><td colspan="7" class="empty-state"><i class="bi bi-
hourglass-split"></i>التحميلجاري...</td></tr>
                </tbody>
            </table>
        </div>
    </div>
    <!-- 📁 Visits Section -->
    <div class="section-shell">
        <div class="section-head">
            <h5><i class="bi bi-clipboard2-data"></i> الزيارات</h5>
        </div>
        <div class="section-body">
            <table class="stmt-table">
                <thead>
                    <tr>
                        <th>التاريخ</th>
                        <th>النوع</th>
                        <th>الإجمالي</th>
                        <th>الخصم</th>
                        <th>المدفوع</th>
                        <th>المتبًقي</th>
                        <th>الحالة</th>
                    </tr>
                </thead>
                <tbody id="visitsBody">
                    <tr><td colspan="7" class="empty-state"><i class="bi bi-
hourglass-split"></i>التحميلجاري...</td></tr>
                </tbody>
            </table>
        </div>
    </div>
```

```
    <!-- 📁 Add Payment Modal -->
    <div class="custom-modal" id="payModal">
        <div class="modal-overlay" onclick="closeAddPaymentModal()"></div>
        <div class="modal-box">
            <div class="modal-head">
                <h5><i class="bi bi-cash-coin me-2"></i> جديدةدفعةتسجيل</h5>
                <button type="button" class="btn-close btn-close-white"
onclick="closeAddPaymentModal()"></button>
```

```
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label-custom">الزيارة <span class="text-
danger">*</span></label>
                    <select class="form-control-custom"
id="payVisitId"></select>
                </div>
                <div class="mb-3">
                    <label class="form-label-custom">المبلغ <span class="text-
danger">*</span></label>
                    <input type="number" step="0.01" class="form-control-custom"
id="payAmount" placeholder="0.00" />
                </div>
                <div class="mb-3">
                    <label class="form-label-custom">الدفعطريًقة</label>
                    <select class="form-control-custom" id="payMethod">
                        <option value="0">نًقدي</option>
                        <option value="1">ائتمانبطاقة</option>
                        <option value="2">خصمبطاقة</option>
                        <option value="3">بنكيتحويل</option>
                        <option value="4">تأمين</option>
                        <option value="5">تًقسيط</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label-custom">مالحظات</label>
                    <textarea class="form-control-custom" id="payDesc" rows="2"
placeholder="إضافيةمالحظات..."></textarea>
                </div>
            </div>
            <div class="modal-foot">
                <button type="button" class="btn-modal btn-modal-cancel"
onclick="closeAddPaymentModal()">إلغاء</button>
                <button type="button" class="btn-modal btn-modal-save"
onclick="submitPayment()">الدفعةحفظ</button>
            </div>
        </div>
    </div>
```

```
</div>
```

```
@section Scripts {
    <script>
        const patientId = @patientId;
        let currentVisits = [];
```

```
        document.addEventListener('DOMContentLoaded', loadStatement);
        async function loadStatement() {
            try {
                const res = await fetch(`/Payments/GetStatement?patientId=$
{patientId}`);
                const result = await res.json();
                document.getElementById('pageLoading').style.display = 'none';
                if (!result.success) {
                    Swal.fire({ title: 'خطأ', text: result.message, icon:
'error' });
                    return;
                }
                renderStatement(result.data);
            } catch (err) {
```

```
                document.getElementById('pageLoading').style.display = 'none';
                console.error('Failed to load statement', err);
                Swal.fire({ title: 'خطأ', text: 'الخادممنالبياناتتحميلفشل',
icon: 'error' });
            }
        }
        function money(v) {
            return Number(v).toLocaleString('ar-EG', { minimumFractionDigits: 2,
maximumFractionDigits: 2 });
        }
        function animateCount(el, target) {
            const start = 0;
            const duration = 800;
            const startTime = performance.now();
            function step(currentTime) {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const easeProgress = 1 - Math.pow(1 - progress, 3);
                el.textContent = money(start + (target * easeProgress));
                if (progress < 1) requestAnimationFrame(step);
                else el.textContent = money(target);
            }
            requestAnimationFrame(step);
        }
        function renderStatement(data) {
            currentVisits = data.visits;
            document.getElementById('stmtPatientName').textContent =
data.patientName;
            document.getElementById('stmtPatientCode').innerHTML = `<i class="bi
bi-hash"></i> ${data.patientCode}`;
            document.getElementById('stmtPatientMobile').innerHTML = `<i
class="bi bi-telephone"></i> ${data.patientMobile || '—'}`;
            animateCount(document.getElementById('sumTotal'), data.totalAmount);
            animateCount(document.getElementById('sumDiscount'),
data.totalDiscount);
            animateCount(document.getElementById('sumPaid'), data.totalPaid);
            animateCount(document.getElementById('sumRemaining'),
data.totalRemaining);
            const visitsBody = document.getElementById('visitsBody');
            visitsBody.innerHTML = data.visits.length ? data.visits.map((v, i)
=> `
                <tr style="animation-delay: ${i * 0.04}s">
                    <td>${new
Date(v.visitDate).toLocaleDateString('ar-EG')}</td>
                    <td>${v.category === 2 ? 'عام' : 'تًقويم'}</td>
                    <td>${money(v.totalAmount)}</td>
                    <td>${money(v.discount)}</td>
                    <td>${money(v.paidAmount)}</td>
                    <td>${money(v.remaining)}</td>
                    <td>${v.isFullyPaid ? '<span class="badge-paid">مسدد</span>'
: '<span class="badge-due">متبًقي</span>'}</td>
                </tr>`).join('') : '<tr><td colspan="7" class="empty-state"><i
class="bi bi-inbox"></i>مسجلةزياراتتوجدال</td></tr>';
```

```
            const paymentsBody = document.getElementById('paymentsBody');
            paymentsBody.innerHTML = data.payments.length ?
data.payments.map((p, i) => `
```

```
                <tr style="animation-delay: ${i * 0.04}s">
                    <td>${new Date(p.paymentDate).toLocaleDateString('ar-EG')}
</td>
```

```
                    <td>${new
Date(p.visitDate).toLocaleDateString('ar-EG')}</td>
                    <td style="font-weight:800; color:#047857;">$
{money(p.amount)}</td>
                    <td>${p.methodText}</td>
                    <td style="direction:ltr; text-align:right;">${p.receiptNo
|| '—'}</td>
```

```
                    <td>${p.description || ''}</td>
                    <td><button class="btn-void" onclick="voidPayment($
{p.id})"><i class="bi bi-x-circle"></i> إلغاء</button></td>
                </tr>`).join('') : '<tr><td colspan="7" class="empty-state"><i
class="bi bi-inbox"></i>مسجلةدفعاتتوجدال</td></tr>';
```

```
            const dueVisits = data.visits.filter(v => v.remaining > 0);
            document.getElementById('payVisitId').innerHTML = dueVisits.length
                ? dueVisits.map(v => `<option value="${v.visitId}">${new
Date(v.visitDate).toLocaleDateString('ar-EG')} — متبًقي
${money(v.remaining)}</option>`).join('')
                : '<option value="">متبًقيمبلغعليهازياراتتوجدال</option>';
        }
```

```
        function openAddPaymentModal() {
            document.getElementById('payAmount').value = '';
            document.getElementById('payDesc').value = '';
            document.getElementById('payModal').classList.add('show');
            document.body.style.overflow = 'hidden';
        }
        function closeAddPaymentModal() {
            document.getElementById('payModal').classList.remove('show');
            document.body.style.overflow = '';
        }
```

```
        async function submitPayment() {
            const visitId =
parseInt(document.getElementById('payVisitId').value);
            const amount =
parseFloat(document.getElementById('payAmount').value);
            const method = parseInt(document.getElementById('payMethod').value);
            const description = document.getElementById('payDesc').value;
            if (!visitId || !amount || amount <= 0) {
                Swal.fire({ title: 'مطلوب', text: ' مبلغوادخلالزيارةاختر
صحيح', icon: 'warning', confirmButtonColor: '#14b8a6' });
                return;
            }
            try {
                const res = await fetch('/Payments/AddPayment', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ VisitId: visitId, Amount: amount,
Method: method, Description: description })
                });
                const result = await res.json();
                if (result.success) {
                    closeAddPaymentModal();
                    Swal.fire({ title: 'تم!', text: result.message, icon:
'success', confirmButtonColor: '#14b8a6', timer: 1500, showConfirmButton:
false });
                    loadStatement();
```

```
                } else {
                    Swal.fire({ title: 'الحفظتعذر', text: result.message, icon:
'error' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'بالسيرفرلتصالفيمشكلة',
icon: 'error' });
            }
        }
        async function voidPayment(paymentId) {
            const confirm = await Swal.fire({
                title: 'الإلغاءتأكيد',
                text: ' .تلًقائي[االزيارةرصيدوتصحيحديالدفعةإلغاءهيتم
متأكد؟',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'إلغاءأيوه،',
                cancelButtonText: 'رجوع',
                confirmButtonColor: '#b91c1c',
                reverseButtons: true
            });
            if (!confirm.isConfirmed) return;
            try {
                const res = await fetch(`/Payments/VoidPayment?paymentId=$
{paymentId}`, { method: 'POST' });
                const result = await res.json();
                if (result.success) {
                    Swal.fire({ title: 'تم', text: result.message, icon:
'success', timer: 1500, showConfirmButton: false });
                    loadStatement();
                } else {
                    Swal.fire({ title: 'التنفيذتعذر', text: result.message,
icon: 'error' });
                }
            } catch (err) {
                Swal.fire({ title: 'خطأ', text: 'بالسيرفرلتصالفيمشكلة',
icon: 'error' });
            }
        }
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') closeAddPaymentModal();
        });
    </script>
}
//View / Shares /
```

```
// _DentalArchStyles.cshtml
@* Shared / _DentalArchStyles.cshtml
   Include once per page via: @await Html.PartialAsync("_DentalArchStyles")
   Provides: CSS for the curved-arch dental chart +
window.initDentalArch(canvasEl)
   Safe to include from both _DentalChartInteractive and _DentalChartReadOnly,
   even if both land on the same page (style rules are idempotent, script is
guarded). *@
<style>
    :root {
        --dc-sky-50: #eafbfc;
        --dc-sky-100: #dcf5f7;
        --dc-teal-900: #0a4d4f;
```

```
        --dc-teal-700: #0d7377;
        --dc-teal-600: #0f9b8e;
        --dc-teal-500: #14b8a6;
        --dc-teal-100: #c7f2ea;
        --dc-coral-300: #f6c6bc;
        --dc-coral-500: #f2b6a8;
        --dc-ink-900: #0b2e30;
        --dc-ink-600: #4b6b6c;
        --dc-healthy: #ffffff;
        --dc-healthy-line: #0f9b8e;
        --dc-decayed: #fee2e2;
        --dc-decayed-line: #ef4444;
        --dc-treated: #dbeafe;
        --dc-treated-line: #3b82f6;
        --dc-missing-line: #cbd5e1;
        --dc-extracted: #1e293b;
        --dc-extracted-line: #1e293b;
        --dc-impacted: #e0e7ff;
        --dc-impacted-line: #6366f1;
        --dc-crown: #f3e8ff;
        --dc-crown-line: #a855f7;
        --dc-bridge: #cffafe;
        --dc-bridge-line: #06b6d4;
        --dc-implant: #fce7f3;
        --dc-implant-line: #ec4899;
        --dc-fractured: #ffedd5;
        --dc-fractured-line: #f97316;
        --dc-rootcanal: #fef3c7;
        --dc-rootcanal-line: #f59e0b;
        --dc-filling: #ccfbf1;
        --dc-filling-line: #14b8a6;
    }
    .dental-chart-shell {
        padding: 10px;
    }
    /* ---------- toolbar ---------- */
    .chart-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 16px;
        flex-wrap: wrap;
        gap: 12px;
    }
    .legend-bar {
        display: flex;
        gap: 14px;
        flex-wrap: wrap;
    }
```

```
    .legend-item {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: .82rem;
        font-weight: 700;
        color: var(--dc-ink-600);
    }
    .dot {
        width: 14px;
```

```
        height: 14px;
        border-radius: 50%;
        border: 2px solid #fff;
        box-shadow: 0 0 0 1px var(--dc-sky-100);
    }
        .dot.healthy {
            background: var(--dc-healthy);
            box-shadow: 0 0 0 1px var(--dc-healthy-line);
        }
        .dot.decayed {
            background: var(--dc-decayed-line);
        }
        .dot.treated {
            background: var(--dc-treated-line);
        }
        .dot.missing {
            background: var(--dc-missing-line);
        }
        .dot.extracted {
            background: var(--dc-extracted-line);
        }
        .dot.rootcanal {
            background: var(--dc-rootcanal-line);
        }
        .dot.crown {
            background: var(--dc-crown-line);
        }
        .dot.implant {
            background: var(--dc-implant-line);
        }
    .btn-save-chart {
        background: linear-gradient(135deg,var(--dc-teal-600),var(--dc-
teal-700));
        color: #fff;
        border: none;
        text-decoration: none;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 800;
        font-size: .85rem;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 8px;
        box-shadow: 0 6px 18px rgba(13,115,119,.3);
        transition: transform .2s ease, box-shadow .2s ease;
    }
        .btn-save-chart:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 24px rgba(13,115,119,.38);
            color: #fff;
        }
    /* ---------- canvas / stage ---------- */
```

```
    .chart-canvas {
        position: relative;
        width: 100%;
        max-width: 640px;
        margin: 0 auto;
        aspect-ratio: 1 / 1.02;
        background: radial-gradient(circle at 10% 8%, var(--dc-teal-100) 0%,
transparent 40%), radial-gradient(circle at 92% 92%, var(--dc-coral-300) 0%,
transparent 32%), var(--dc-sky-50);
        border-radius: 26px;
        border: 1px solid rgba(255,255,255,.7);
        box-shadow: 0 16px 40px rgba(10,77,79,.1);
        padding: 0;
        overflow: hidden;
    }
    .bg-arcs {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        pointer-events: none; /* <-- الضغطاتيمسكشماعشان :جديد */
        z-index: 1; /* <-- الأسنانتحت :جديد */
    }
        .bg-arcs path {
            opacity: 0;
            transition: opacity .8s ease;
        }
            .bg-arcs path.show {
                opacity: 1;
            }
    .arch-label {
        position: absolute;
        left: 50%;
        transform: translateX(-50%);
        font-size: .72rem;
        font-weight: 800;
        color: var(--dc-teal-700);
        letter-spacing: .5px;
        background: rgba(255,255,255,.75);
        padding: 3px 14px;
        border-radius: 20px;
        z-index: 3;
        white-space: nowrap;
        text-transform: none;
    }
        .arch-label.upper {
            top: 3%;
        }
        .arch-label.lower {
            bottom: 3%;
        }
    .arch-center-mark {
        position: absolute;
        left: 50%;
        top: 50%;
        transform: translate(-50%,-50%);
        width: 52px;
```

```
        height: 52px;
        border-radius: 50%;
        background: radial-
gradient(circle,rgba(255,255,255,.9),rgba(255,255,255,.35));
        box-shadow: 0 0 0 8px rgba(15,155,142,.06);
        display: flex;
        align-items: center;
        justify-content: center;
        animation: dcBreathe 3.2s ease-in-out infinite;
        z-index: 1;
    }
        .arch-center-mark svg {
            width: 24px;
            height: 24px;
            color: var(--dc-teal-600);
        }
    @@keyframes dcBreathe {
        0%, 100% {
            box-shadow: 0 0 0 8px rgba(15,155,142,.06);
        }
        50% {
            box-shadow: 0 0 0 14px rgba(15,155,142,.12);
        }
    }
    /* teeth-row is now an absolutely-positioned overlay; JS places each .tooth-
box */
    .teeth-row {
        position: absolute;
        inset: 0;
        z-index: 2;
        pointer-events: none; /* <-- يمسكشمانفسهالسطر :جديددهالسطر clicks
*/
    }
    .tooth-box {
        position: absolute;
        left: 50%;
        top: 50%;
        width: 44px;
        cursor: pointer;
        transform: translate(-50%,-50%) rotate(var(--rot,0deg));
        pointer-events: auto; /* <-- للضغطقابلةتفضلالأسنان */
    }
    .tooth-inner {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 3px;
        transform: scale(0);
        opacity: 0;
        transition: filter .2s ease;
    }
        .tooth-inner.arc-placed {
            animation: dcArcPop .55s cubic-bezier(.2,1.4,.4,1) forwards;
            animation-delay: var(--d,0s);
        }
    @@keyframes dcArcPop {
```

```
        from {
            transform: scale(0);
            opacity: 0;
        }
        to {
            transform: scale(1);
            opacity: 1;
        }
    }
    .tooth-box:hover {
        z-index: 8;
    }
        .tooth-box:hover .tooth-body {
            transform: scale(1.16) translateY(-2px);
            box-shadow: 0 10px 20px rgba(10,77,79,.25);
        }
        .tooth-box:hover .tooth-num {
            transform: scale(1.12);
        }
    .tooth-box.selected {
        z-index: 9;
    }
        .tooth-box.selected .tooth-body {
            transform: scale(1.2) translateY(-3px);
            box-shadow: 0 0 0 4px rgba(15,155,142,.25), 0 12px 24px
rgba(10,77,79,.3);
            border-color: var(--dc-teal-600);
        }
        .tooth-box.selected .tooth-num {
            background: var(--dc-teal-700);
            transform: scale(1.15);
        }
    .tooth-num {
        width: 22px;
        height: 22px;
        border-radius: 50%;
        background: var(--dc-teal-600);
        color: #fff;
        font-size: .66rem;
        font-weight: 800;
        font-family: 'JetBrains Mono',monospace;
        display: flex;
        align-items: center;
        justify-content: center;
        border: 2px solid #fff;
        box-shadow: 0 3px 8px rgba(10,77,79,.22);
        transition: transform .2s ease, background .2s ease;
        flex-shrink: 0;
    }
    .tooth-body {
        width: 34px;
        height: 38px;
        border-radius: 9px 9px 12px 12px;
        border: 2.5px solid var(--dc-healthy-line);
        background: var(--dc-healthy);
```

```
        position: relative;
        transition: transform .2s cubic-bezier(.3,1.5,.4,1), box-shadow .2s
ease, border-color .2s ease, background .2s ease;
        box-shadow: 0 3px 8px rgba(10,77,79,.12);
    }
        .tooth-body .surface {
            position: absolute;
            background: rgba(0,0,0,.06);
        }
            .tooth-body .surface.top {
                top: 0;
                left: 6px;
                right: 6px;
                height: 5px;
                border-radius: 4px 4px 0 0;
            }
            .tooth-body .surface.bottom {
                bottom: 0;
                left: 6px;
                right: 6px;
                height: 5px;
                border-radius: 0 0 4px 4px;
            }
            .tooth-body .surface.left {
                left: 0;
                top: 6px;
                bottom: 6px;
                width: 5px;
                border-radius: 4px 0 0 4px;
            }
            .tooth-body .surface.right {
                right: 0;
                top: 6px;
                bottom: 6px;
                width: 5px;
                border-radius: 0 4px 4px 0;
            }
            .tooth-body .surface.center {
                inset: 8px;
                border-radius: 4px;
                background: rgba(0,0,0,.04);
            }
    /* ---------- status colors (num badge + tooth body) ---------- */
    .tooth-box.decayed .tooth-body {
        background: var(--dc-decayed);
        border-color: var(--dc-decayed-line);
    }
    .tooth-box.decayed .tooth-num {
        background: var(--dc-decayed-line);
    }
    .tooth-box.treated .tooth-body {
        background: var(--dc-treated);
        border-color: var(--dc-treated-line);
    }
```

```
    .tooth-box.treated .tooth-num {
        background: var(--dc-treated-line);
    }
    .tooth-box.missing .tooth-body {
        background: transparent;
        border: 2.5px dashed var(--dc-missing-line);
        box-shadow: none;
        opacity: .65;
    }
    .tooth-box.missing .tooth-num {
        background: #94a3b8;
    }
    .tooth-box.extracted .tooth-body {
        background: var(--dc-extracted);
        border-color: var(--dc-extracted-line);
    }
        .tooth-box.extracted .tooth-body::before,
        .tooth-box.extracted .tooth-body::after {
            content: '';
            position: absolute;
            width: 62%;
            height: 2px;
            background: #fca5a5;
            top: 50%;
            left: 19%;
        }
        .tooth-box.extracted .tooth-body::before {
            transform: rotate(45deg);
        }
        .tooth-box.extracted .tooth-body::after {
            transform: rotate(-45deg);
        }
    .tooth-box.extracted .tooth-num {
        background: var(--dc-extracted-line);
    }
    .tooth-box.impacted .tooth-body {
        background: var(--dc-impacted);
        border-color: var(--dc-impacted-line);
    }
    .tooth-box.impacted .tooth-num {
        background: var(--dc-impacted-line);
    }
    .tooth-box.crown .tooth-body {
        background: var(--dc-crown);
        border-color: var(--dc-crown-line);
    }
    .tooth-box.crown .tooth-num {
        background: var(--dc-crown-line);
    }
    .tooth-box.bridge .tooth-body {
        background: var(--dc-bridge);
        border-color: var(--dc-bridge-line);
```

```
    }
    .tooth-box.bridge .tooth-num {
        background: var(--dc-bridge-line);
    }
    .tooth-box.implant .tooth-body {
        background: var(--dc-implant);
        border-color: var(--dc-implant-line);
    }
    .tooth-box.implant .tooth-num {
        background: var(--dc-implant-line);
    }
    .tooth-box.fractured .tooth-body {
        background: var(--dc-fractured);
        border-color: var(--dc-fractured-line);
    }
    .tooth-box.fractured .tooth-num {
        background: var(--dc-fractured-line);
    }
    .tooth-box.rootcanal .tooth-body {
        background: var(--dc-rootcanal);
        border-color: var(--dc-rootcanal-line);
    }
    .tooth-box.rootcanal .tooth-num {
        background: var(--dc-rootcanal-line);
    }
    .tooth-box.filling .tooth-body {
        background: var(--dc-filling);
        border-color: var(--dc-filling-line);
    }
    .tooth-box.filling .tooth-num {
        background: var(--dc-filling-line);
    }
    /* ---------- modal ---------- */
    .tooth-modal {
        position: fixed;
        inset: 0;
        background: rgba(10,40,42,.45);
        backdrop-filter: blur(6px);
        -webkit-backdrop-filter: blur(6px);
        display: none;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        pointer-events: none;
    }
        .tooth-modal.dc-show {
            display: flex;
            pointer-events: auto;
            animation: dcFadeIn .25s ease;
        }
```

```
    @@keyframes dcFadeIn {
```

```
        from {
            opacity: 0;
        }
        to {
            opacity: 1;
        }
    }
    .modal-glass {
        width: 100%;
        margin: 16px;
        background: #fff;
        border-radius: 22px;
        overflow: hidden;
        box-shadow: 0 30px 70px rgba(0,0,0,.3);
        transform: translateY(24px) scale(.96);
        opacity: 0;
        animation: dcModalIn .35s cubic-bezier(.2,1.2,.3,1) forwards;
    }
    @@keyframes dcModalIn {
        to {
            transform: translateY(0) scale(1);
            opacity: 1;
        }
    }
    .tooth-modal-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 18px 22px;
        margin-bottom: 0;
        color: #fff;
        background: linear-gradient(135deg,var(--dc-teal-600),var(--dc-
teal-700));
    }
        .tooth-modal-head h5 {
            margin: 0;
            font-weight: 800;
            font-size: 1rem;
            color: #fff;
            display: flex;
            align-items: center;
            gap: 8px;
        }
    .btn-close-modal {
        width: 32px;
        height: 32px;
        border-radius: 10px;
        border: none;
        background: rgba(255,255,255,.2);
        color: #fff;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .tooth-modal-body {
        padding: 20px 22px;
```

```
        margin-bottom: 0;
    }
    .tooth-modal-foot {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        padding: 14px 22px;
        border-top: 1px solid var(--dc-sky-100);
    }
    .form-group label {
        display: block;
        font-size: .8rem;
        font-weight: 800;
        color: var(--dc-ink-600);
        margin-bottom: 6px;
    }
    .form-glass {
        width: 100%;
        padding: 10px 14px;
        border: 1.5px solid var(--dc-sky-100);
        border-radius: 10px;
        background: var(--dc-sky-50);
        font-size: .9rem;
        outline: none;
        transition: border-color .2s, box-shadow .2s;
    }
        .form-glass:focus {
            border-color: var(--dc-teal-500);
            box-shadow: 0 0 0 3px rgba(15,155,142,.15);
        }
    .surface-picker {
        display: flex;
        gap: 8px;
    }
    .surf-check {
        flex: 1;
        text-align: center;
        padding: 9px 0;
        border-radius: 10px;
        border: 2px solid var(--dc-sky-100);
        background: #fff;
        font-weight: 800;
        color: var(--dc-ink-600);
        cursor: pointer;
        transition: .2s;
    }
        .surf-check:has(input:checked) {
            background: linear-gradient(135deg,var(--dc-teal-600),var(--dc-
teal-700));
            color: #fff;
            border-color: transparent;
            box-shadow: 0 4px 12px rgba(15,155,142,.3);
        }
        .surf-check input {
            display: none;
        }
```

```
    .btn-glass {
        padding: 9px 18px;
        border-radius: 10px;
        font-weight: 800;
        font-size: .85rem;
        border: none;
        cursor: pointer;
        transition: .2s;
    }
    .btn-cancel {
        background: var(--dc-sky-100);
        color: var(--dc-ink-600);
    }
    .btn-save {
        background: linear-gradient(135deg,var(--dc-teal-600),var(--dc-
teal-700));
        color: #fff;
        box-shadow: 0 6px 16px rgba(15,155,142,.3);
    }
        .btn-save:hover {
            transform: translateY(-1px);
        }
    @@media (max-width: 768px) {
        .tooth-box {
            width: 36px;
        }
        .tooth-body {
            width: 28px;
            height: 32px;
        }
        .tooth-num {
            width: 19px;
            height: 19px;
            font-size: .6rem;
        }
    }
</style>
<script>
    /* Guarded so it's safe if this partial is included more than once on the
same page */
    if (!window.__dcArchEngine) {
        window.__dcArchEngine = true;
        window.__dcArchPoint = function (cx, cy, rx, ry, angleDeg) {
            var rad = angleDeg * Math.PI / 180;
            return { x: cx + rx * Math.cos(rad), y: cy + ry * Math.sin(rad) };
        };
        window.__dcArchPathD = function (cx, cy, rx, ry, centerAngle, spread) {
            var startA = (centerAngle + spread / 2 + 14) * Math.PI / 180;
            var endA   = (centerAngle - spread / 2 - 14) * Math.PI / 180;
            var rxO = rx + 40, ryO = ry + 40, rxI = rx - 34, ryI = ry - 34;
            var p = function (RX, RY, a) { return { x: cx + RX * Math.cos(a), y:
cy + RY * Math.sin(a) }; };
            var o1 = p(rxO, ryO, startA), o2 = p(rxO, ryO, endA);
            var i1 = p(rxI, ryI, endA), i2 = p(rxI, ryI, startA);
```

```
            return 'M ' + o1.x + ' ' + o1.y +
                   ' A ' + rxO + ' ' + ryO + ' 0 0 1 ' + o2.x + ' ' + o2.y +
                   ' L ' + i1.x + ' ' + i1.y +
                   ' A ' + rxI + ' ' + ryI + ' 0 0 0 ' + i2.x + ' ' + i2.y + '
Z';
        };
        /**
         * Lays out a .teeth-row's .tooth-box children along a curved arch,
         * and paints the matching gum-arc background inside the given SVG
paths.
         * canvas: the .chart-canvas element (position:relative, square-ish)
         */
        window.initDentalArch = function (canvas) {
            if (!canvas) return;
            var VB_W = 620, VB_H = 632, cx = 310, spread = 132;
            var upperCfg = { cy: 220, rx: 250, ry: 150, centerAngle: 270 };
            var lowerCfg = { cy: 412, rx: 250, ry: 150, centerAngle: 90 };
            function layoutRow(row, cfg) {
                if (!row) return;
                var items = Array.prototype.filter.call(row.children, function
(el) {
                    return el.classList.contains('tooth-box');
                });
                var N = items.length;
                if (N < 2) return;
                var startA = cfg.centerAngle + spread / 2;
                var endA = cfg.centerAngle - spread / 2;
                items.forEach(function (el, idx) {
                    var t = idx / (N - 1);
                    var angle = startA + (endA - startA) * t;
                    var pt = window.__dcArchPoint(cx, cfg.cy, cfg.rx, cfg.ry,
angle);
                    el.style.left = (pt.x / VB_W * 100) + '%';
                    el.style.top = (pt.y / VB_H * 100) + '%';
                    var localRot = (angle - cfg.centerAngle) * 0.55;
                    el.style.setProperty('--rot', localRot + 'deg');
                    var inner = el.querySelector('.tooth-inner');
                    if (inner) {
                        inner.style.setProperty('--d', (idx * 0.045).toFixed(3)
+ 's');
                        inner.classList.add('arc-placed');
                    }
                });
            }
            layoutRow(canvas.querySelector('.teeth-row.upper'), upperCfg);
            layoutRow(canvas.querySelector('.teeth-row.lower'), lowerCfg);
```

```
            var upperPath = canvas.querySelector('#upperArchPath');
            var lowerPath = canvas.querySelector('#lowerArchPath');
            if (upperPath) upperPath.setAttribute('d', window.__dcArchPathD(cx,
upperCfg.cy, upperCfg.rx, upperCfg.ry, upperCfg.centerAngle, spread));
            if (lowerPath) lowerPath.setAttribute('d', window.__dcArchPathD(cx,
lowerCfg.cy, lowerCfg.rx, lowerCfg.ry, lowerCfg.centerAngle, spread));
            requestAnimationFrame(function () {
                setTimeout(function () {
                    if (upperPath) upperPath.classList.add('show');
                    if (lowerPath) lowerPath.classList.add('show');
                }, 60);
            });
        };
    }
```

```
</script>
```

```
// _DentalChartInteractive.cshtml
```

```
@* Shared / _DentalChartInteractive.cshtml *@
@model Dental.Modules.Clinical.ViewModels.SessionViewModel
@await Html.PartialAsync("_DentalArchStyles")
```

```
<div class="dental-chart-shell">
    <div class="chart-toolbar">
        <div class="legend-bar">
            <span class="legend-item"><span class="dot healthy"></span>
سليم</span>
            <span class="legend-item"><span class="dot decayed"></span>
تسوس</span>
            <span class="legend-item"><span class="dot treated"></span>
معالج</span>
            <span class="legend-item"><span class="dot missing"></span>
مفًقود</span>
            <span class="legend-item"><span class="dot extracted"></span>
مخلوع</span>
            <span class="legend-item"><span class="dot rootcanal"></span>
عصب</span>
            <span class="legend-item"><span class="dot crown"></span> تاج</span>
            <span class="legend-item"><span class="dot implant"></span>
زرعة</span>
        </div>
        <button class="btn-save-chart" onclick="saveChart()">
            <i class="bi bi-check-lg"></i> الرسمةحفظ
        </button>
    </div>
    <div class="chart-canvas" id="chartCanvas">
        <svg class="bg-arcs" viewBox="0 0 620 632" preserveAspectRatio="xMidYMid
meet">
            <defs>
                <linearGradient id="gumUpperInt" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stop-color="#f8d3ca" />
                    <stop offset="100%" stop-color="#f2b6a8" />
                </linearGradient>
                <linearGradient id="gumLowerInt" x1="0" y1="1" x2="0" y2="0">
                    <stop offset="0%" stop-color="#f8d3ca" />
                    <stop offset="100%" stop-color="#f2b6a8" />
                </linearGradient>
            </defs>
            <path id="upperArchPath" fill="url(#gumUpperInt)"></path>
            <path id="lowerArchPath" fill="url(#gumLowerInt)"></path>
        </svg>
        <div class="arch-label upper">العلويالفك</div>
        <div class="arch-label lower">السفليالفك</div>
        <div class="arch-center-mark">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-
width="1.8"><path d="M12 3c-2.4 0-3.6 1.3-4.8 1.3-1.6 0-2.7-1-3.5-.4-.9.7-.7
2.4-.4 3.9.4 2.2 1.1 6.6 2 9.3.5 1.5 1 2.9 2.1 2.9 1.4 0 1.4-3 2.6-3s1.2 3 2.6
3c1.1 0 1.6-1.4 2.1-2.9.9-2.7 1.6-7.1 2-9.3.3-1.5.5-3.2-.4-3.9-.8-.6-1.9.4-
3.5.4C15.6 4.3 14.4 3 12 3Z" /></svg>
        </div>
        <!-- Upper Arch -->
        <div class="teeth-row upper">
            @for (int i = 11; i <= 18; i++)
```

```
            {
                var entry = Model.DentalHistory.FirstOrDefault(d =>
d.ToothNumber == i);
                var status = entry?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" onclick="selectTooth(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
            @for (int i = 21; i <= 28; i++)
            {
                var entry = Model.DentalHistory.FirstOrDefault(d =>
d.ToothNumber == i);
                var status = entry?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" onclick="selectTooth(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
        </div>
        <!-- Lower Arch -->
        <div class="teeth-row lower">
            @for (int i = 41; i <= 48; i++)
            {
                var entry = Model.DentalHistory.FirstOrDefault(d =>
d.ToothNumber == i);
                var status = entry?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" onclick="selectTooth(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
            @for (int i = 31; i <= 38; i++)
```

```
            {
                var entry = Model.DentalHistory.FirstOrDefault(d =>
d.ToothNumber == i);
                var status = entry?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" onclick="selectTooth(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
        </div>
    </div>
    <!-- Tooth Detail Modal -->
    <div class="tooth-modal" id="toothModal">
        <div class="modal-overlay" onclick="closeToothModal()"></div>
        <div class="modal-glass" style="max-width: 420px;">
            <div class="tooth-modal-head">
                <h5><i class="bi bi-grid-3x3-gap"></i> السنتفاصيل <span
id="selectedToothNum"></span></h5>
                <button type="button" class="btn-close-modal"
onclick="closeToothModal()"><i class="bi bi-x-lg"></i></button>
            </div>
            <div class="tooth-modal-body">
                <div class="form-group mb-3">
                    <label>السنحالة</label>
                    <select class="form-glass" id="toothStatus">
                        <option value="1">سليم</option>
                        <option value="2">تسوس</option>
                        <option value="3">معالج</option>
                        <option value="4">مفًقود</option>
                        <option value="5">مخلوع</option>
                        <option value="6">مطمور</option>
                        <option value="7">تاج</option>
                        <option value="8">جسر</option>
                        <option value="9">زرعة</option>
                        <option value="10">مكسور</option>
                        <option value="11">عصب</option>
                        <option value="12">حشوة</option>
                    </select>
                </div>
                <div class="form-group mb-3">
                    <label>المتأثرةالسطوح</label>
                    <div class="surface-picker">
                        <label class="surf-check"><input type="checkbox"
value="M" /> M</label>
                        <label class="surf-check"><input type="checkbox"
value="D" /> D</label>
                        <label class="surf-check"><input type="checkbox"
value="B" /> B</label>
                        <label class="surf-check"><input type="checkbox"
value="L" /> L</label>
                        <label class="surf-check"><input type="checkbox"
value="O" /> O</label>
                    </div>
```

```
                </div>
                <div class="form-group mb-3">
                    <label>الإجراءكود</label>
                    <input type="text" class="form-glass" id="toothProcedure"
placeholder="مثال: RCT, CRN, FLL" />
                </div>
                <div class="form-group">
                    <label>مالحظات</label>
                    <textarea class="form-glass" id="toothNotes"
rows="2"></textarea>
                </div>
            </div>
            <div class="tooth-modal-foot">
                <button type="button" class="btn-glass btn-cancel"
onclick="closeToothModal()">إلغاء</button>
                <button type="button" class="btn-glass btn-save"
onclick="applyToothChanges()">تطبيق</button>
            </div>
        </div>
    </div>
</div>
<script>
    (function () {
        var canvas = document.getElementById('chartCanvas');
        if (window.initDentalArch) window.initDentalArch(canvas);
    })();
    var STATUS_ID_TO_CLASS = {
        1: 'healthy', 2: 'decayed', 3: 'treated', 4: 'missing', 5: 'extracted',
        6: 'impacted', 7: 'crown', 8: 'bridge', 9: 'implant', 10: 'fractured',
        11: 'rootcanal', 12: 'filling'
    };
    let selectedToothId = null;
    let chartData = [];
        function selectTooth(num, evt) {
        if (evt) {
            evt.preventDefault();
            evt.stopPropagation();
        }
        selectedToothId = num;
        // السابقالتحديدإزالة
        document.querySelectorAll('#chartCanvas .tooth-box').forEach(function
(t) {
            t.classList.remove('selected');
        });
        // الحاليالسنتحديد
        const target = document.querySelector(
            '#chartCanvas [data-tooth="' + num + '"]'
        );
        if (target) {
            target.classList.add('selected');
        }
        // السنرقم
        const toothNumber = document.getElementById('selectedToothNum');
```

```
        if (toothNumber) {
            toothNumber.textContent = '#' + num;
        }
        const modal = document.getElementById('toothModal');
```

```
        if (!modal) {
            console.error('Dental modal #toothModal not found');
            return false;
        }
        // =====================================================
        // علىلعتمادبدون - بالًقوةالمودالفتح dc-show
        // =====================================================
```

```
        modal.classList.add('dc-show');
```

```
        modal.style.setProperty('display', 'flex', 'important');
        modal.style.setProperty('visibility', 'visible', 'important');
        modal.style.setProperty('opacity', '1', 'important');
        modal.style.setProperty('pointer-events', 'auto', 'important');
```

```
        return false;
    }
         function closeToothModal() {
```

```
        const modal = document.getElementById('toothModal');
```

```
        if (modal) {
```

```
            // إزالة class
            modal.classList.remove('dc-show');
            // بالًقوةالمودالإغالق
            modal.style.setProperty('display', 'none', 'important');
            modal.style.setProperty('visibility', 'hidden', 'important');
            modal.style.setProperty('opacity', '0', 'important');
            modal.style.setProperty('pointer-events', 'none', 'important');
            // الـإزالة blur تمام[ا
            modal.style.setProperty('backdrop-filter', 'none', 'important');
            modal.style.setProperty('-webkit-backdrop-filter', 'none',
'important');
        }
        // السنتحديدإزالة
        document.querySelectorAll('#chartCanvas .tooth-box').forEach(function
(t) {
            t.classList.remove('selected');
        });
        selectedToothId = null;
    }
```

```
      const toothModal = document.getElementById('toothModal');
```

```
    if (toothModal) {
        toothModal.addEventListener('click', function (e) {
            if (e.target === toothModal && !_modalLock) {
                closeToothModal();
```

```
            }
        });
    }
    function applyToothChanges() {
        const statusId = parseInt(document.getElementById('toothStatus').value);
        const surfaces = Array.from(document.querySelectorAll('.surf-check
input:checked')).map(cb => cb.value);
        const proc = document.getElementById('toothProcedure').value;
        const notes = document.getElementById('toothNotes').value;
        chartData.push({
            Number: selectedToothId,
            Status: statusId,
            Surfaces: surfaces.map(s => ({ Surface: s, Condition: 'Treated' })),
            ProcedureCode: proc,
            Notes: notes
        });
        const toothEl = document.querySelector(`#chartCanvas [data-tooth="$
{selectedToothId}"]`);
        const newClass = STATUS_ID_TO_CLASS[statusId] || 'healthy';
        toothEl.className = toothEl.className
            .split(' ')
            .filter(c => !Object.values(STATUS_ID_TO_CLASS).includes(c))
            .join(' ');
        toothEl.classList.add(newClass);
        closeToothModal();
    }
    async function saveChart() {
        if (chartData.length === 0) {
            Swal.fire({ title: 'تنبيه', text: 'تغييراتإجراءيتملم', icon:
'warning', confirmButtonColor: '#0d9488' });
            return;
        }
        const payload = {
            PatientId: @Model.Patient.Id,
            VisitId: @Model.Visit.Id,
            Teeth: chartData
        };
        const res = await fetch('/Clinical/SaveDentalChart', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            Swal.fire({ title: 'تم', text: 'الأسنانرسمةحفظتم', icon:
'success', timer: 1500, showConfirmButton: false });
            chartData = [];
        }
    }
</script>
```

```
//_DentalChartReadOnly.cshtml
```

```
@* Shared / _DentalChartReadOnly.cshtml *@
@model Dental.Modules.Patients.DTOs.DentalChartDto
@await Html.PartialAsync("_DentalArchStyles")
```

```
<div class="dental-chart-shell">
    <div class="chart-toolbar">
```

```
        <div class="legend-bar">
```

```
            <span class="legend-item"><span class="dot healthy"></span>
سليم</span>
```

```
            <span class="legend-item"><span class="dot decayed"></span>
تسوس</span>
```

```
            <span class="legend-item"><span class="dot treated"></span>
معالج</span>
```

```
            <span class="legend-item"><span class="dot missing"></span>
مفًقود</span>
```

```
            <span class="legend-item"><span class="dot extracted"></span>
مخلوع</span>
            <span class="legend-item"><span class="dot rootcanal"></span>
عصب</span>
            <span class="legend-item"><span class="dot crown"></span> تاج</span>
            <span class="legend-item"><span class="dot implant"></span>
زرعة</span>
```

```
        </div>
```

```
        <a asp-action="NewSession" asp-route-patientId="@Model.PatientId"
class="btn-save-chart">
            <i class="bi bi-pencil"></i> للتعديلسيشنفتح
        </a>
    </div>
```

```
    <div class="chart-canvas" id="chartCanvasRO">
        <svg class="bg-arcs" viewBox="0 0 620 632" preserveAspectRatio="xMidYMid
meet">
            <defs>
                <linearGradient id="gumUpperRO" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stop-color="#f8d3ca" />
                    <stop offset="100%" stop-color="#f2b6a8" />
                </linearGradient>
                <linearGradient id="gumLowerRO" x1="0" y1="1" x2="0" y2="0">
                    <stop offset="0%" stop-color="#f8d3ca" />
                    <stop offset="100%" stop-color="#f2b6a8" />
                </linearGradient>
            </defs>
            <path id="upperArchPath" fill="url(#gumUpperRO)"></path>
            <path id="lowerArchPath" fill="url(#gumLowerRO)"></path>
        </svg>
        <div class="arch-label upper">العلويالفك</div>
        <div class="arch-label lower">السفليالفك</div>
        <div class="arch-center-mark">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-
width="1.8"><path d="M12 3c-2.4 0-3.6 1.3-4.8 1.3-1.6 0-2.7-1-3.5-.4-.9.7-.7
2.4-.4 3.9.4 2.2 1.1 6.6 2 9.3.5 1.5 1 2.9 2.1 2.9 1.4 0 1.4-3 2.6-3s1.2 3 2.6
3c1.1 0 1.6-1.4 2.1-2.9.9-2.7 1.6-7.1 2-9.3.3-1.5.5-3.2-.4-3.9-.8-.6-1.9.4-
3.5.4C15.6 4.3 14.4 3 12 3Z" /></svg>
        </div>
        <div class="teeth-row upper">
            @for (int i = 11; i <= 18; i++)
            {
                var tooth = Model.Teeth.FirstOrDefault(t => t.ToothNumber == i);
                var status = tooth?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                var history = tooth != null ?
System.Text.Json.JsonSerializer.Serialize(tooth.Treatments) : "[]";
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" data-history='@history' onclick="selectToothRO(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
```

```
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
            @for (int i = 21; i <= 28; i++)
            {
                var tooth = Model.Teeth.FirstOrDefault(t => t.ToothNumber == i);
                var status = tooth?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                var history = tooth != null ?
System.Text.Json.JsonSerializer.Serialize(tooth.Treatments) : "[]";
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" data-history='@history' onclick="selectToothRO(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
        </div>
        <div class="teeth-row lower">
            @for (int i = 41; i <= 48; i++)
            {
                var tooth = Model.Teeth.FirstOrDefault(t => t.ToothNumber == i);
                var status = tooth?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                var history = tooth != null ?
System.Text.Json.JsonSerializer.Serialize(tooth.Treatments) : "[]";
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" data-history='@history' onclick="selectToothRO(@i, event)">
                    <div class="tooth-inner">
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
            @for (int i = 31; i <= 38; i++)
            {
                var tooth = Model.Teeth.FirstOrDefault(t => t.ToothNumber == i);
                var status = tooth?.Status ??
Dental.Models.Patient.ToothStatus.Healthy;
                var history = tooth != null ?
System.Text.Json.JsonSerializer.Serialize(tooth.Treatments) : "[]";
                <div class="tooth-box @status.ToString().ToLower()" data-
tooth="@i" data-history='@history' onclick="selectToothRO(@i, event)">
                    <div class="tooth-inner">
```

```
                        <span class="tooth-num">@i</span>
                        <div class="tooth-body">
                            <div class="surface top"></div>
                            <div class="surface right"></div>
                            <div class="surface bottom"></div>
                            <div class="surface left"></div>
                            <div class="surface center"></div>
                        </div>
                    </div>
                </div>
            }
        </div>
    </div>
    <!-- Read-only history modal -->
    <div class="tooth-modal" id="toothModalRO" onclick="if(event.target ===
this) closeToothModalRO()">
        <div class="modal-glass" style="max-width: 420px;"
onclick="event.stopPropagation()">
            <div class="tooth-modal-head">
                <h5><i class="bi bi-grid-3x3-gap"></i> السنتاريخ <span
id="selectedToothNumRO"></span></h5>
                <button type="button" class="btn-close-modal"
onclick="closeToothModalRO()"><i class="bi bi-x-lg"></i></button>
            </div>
            <div class="tooth-modal-body" id="toothHistoryBody">
                <p class="text-muted">السنلهذامسجلعالجتاريخيوجدال.</p>
            </div>
            <div class="tooth-modal-foot">
                <button type="button" class="btn-glass btn-cancel"
onclick="closeToothModalRO()">إغالق</button>
            </div>
        </div>
    </div>
</div>
<script>
    (function () {
        var canvas = document.getElementById('chartCanvasRO');
        if (window.initDentalArch) window.initDentalArch(canvas);
    })();
        function selectToothRO(num, evt) {
        if (evt) {
            evt.preventDefault();
            evt.stopPropagation();
        }
        document.querySelectorAll('#chartCanvasRO .tooth-box').forEach(t => {
            t.classList.remove('selected');
        });
        const box = document.querySelector(
            `#chartCanvasRO [data-tooth="${num}"]`
        );
        if (!box) {
            return false;
        }
        box.classList.add('selected');
        let history = [];
        try {
```

```
            history = JSON.parse(box.dataset.history || '[]');
        } catch (error) {
            console.error('Invalid tooth history:', error);
            history = [];
        }
        document.getElementById('selectedToothNumRO').textContent = '#' + num;
        const body = document.getElementById('toothHistoryBody');
```

```
        if (!body) {
            return false;
        }
```

```
        if (history.length === 0) {
```

```
            body.innerHTML =
                '<p class="text-muted">السنلهذامسجلعالجتاريخيوجدال.</p>';
        } else {
```

```
            body.innerHTML = history.map(h => {
```

```
                const procedure =
                    h.procedureName ??
                    h.ProcedureName ??
                    '-';
```

```
                const dateValue =
                    h.treatmentDate ??
                    h.TreatmentDate;
                const notes =
                    h.notes ??
                    h.Notes;
                let dateText = '-';
```

```
                if (dateValue) {
                    const date = new Date(dateValue);
```

```
                    if (!isNaN(date.getTime())) {
                        dateText = date.toLocaleDateString('ar-EG');
                    }
                }
                return `
                    <div style="border-bottom:1px solid #e2e8f0; padding:8px
0;">
                        <strong>${procedure}</strong><br/>
                        <small class="text-muted">${dateText}</small>
                        ${notes ? `<p class="mb-0">${notes}</p>` : ''}
                    </div>
                `;
            }).join('');
        }
```

```
        const modal = document.getElementById('toothModalRO');
```

```
        if (modal) {
            modal.classList.add('dc-show');
        }
```

```
        return false;
    }
```

```
    function closeToothModalRO() {
```

```
        const modal = document.getElementById('toothModalRO');
        if (modal) {
            modal.classList.remove('dc-show');
        }
        document.querySelectorAll('#chartCanvasRO .tooth-box').forEach(t => {
            t.classList.remove('selected');
        });
    }
```

```
</script>
```

```
// _DynamicDiagnosis.cshtml
```

```
<!-- Views/Shared/_DynamicDiagnosis.cshtml -->
<div class="dynamic-diagnosis-shell">
    <div class="diag-toolbar">
        <div class="diag-filters">
            <button class="filter-chip active"
onclick="filterDiagnosis('all')">الكل</button>
            <button class="filter-chip"
onclick="filterDiagnosis('caries')">تسوس</button>
            <button class="filter-chip"
onclick="filterDiagnosis('periodontal')">لثوي</button>
            <button class="filter-chip"
onclick="filterDiagnosis('endodontic')">عصب</button>
        </div>
        <button class="btn-add-diag" onclick="openDiagModal()">
            <i class="bi bi-plus-lg"></i> جديدتشخيص
        </button>
    </div>
```

```
    <div class="diagnosis-list" id="diagnosisList">
        @foreach (var d in Model.Visit.Diagnoses)
        {
            <div class="diagnosis-item" data-category="@d.Diagnosis.Category">
                <div class="diag-tooth">@d.ToothNumber</div>
                <div class="diag-body">
                    <div class="diag-title">@d.Diagnosis.NameAr</div>
                    <div class="diag-meta">
                        <span class="surface-badges">
                            @if (!string.IsNullOrEmpty(d.Surface))
                            {
                                foreach (var s in d.Surface.Split(','))
                                {
                                    <span class="surf-badge">@s</span>
                                }
                            }
                        </span>
                        <span class="diag-note">@d.Notes</span>
                    </div>
                </div>
                <div class="diag-actions">
                    <button class="icon-btn" onclick="editDiag(@d.Id)"><i
```

```
class="bi bi-pencil"></i></button>
                    <button class="icon-btn danger"
onclick="deleteDiag(@d.Id)"><i class="bi bi-trash"></i></button>
                </div>
            </div>
        }
    </div>
</div>
<!-- Add Diagnosis Modal -->
<div class="diag-modal" id="diagModal" style="display:none;">
    <div class="modal-glass" style="max-width: 500px;">
        <h5><i class="bi bi-activity"></i> تشخيصإضافة</h5>
        <div class="form-row">
            <div class="form-group">
                <label>التشخيص</label>
                <select class="form-glass searchable-diag" id="diagCatalogId">
                    @foreach (var cat in ViewBag.DiagnosisList)
                    {
                        <option value="@cat.Id">@cat.NameAr (@cat.Code)</option>
                    }
                </select>
            </div>
            <div class="form-group">
                <label>السنرقم</label>
                <select class="form-glass" id="diagTooth">
                    @foreach (var t in ViewBag.ToothList)
                    {
                        <option value="@t">@t</option>
                    }
                </select>
            </div>
        </div>
        <div class="form-group">
            <label>المتأثرةالسطوح</label>
            <div class="surface-toggle-group">
                <label class="surf-toggle"><input type="checkbox" value="M" />
M</label>
                <label class="surf-toggle"><input type="checkbox" value="D" />
D</label>
                <label class="surf-toggle"><input type="checkbox" value="B" />
B</label>
                <label class="surf-toggle"><input type="checkbox" value="L" />
L</label>
                <label class="surf-toggle"><input type="checkbox" value="O" />
O</label>
            </div>
        </div>
        <div class="form-group">
            <label>مالحظات</label>
            <textarea class="form-glass" id="diagNotes" rows="2"></textarea>
        </div>
        <div class="modal-actions">
            <button class="btn-glass btn-cancel"
onclick="closeDiagModal()">إلغاء</button>
            <button class="btn-glass btn-save"
onclick="submitDiagnosis()">حفظ</button>
        </div>
    </div>
</div>
<style>
    .dynamic-diagnosis-shell {
        padding: 10px;
```

```
    }
    .diag-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 24px;
        flex-wrap: wrap;
        gap: 12px;
    }
    .diag-filters {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
    }
    .filter-chip {
        padding: 8px 16px;
        border-radius: 999px;
        border: 1px solid #e2e8f0;
        background: #fff;
        font-weight: 600;
        font-size: 0.85rem;
        color: #475569;
        cursor: pointer;
        transition: all 0.2s;
    }
        .filter-chip:hover {
            border-color: #0d9488;
            color: #0d9488;
        }
        .filter-chip.active {
            background: linear-gradient(135deg, #0d9488, #0891b2);
            color: #fff;
            border-color: transparent;
            box-shadow: 0 4px 12px rgba(13,148,136,0.3);
        }
    .btn-add-diag {
        background: linear-gradient(135deg, #8b5cf6, #7c3aed);
        color: #fff;
        border: none;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 700;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 8px;
        box-shadow: 0 4px 16px rgba(139,92,246,0.3);
        transition: all 0.3s;
    }
        .btn-add-diag:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(139,92,246,0.4);
        }
    .diagnosis-list {
        display: flex;
        flex-direction: column;
```

```
        gap: 10px;
    }
    .diagnosis-item {
        display: flex;
        align-items: center;
        gap: 16px;
        background: linear-gradient(135deg, #fff, #f8fafc);
        border-radius: 16px;
        padding: 16px 20px;
        border: 1px solid rgba(226,232,240,0.8);
        box-shadow: 0 2px 8px rgba(0,0,0,0.03);
        transition: all 0.3s;
        animation: slideInRight 0.4s ease backwards;
    }
        .diagnosis-item:hover {
            transform: translateX(-6px);
            box-shadow: 0 8px 24px rgba(0,0,0,0.08);
            border-color: rgba(139,92,246,0.3);
        }
    .diag-tooth {
        width: 48px;
        height: 48px;
        border-radius: 14px;
        background: linear-gradient(135deg, #ede9fe, #ddd6fe);
        color: #5b21b6;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 800;
        font-size: 1.1rem;
        flex-shrink: 0;
    }
    .diag-body {
        flex: 1;
    }
    .diag-title {
        font-weight: 700;
        color: #1e293b;
        font-size: 1rem;
    }
    .diag-meta {
        display: flex;
        gap: 10px;
        margin-top: 6px;
        flex-wrap: wrap;
        align-items: center;
    }
    .surface-badges {
        display: flex;
        gap: 4px;
    }
    .surf-badge {
        padding: 2px 8px;
        border-radius: 6px;
        background: #f1f5f9;
        color: #475569;
```

```
        font-size: 0.75rem;
        font-weight: 700;
        font-family: monospace;
    }
    .diag-note {
        font-size: 0.85rem;
        color: #64748b;
    }
    .diag-actions {
        display: flex;
        gap: 6px;
    }
    .icon-btn {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        border: none;
        background: transparent;
        color: #64748b;
        cursor: pointer;
        transition: all 0.2s;
        display: flex;
        align-items: center;
        justify-content: center;
    }
        .icon-btn:hover {
            background: #f1f5f9;
            color: #0d9488;
            transform: scale(1.1);
        }
        .icon-btn.danger:hover {
            background: #fee2e2;
            color: #dc2626;
        }
    .surface-toggle-group {
        display: flex;
        gap: 10px;
    }
    .surf-toggle {
        padding: 10px 16px;
        border-radius: 12px;
        border: 2px solid #e2e8f0;
        background: #fff;
        font-weight: 700;
        color: #475569;
        cursor: pointer;
        transition: all 0.2s;
        display: flex;
        align-items: center;
        gap: 6px;
    }
        .surf-toggle:has(input:checked) {
            background: linear-gradient(135deg, #0d9488, #0891b2);
            color: #fff;
            border-color: transparent;
            box-shadow: 0 4px 12px rgba(13,148,136,0.3);
```

```
        }
        .surf-toggle input {
            display: none;
        }
    .form-row {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: 12px;
    }
</style>
<script>
    function openDiagModal()
{ document.getElementById('diagModal').style.display = 'flex'; }
    function closeDiagModal()
{ document.getElementById('diagModal').style.display = 'none'; }
    function filterDiagnosis(cat) {
        document.querySelectorAll('.filter-chip').forEach(c =>
c.classList.remove('active'));
        event.target.classList.add('active');
        document.querySelectorAll('.diagnosis-item').forEach(item => {
            item.style.display = (cat === 'all' || item.dataset.category ===
cat) ? 'flex' : 'none';
        });
    }
    async function submitDiagnosis() {
        const surfaces = Array.from(document.querySelectorAll('.surf-toggle
input:checked')).map(cb => cb.value).join(',');
        const data = {
            VisitId: @Model.Visit.Id,
            DiagnosisId:
parseInt(document.getElementById('diagCatalogId').value),
            ToothNumber: parseInt(document.getElementById('diagTooth').value),
            Surface: surfaces,
            Notes: document.getElementById('diagNotes').value,
            IsPrimary: true
        };
        const res = await fetch('/Clinical/AddDiagnosis', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (res.ok) location.reload();
    }
</script>
```

```
// _DynamicProcedures.cshtml
<!-- Views/Shared/_DynamicDiagnosis.cshtml -->
<div class="dynamic-diagnosis-shell">
    <div class="diag-toolbar">
        <div class="diag-filters">
            <button class="filter-chip active"
onclick="filterDiagnosis('all')">الكل</button>
            <button class="filter-chip"
onclick="filterDiagnosis('caries')">تسوس</button>
            <button class="filter-chip"
onclick="filterDiagnosis('periodontal')">لثوي</button>
            <button class="filter-chip"
onclick="filterDiagnosis('endodontic')">عصب</button>
        </div>
```

```
        <button class="btn-add-diag" onclick="openDiagModal()">
            <i class="bi bi-plus-lg"></i> جديدتشخيص
        </button>
    </div>
    <div class="diagnosis-list" id="diagnosisList">
        @foreach (var d in Model.Visit.Diagnoses)
        {
            <div class="diagnosis-item" data-category="@d.Diagnosis.Category">
                <div class="diag-tooth">@d.ToothNumber</div>
                <div class="diag-body">
                    <div class="diag-title">@d.Diagnosis.NameAr</div>
                    <div class="diag-meta">
                        <span class="surface-badges">
                            @if (!string.IsNullOrEmpty(d.Surface))
                            {
                                foreach (var s in d.Surface.Split(','))
                                {
                                    <span class="surf-badge">@s</span>
                                }
                            }
                        </span>
                        <span class="diag-note">@d.Notes</span>
                    </div>
                </div>
                <div class="diag-actions">
                    <button class="icon-btn" onclick="editDiag(@d.Id)"><i
class="bi bi-pencil"></i></button>
                    <button class="icon-btn danger"
onclick="deleteDiag(@d.Id)"><i class="bi bi-trash"></i></button>
                </div>
            </div>
        }
    </div>
</div>
<!-- Add Diagnosis Modal -->
<div class="diag-modal" id="diagModal" style="display:none;">
    <div class="modal-glass" style="max-width: 500px;">
        <h5><i class="bi bi-activity"></i> تشخيصإضافة</h5>
        <div class="form-row">
            <div class="form-group">
                <label>التشخيص</label>
                <select class="form-glass searchable-diag" id="diagCatalogId">
                    @foreach (var cat in ViewBag.DiagnosisList)
                    {
                        <option value="@cat.Id">@cat.NameAr (@cat.Code)</option>
                    }
                </select>
            </div>
            <div class="form-group">
                <label>السنرقم</label>
                <select class="form-glass" id="diagTooth">
                    @foreach (var t in ViewBag.ToothList)
                    {
                        <option value="@t">@t</option>
                    }
                </select>
            </div>
        </div>
        <div class="form-group">
            <label>المتأثرةالسطوح</label>
            <div class="surface-toggle-group">
                <label class="surf-toggle"><input type="checkbox" value="M" />
```

```
M</label>
```

```
                <label class="surf-toggle"><input type="checkbox" value="D" />
D</label>
```

```
                <label class="surf-toggle"><input type="checkbox" value="B" />
B</label>
                <label class="surf-toggle"><input type="checkbox" value="L" />
L</label>
                <label class="surf-toggle"><input type="checkbox" value="O" />
O</label>
```

```
            </div>
        </div>
        <div class="form-group">
            <label>مالحظات</label>
            <textarea class="form-glass" id="diagNotes" rows="2"></textarea>
        </div>
        <div class="modal-actions">
            <button class="btn-glass btn-cancel"
onclick="closeDiagModal()">إلغاء</button>
            <button class="btn-glass btn-save"
onclick="submitDiagnosis()">حفظ</button>
        </div>
    </div>
</div>
<style>
    .dynamic-diagnosis-shell {
        padding: 10px;
    }
    .diag-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 24px;
        flex-wrap: wrap;
        gap: 12px;
    }
    .diag-filters {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
    }
    .filter-chip {
        padding: 8px 16px;
        border-radius: 999px;
        border: 1px solid #e2e8f0;
        background: #fff;
        font-weight: 600;
        font-size: 0.85rem;
        color: #475569;
        cursor: pointer;
        transition: all 0.2s;
    }
        .filter-chip:hover {
            border-color: #0d9488;
            color: #0d9488;
        }
        .filter-chip.active {
            background: linear-gradient(135deg, #0d9488, #0891b2);
            color: #fff;
```

```
            border-color: transparent;
            box-shadow: 0 4px 12px rgba(13,148,136,0.3);
        }
    .btn-add-diag {
        background: linear-gradient(135deg, #8b5cf6, #7c3aed);
        color: #fff;
        border: none;
        padding: 10px 20px;
        border-radius: 12px;
        font-weight: 700;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 8px;
        box-shadow: 0 4px 16px rgba(139,92,246,0.3);
        transition: all 0.3s;
    }
        .btn-add-diag:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(139,92,246,0.4);
        }
    .diagnosis-list {
        display: flex;
        flex-direction: column;
        gap: 10px;
    }
    .diagnosis-item {
        display: flex;
        align-items: center;
        gap: 16px;
        background: linear-gradient(135deg, #fff, #f8fafc);
        border-radius: 16px;
        padding: 16px 20px;
        border: 1px solid rgba(226,232,240,0.8);
        box-shadow: 0 2px 8px rgba(0,0,0,0.03);
        transition: all 0.3s;
        animation: slideInRight 0.4s ease backwards;
    }
        .diagnosis-item:hover {
            transform: translateX(-6px);
            box-shadow: 0 8px 24px rgba(0,0,0,0.08);
            border-color: rgba(139,92,246,0.3);
        }
    .diag-tooth {
        width: 48px;
        height: 48px;
        border-radius: 14px;
        background: linear-gradient(135deg, #ede9fe, #ddd6fe);
        color: #5b21b6;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 800;
        font-size: 1.1rem;
        flex-shrink: 0;
    }
    .diag-body {
```

```
        flex: 1;
    }
    .diag-title {
        font-weight: 700;
        color: #1e293b;
        font-size: 1rem;
    }
    .diag-meta {
        display: flex;
        gap: 10px;
        margin-top: 6px;
        flex-wrap: wrap;
        align-items: center;
    }
    .surface-badges {
        display: flex;
        gap: 4px;
    }
    .surf-badge {
        padding: 2px 8px;
        border-radius: 6px;
        background: #f1f5f9;
        color: #475569;
        font-size: 0.75rem;
        font-weight: 700;
        font-family: monospace;
    }
    .diag-note {
        font-size: 0.85rem;
        color: #64748b;
    }
    .diag-actions {
        display: flex;
        gap: 6px;
    }
    .icon-btn {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        border: none;
        background: transparent;
        color: #64748b;
        cursor: pointer;
        transition: all 0.2s;
        display: flex;
        align-items: center;
        justify-content: center;
    }
        .icon-btn:hover {
            background: #f1f5f9;
            color: #0d9488;
            transform: scale(1.1);
        }
        .icon-btn.danger:hover {
            background: #fee2e2;
```

```
            color: #dc2626;
        }
    .surface-toggle-group {
        display: flex;
        gap: 10px;
    }
    .surf-toggle {
        padding: 10px 16px;
        border-radius: 12px;
        border: 2px solid #e2e8f0;
        background: #fff;
        font-weight: 700;
        color: #475569;
        cursor: pointer;
        transition: all 0.2s;
        display: flex;
        align-items: center;
        gap: 6px;
    }
        .surf-toggle:has(input:checked) {
            background: linear-gradient(135deg, #0d9488, #0891b2);
            color: #fff;
            border-color: transparent;
            box-shadow: 0 4px 12px rgba(13,148,136,0.3);
        }
        .surf-toggle input {
            display: none;
        }
    .form-row {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: 12px;
    }
</style>
<script>
    function openDiagModal()
{ document.getElementById('diagModal').style.display = 'flex'; }
    function closeDiagModal()
{ document.getElementById('diagModal').style.display = 'none'; }
    function filterDiagnosis(cat) {
        document.querySelectorAll('.filter-chip').forEach(c =>
c.classList.remove('active'));
        event.target.classList.add('active');
        document.querySelectorAll('.diagnosis-item').forEach(item => {
            item.style.display = (cat === 'all' || item.dataset.category ===
cat) ? 'flex' : 'none';
        });
    }
    async function submitDiagnosis() {
        const surfaces = Array.from(document.querySelectorAll('.surf-toggle
input:checked')).map(cb => cb.value).join(',');
        const data = {
            VisitId: @Model.Visit.Id,
            DiagnosisId:
parseInt(document.getElementById('diagCatalogId').value),
            ToothNumber: parseInt(document.getElementById('diagTooth').value),
            Surface: surfaces,
            Notes: document.getElementById('diagNotes').value,
```

```
            IsPrimary: true
        };
        const res = await fetch('/Clinical/AddDiagnosis', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (res.ok) location.reload();
    }
</script>
```

```
// _Layout.cshtml
```

```
@using Microsoft.AspNetCore.Identity
@using Dental.Models.User
@inject IAuthorizationService AuthorizationService
@inject SignInManager<ApplicationUser> SignInManager
@inject UserManager<ApplicationUser> UserManager
@inject RoleManager<ApplicationRole> RoleManager
```

```
<!DOCTYPE html>
<html lang="ar" data-theme="light">
<head>
    <meta name="autocomplete" content="off">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - الخوليمحمد / دعيادة</title>
```

```
    <!-- Bootstrap (base only) -->
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <!-- Fonts: Beiruti (Arabic) + Ubuntu Condensed (Latin) -->
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?
family=Beiruti:wght@200..900&family=Ubuntu+Condensed&display=swap"
rel="stylesheet">
```

```
    <!-- Premium Design System v2.0 -->
    <link rel="stylesheet" href="~/css/clinic-design-system-v2.css" asp-append-
version="true" />
```

```
    <!-- Page-specific styles -->
    @await RenderSectionAsync("Styles", required: false)
```

```
    <!-- Legacy CSS — TODO: Migrate and remove -->
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/css/CreationStyle.css" asp-append-
version="true" />
    <link rel="stylesheet" href="~/css/executive-dashboard.css" asp-append-
version="true" />
    <link rel="stylesheet" href="~/Dental.styles.css" asp-append-
version="true" />
    <!-- Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-
icons@1.11.3/font/bootstrap-icons.min.css">
    <link rel="icon" type="image/x-icon" href="~/images/sklogo.ico">
```

```
    <!-- Select2 -->
    <link
href="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css"
rel="stylesheet" />
```

```
    <!-- DataTables -->
    <link rel="stylesheet"
href="https://cdn.datatables.net/2.3.4/css/dataTables.dataTables.css" />
    <link rel="stylesheet"
href="https://cdn.datatables.net/columncontrol/1.1.1/css/columnControl.dataTable
s.css" />
```

```
    <!-- SweetAlert -->
    <link href="~/sweetalert2/sweetalert2.min.css" rel="stylesheet" />
    <!-- JS (core first) -->
    <script src="https://code.jquery.com/jquery-3.7.1.js"></script>
    <script src="https://cdn.datatables.net/2.3.4/js/dataTables.js"></script>
    <script src="https://code.highcharts.com/highcharts.js"></script>
    <script
src="https://cdnjs.cloudflare.com/ajax/libs/moment.js/2.29.2/moment.min.js"></
script>
</head>
<body>
```

```
    <div class="app-shell">
        <!-- Sidebar Overlay (mobile) -->
        <div class="sidebar-overlay" id="sidebarOverlay"
onclick="toggleSidebar()"></div>
        <!-- ============ Sidebar ============ -->
        <aside class="app-sidebar" id="appSidebar">
            <div class="sidebar-brand">
                <span class="mark">
                    <svg viewBox="0 0 24 24" fill="none"
xmlns="http://www.w3.org/2000/svg">
                        <path d="M12 2C9.6 2 8.1 3.2 6.7 3.2c-1.7 0-3.2 1.5-3.2
4 0 2.1.6 3.9 1.1 5.9.5 2 .9 4.3 1.6 5.9.5 1.2 1.1 2 1.9 2 1.1 0 1.3-2.5 1.6-
4.2.2-1.2.6-2.1 1.3-2.1s1.1.9 1.3 2.1c.3 1.7.5 4.2 1.6 4.2.8 0 1.4-.8 1.9-2 .7-
1.6 1.1-3.9 1.6-5.9.5-2 1.1-3.8 1.1-5.9 0-2.5-1.5-4-3.2-4C15.9 3.2 14.4 2 12 2z"
fill="white" />
                    </svg>
                </span>
                <div class="brand-text">
                    <strong>الخوليمحمد /د</strong>
                    <span>Dental Suite</span>
                </div>
            </div>
            <nav class="sidebar-nav">
                <a asp-controller="Home" asp-action="Index" class="sidebar-link"
data-title="التحكملوحة">
                    <i class="bi bi-grid-1x2-fill"></i>
                    <span>التحكملوحة</span>
                </a>
                @if (SignInManager.IsSignedIn(User))
                {
                    <div class="sidebar-section-label">العيادة</div>
                    <a asp-controller="Patients" asp-action="Index"
class="sidebar-link" data-title="المرضىقائمة">
                        <i class="bi bi-people-fill"></i>
                        <span>المرضىقائمة</span>
                    </a>
```

```
                    <a asp-controller="Appointments" asp-action="index"
class="sidebar-link" data-title="المواعيد">
                        <i class="bi bi-calendar2-week-fill"></i>
                        <span>المواعيد</span>
                    </a>
                    <a asp-controller="Payments" asp-action="index"
class="sidebar-link" data-title="والمدفوعاتالفواتير">
                        <i class="bi bi-cash-coin"></i>
                        <span>والمدفوعاتالفواتير</span>
                    </a>
                    <a href="#" class="sidebar-link" data-title="العالجات">
                        <i class="bi bi-clipboard2-pulse-fill"></i>
                        <span>العالجات</span>
                    </a>
                    @if (User.IsInRole(Roles.Admin.ToString()))
                    {
                        <div class="sidebar-section-label">الإدارة</div>
                        <a asp-controller="Users" asp-action="Index"
class="sidebar-link" data-title="المستخدمين">
                            <i class="bi bi-person-badge-fill"></i>
                            <span>المستخدمين</span>
                        </a>
                        <a asp-controller="Roles" asp-action="Index"
class="sidebar-link" data-title="المجموعات">
                            <i class="bi bi-shield-lock-fill"></i>
                            <span>المجموعات</span>
                        </a>
                        <a href="#" class="sidebar-link" data-title="التًقارير">
                            <i class="bi bi-bar-chart-line-fill"></i>
                            <span>التًقارير</span>
                        </a>
                    }
                }
            </nav>
            <div class="sidebar-footer">
                <a class="sidebar-clinic-link"
href="https://www.facebook.com/share/1B3xKJX65H/" target="_blank" data-
title="العيادةصفحة">
                    <i class="bi bi-facebook"></i>
                    <span>العيادةصفحة</span>
                </a>
            </div>
        </aside>
        <!-- ============ Main Column ============ -->
        <div class="app-main">
            <!-- Topbar — Floating Glass -->
            <header class="app-topbar">
                <div class="topbar-left">
                    <button class="sidebar-toggle" id="sidebarToggle"
onclick="toggleSidebar()" aria-label="Toggle menu">
                        <i class="bi bi-list"></i>
                    </button>
                    <div class="topbar-search">
                        <i class="bi bi-search"></i>
```

```
                        <input type="text" placeholder=" ،بحثفاتورةمريض،عن
موعد..." />
                    </div>
                </div>
                <div class="topbar-actions">
                    <!-- Theme Toggle -->
                    <button class="theme-toggle" id="themeToggle"
onclick="toggleTheme()" title="الوضعتبديل">
                        <i class="bi bi-moon-fill" id="themeIcon"></i>
                    </button>
                    <button class="topbar-btn" title="الإشعارات"
id="notificationBtn">
                        <i class="bi bi-bell-fill"></i>
                        <span class="badge">3</span>
                    </button>
                    <button class="topbar-btn" title="الرسائل">
                        <i class="bi bi-chat-left-text-fill"></i>
                    </button>
                    <!-- User Menu -->
                    <div class="user-menu" id="userMenu"
onclick="toggleUserDropdown()">
                        <div class="user-avatar">
                            @(User.Identity?.Name?.Substring(0, 1).ToUpper() ??
"م")
                        </div>
                        <div class="user-info">
                            <span class="name">@(User.Identity?.Name ??
"مستخدم")</span>
                            <span class="role">
                                @(User.IsInRole(Roles.Admin.ToString()) ? " مدير
طبيب" : "النظام")
                            </span>
                        </div>
                        <i class="bi bi-chevron-down"></i>
                        <!-- Dropdown -->
                        <div class="user-dropdown" id="userDropdown">
                            <a href="#" class="dropdown-item">
                                <i class="bi bi-person"></i>
                                <span>الشخصيالملف</span>
                            </a>
                            <a href="#" class="dropdown-item">
                                <i class="bi bi-gear"></i>
                                <span>الإعدادات</span>
                            </a>
                            <div class="dropdown-divider"></div>
                            <form asp-area="Identity" asp-page="/Account/Logout"
asp-route-returnUrl="@Url.Action("Index", "Home", new { area = "" })"
method="post" style="margin:0;">
```

```
                                <button type="submit" class="dropdown-item
danger">
                                    <i class="bi bi-box-arrow-right"></i>
                                    <span>الخروجتسجيل</span>
                                </button>
                            </form>
                        </div>
```

```
                    </div>
                </div>
```

```
            </header>
```

```
            <!-- Page Content -->
            <main class="app-content" dir="auto">
                @RenderBody()
            </main>
        </div>
    </div>
    <!-- Floating Action Button -->
    <button class="fab" id="fabBtn" title="سريعإجراء"
onclick="showQuickActions()">
        <i class="bi bi-plus-lg"></i>
    </button>
    <!-- ================= Scripts ================= -->
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script
src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></
script>
    <script src="~/sweetalert2/sweetalert2.all.min.js"></script>
    <script src="~/lib/microsoft/signalr/dist/browser/signalr.js"></script>
    <script src="https://www.gstatic.com/firebasejs/10.7.0/firebase-app-
compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/10.7.0/firebase-messaging-
compat.js"></script>
    <!-- App Scripts -->
    <script src="~/js/site.js"></script>
    <script src="~/js/login.js"></script>
    <script src="~/js/Users.js"></script>
    <script>
        $(document).ready(function () {
        // Initialize Select2
        $('.searchable').select2({
        dir: 'rtl',
        width: '100%'
        });
        // Mark active sidebar link based on current URL
        var path = window.location.pathname.toLowerCase();
        $('.sidebar-link').each(function () {
        var href = ($(this).attr('href') || '').toLowerCase();
        if (href && href !== '#' && path.indexOf(href) === 0) {
        $(this).addClass('active');
        }
        });
        // Close dropdowns when clicking outside
        $(document).on('click', function (e) {
        if (!$(e.target).closest('#userMenu').length) {
        $('#userDropdown').removeClass('show');
        $('#userMenu').removeClass('active');
        }
        });
        // Animate stat cards on scroll
        animateOnScroll();
        });
        // Sidebar Toggle
        function toggleSidebar() {
        const sidebar = document.getElementById('appSidebar');
        const overlay = document.getElementById('sidebarOverlay');
```

```
        const isMobile = window.innerWidth <= 768;
```

```
        if (isMobile) {
        sidebar.classList.toggle('mobile-open');
        overlay.classList.toggle('active');
        } else {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed',
sidebar.classList.contains('collapsed'));
        }
        }
        // Restore sidebar state
        (function () {
        const sidebar = document.getElementById('appSidebar');
        const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (isCollapsed && window.innerWidth > 768) {
        sidebar.classList.add('collapsed');
        }
        })();
        // User Dropdown Toggle
        function toggleUserDropdown() {
        const dropdown = document.getElementById('userDropdown');
        const menu = document.getElementById('userMenu');
        dropdown.classList.toggle('show');
        menu.classList.toggle('active');
        }
        // Theme Toggle
        function toggleTheme() {
        const html = document.documentElement;
        const icon = document.getElementById('themeIcon');
        const currentTheme = html.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        html.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        if (newTheme === 'dark') {
        icon.classList.remove('bi-moon-fill');
        icon.classList.add('bi-sun-fill');
        } else {
        icon.classList.remove('bi-sun-fill');
        icon.classList.add('bi-moon-fill');
        }
        }
        // Restore theme
        (function () {
        const savedTheme = localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', savedTheme);
        const icon = document.getElementById('themeIcon');
        if (savedTheme === 'dark' && icon) {
        icon.classList.remove('bi-moon-fill');
        icon.classList.add('bi-sun-fill');
        }
        })();
        // Handle window resize
        window.addEventListener('resize', function () {
        const sidebar = document.getElementById('appSidebar');
        const overlay = document.getElementById('sidebarOverlay');
```

```
        if (window.innerWidth > 768) {
        sidebar.classList.remove('mobile-open');
        overlay.classList.remove('active');
        }
        });
        // Animate elements on scroll
        function animateOnScroll() {
        const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
        if (entry.isIntersecting) {
        entry.target.style.animationPlayState = 'running';
        }
        });
        }, { threshold: 0.1 });
        document.querySelectorAll('.stat-card, .card').forEach(el => {
        observer.observe(el);
        });
        }
        // Quick Actions FAB
        function showQuickActions() {
        // You can implement a modal or dropdown here
        console.log('Quick actions clicked');
        }
    </script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
// _ProfessionalPayments.cshtml
<!-- Views/Shared/_ProfessionalPayments.cshtml -->
<div class="payments-container">
    <div class="payments-header">
        <h4><i class="bi bi-credit-card-2-front"></i> المدفوعات</h4>
        <button class="btn-add-payment" onclick="openPaymentModal()">
            <i class="bi bi-plus-lg"></i> دفعةتسجيل
        </button>
    </div>
    <div class="payment-cards" id="paymentCards">
        @foreach (var payment in Model.Visit.Payments)
        {
            <div class="payment-card">
                <div class="payment-main">
                    <div class="payment-icon">
                        <i class="bi bi-cash-stack"></i>
                    </div>
                    <div class="payment-info">
                        <div class="payment-
amount">@payment.Amount.ToString("N0") EGP</div>
                        <div class="payment-desc">@payment.Description</div>
                        <div class="payment-meta">
                            <span class="method-badge
@payment.Method.ToString().ToLower()">@payment.Method</span>
                            <span class="receipt-no"><i class="bi bi-
receipt"></i> @payment.ReceiptNo</span>
                            <span class="payment-date"><i class="bi bi-
calendar"></i> @payment.PaymentDate.ToString("dd/MM/yyyy HH:mm")</span>
                        </div>
```

```
                    </div>
                </div>
            </div>
        }
    </div>
    <!-- Payment Form Modal -->
    <div class="payment-modal" id="paymentModal" style="display:none;">
        <div class="modal-glass">
            <h5>جديدةدفعةتسجيل</h5>
            <div class="form-group">
                <label>المبلغ</label>
                <input type="number" class="form-glass" id="payAmount" />
            </div>
            <div class="form-group">
                <label>الدفعطريًقة</label>
                <select class="form-glass" id="payMethod">
                    <option value="Cash">نًقدي</option>
                    <option value="CreditCard">فيزا</option>
                    <option value="BankTransfer">بنكيتحويل</option>
                </select>
            </div>
            <div class="form-group">
                <label>المدفوعةالإجراءات / السبب</label>
                <textarea class="form-glass" id="payDesc" rows="2"
placeholder="جزئيةدفعة :مثال — Crown 16"></textarea>
            </div>
            <div class="modal-actions">
                <button class="btn-glass btn-cancel"
onclick="closePaymentModal()">إلغاء</button>
                <button class="btn-glass btn-save" onclick="submitPayment()">حفظ
الدفعة</button>
            </div>
        </div>
    </div>
</div>
<style>
    .payments-container {
        padding: 10px;
    }
    .payments-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 24px;
    }
        .payments-header h4 {
            margin: 0;
            font-weight: 800;
            color: #0f172a;
            display: flex;
            align-items: center;
            gap: 10px;
        }
    .btn-add-payment {
        background: linear-gradient(135deg, #0d9488, #0891b2);
        color: #fff;
        border: none;
        padding: 10px 20px;
        border-radius: 12px;
```

```
        font-weight: 700;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 8px;
        box-shadow: 0 4px 16px rgba(13,148,136,0.3);
        transition: all 0.3s;
    }
        .btn-add-payment:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(13,148,136,0.4);
        }
    .payment-cards {
        display: flex;
        flex-direction: column;
        gap: 12px;
    }
    .payment-card {
        background: linear-gradient(135deg, #fff 0%, #f8fafc 100%);
        border-radius: 16px;
        padding: 20px;
        border: 1px solid rgba(226,232,240,0.8);
        box-shadow: 0 4px 12px rgba(0,0,0,0.04);
        transition: all 0.3s;
        display: flex;
        justify-content: space-between;
        align-items: center;
    }
        .payment-card:hover {
            transform: translateX(-4px);
            box-shadow: 0 8px 24px rgba(0,0,0,0.08);
            border-color: rgba(13,148,136,0.3);
        }
    .payment-main {
        display: flex;
        align-items: center;
        gap: 16px;
    }
    .payment-icon {
        width: 48px;
        height: 48px;
        border-radius: 14px;
        background: linear-gradient(135deg, #dcfce7, #bbf7d0);
        color: #16a34a;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.3rem;
    }
    .payment-amount {
        font-size: 1.3rem;
        font-weight: 800;
        color: #0f172a;
    }
    .payment-desc {
        color: #475569;
```

```
        font-size: 0.9rem;
        margin-top: 4px;
    }
    .payment-meta {
        display: flex;
        gap: 10px;
        margin-top: 8px;
        flex-wrap: wrap;
    }
    .method-badge {
        padding: 4px 12px;
        border-radius: 8px;
        font-size: 0.75rem;
        font-weight: 700;
    }
        .method-badge.cash {
            background: #dcfce7;
            color: #166534;
        }
        .method-badge.creditcard {
            background: #dbeafe;
            color: #1e40af;
        }
        .method-badge.banktransfer {
            background: #fef3c7;
            color: #92400e;
        }
    .receipt-no, .payment-date {
        font-size: 0.8rem;
        color: #64748b;
        display: flex;
        align-items: center;
        gap: 4px;
    }
    /* Modal */
    .payment-modal {
        position: fixed;
        inset: 0;
        background: rgba(15,23,42,0.4);
        backdrop-filter: blur(8px);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        animation: fadeIn 0.3s;
    }
    .modal-glass {
        background: rgba(255,255,255,0.95);
        backdrop-filter: blur(20px);
        border-radius: 24px;
        padding: 32px;
        width: 90%;
        max-width: 480px;
        border: 1px solid rgba(255,255,255,0.5);
        box-shadow: 0 25px 50px rgba(0,0,0,0.15);
        animation: scaleIn 0.3s;
```

```
    }
        .modal-glass h5 {
            margin: 0 0 20px 0;
            font-weight: 800;
            color: #0f172a;
        }
    .form-group {
        margin-bottom: 16px;
    }
        .form-group label {
            display: block;
            font-size: 0.85rem;
            font-weight: 700;
            color: #374151;
            margin-bottom: 6px;
        }
    .form-glass {
        width: 100%;
        padding: 12px 16px;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        background: rgba(255,255,255,0.8);
        font-size: 0.95rem;
        transition: all 0.2s;
        outline: none;
    }
        .form-glass:focus {
            border-color: #0d9488;
            box-shadow: 0 0 0 4px rgba(13,148,136,0.1);
        }
    .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 24px;
    }
    .btn-save {
        background: linear-gradient(135deg, #0d9488, #0891b2);
        color: #fff;
    }
        .btn-save:hover {
            box-shadow: 0 8px 24px rgba(13,148,136,0.4);
            transform: translateY(-2px);
        }
</style>
<script>
    function openPaymentModal()
{ document.getElementById('paymentModal').style.display = 'flex'; }
    function closePaymentModal()
{ document.getElementById('paymentModal').style.display = 'none'; }
    async function submitPayment() {
        const data = {
            VisitId: @Model.Visit.Id,
            Amount: parseFloat(document.getElementById('payAmount').value),
            Method: document.getElementById('payMethod').value,
```

```
            Description: document.getElementById('payDesc').value
        };
        const res = await fetch('/Clinical/AddPayment', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await res.json();
        if (result.success) {
            // Update UI dynamically
            document.getElementById('sumPaid').textContent =
(parseFloat(document.getElementById('sumPaid').textContent.replace(/,/g,'')) +
data.Amount).toLocaleString();
            document.getElementById('sumRemaining').textContent =
result.remaining.toLocaleString();
            closePaymentModal();
            location.reload(); // or prepend new card
        }
    }
</script>
```

```
// _SessionOverview.cshtml
```

```
@using Dental.Modules.Patients.Enums
@model Dental.Modules.Clinical.ViewModels.SessionViewModel
```

```
<div class="session-overview-shell">
```

```
    <!-- Chief Complaint & Examination -->
    <div class="overview-grid">
        <div class="overview-card">
```

```
            <div class="card-head"><i class="bi bi-clipboard-pulse"></i>  شكوى
الرئيسيةالمريض</div>
            <div class="card-body">
```

```
                <textarea class="form-glass" id="chiefComplaint" rows="3"
placeholder="خلعتسوس،ألم،...">@(Model.Visit.ChiefComplaint ?? "")</textarea>
            </div>
        </div>
```

```
        <div class="overview-card">
```

```
            <div class="card-head"><i class="bi bi-stethoscope"></i>  الفحص
السريري</div>
```

```
            <div class="card-body">
                <textarea class="form-glass" id="examination" rows="3"
placeholder="الفحصنتائج...">@(Model.Visit.Examination ?? "")</textarea>
            </div>
```

```
        </div>
    </div>
```

```
    <!-- Notes & Follow Up -->
```

```
    <div class="overview-grid mt-4">
```

```
        <div class="overview-card wide">
```

```
            <div class="card-head"><i class="bi bi-journal-text"></i>  مالحظات
الزيارة</div>
```

```
            <div class="card-body">
```

```
                <textarea class="form-glass" id="visitNotes" rows="4"
placeholder="عامةمالحظات...">@(Model.Visit.Notes ?? "")</textarea>
            </div>
        </div>
        <div class="overview-card">
            <div class="card-head"><i class="bi bi-calendar-check"></i>
المتابعة</div>
            <div class="card-body">
                <div class="form-group mb-3">
                    <label>المتابعةنوع</label>
                    <select class="form-glass" id="followUpType">
```

```
                        <option value="1" selected="@(Model.Visit.FollowUpType
== FollowUpType.Normal)">عادية</option>
                        <option value="2" selected="@(Model.Visit.FollowUpType
== FollowUpType.Long)">طويلة</option>
```

```
                    </select>
                </div>
                <div class="form-group">
                    <label>المتابعةتاريخ</label>
                    <input type="date" class="form-glass" id="followUpDate"
value="@(Model.Visit.FollowUpDate?.ToString("yyyy-MM-dd"))" />
                </div>
            </div>
        </div>
    </div>
    <!-- Status Bar -->
    <div class="status-bar mt-4">
        <div class="status-item">
            <span class="status-label">الزيارةحالة</span>
            <span class="status-badge @Model.Visit.Status.ToString().ToLower()">
                @(Model.Visit.Status switch {
                    Dental.Modules.Patients.Enums.VisitStatus.InProgress =>
"جارية",
                    Dental.Modules.Patients.Enums.VisitStatus.Completed =>
"مكتملة",
                    Dental.Modules.Patients.Enums.VisitStatus.Cancelled =>
"ملغاة",
                    Dental.Modules.Patients.Enums.VisitStatus.NoShow => " لم
يحضر",
                    _ => Model.Visit.Status.ToString()
                })
            </span>
        </div>
        <div class="status-item">
            <span class="status-label">الزيارةنوع</span>
            <span class="type-badge @(Model.Visit.Category ==
Dental.Modules.Patients.Enums.VisitCategory.Orthodontics ? "ortho" :
"general")">
                <i class="bi @(Model.Visit.Category ==
Dental.Modules.Patients.Enums.VisitCategory.Orthodontics ? "bi-braces" : "bi-
app-indicator")"></i>
                @(Model.Visit.Category ==
Dental.Modules.Patients.Enums.VisitCategory.Orthodontics ? "عام" : "تًقويم")
            </span>
        </div>
        <div class="status-item">
            <span class="status-label">الزيارةتاريخ</span>
            <span
class="status-value">@Model.Visit.VisitDate.ToString("dd/MM/yyyy HH:mm")</span>
        </div>
    </div>
    <!-- Save Button -->
    <div class="text-start mt-4">
        <button class="btn-glass btn-save" onclick="saveOverview()">
            <i class="bi bi-check-lg"></i> البياناتحفظ
        </button>
    </div>
</div>
<style>
    .session-overview-shell { padding: 10px; }
    .overview-grid {
        display: grid;
```

```
        grid-template-columns: 1fr 1fr;
        gap: 16px;
    }
    .overview-card {
        background: linear-gradient(135deg, #fff 0%, #f8fafc 100%);
        border-radius: 16px;
        border: 1px solid rgba(226,232,240,0.8);
        overflow: hidden;
        box-shadow: 0 2px 8px rgba(0,0,0,0.03);
    }
    .overview-card.wide { grid-column: 1 / -1; }
    .card-head {
        padding: 14px 18px;
        background: #f8fafc;
        border-bottom: 1px solid #f1f5f9;
        font-weight: 800;
        color: #1e293b;
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.95rem;
    }
    .card-head i { color: #0d9488; }
    .card-body { padding: 18px; }
    .form-glass {
        width: 100%;
        padding: 12px 16px;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        background: rgba(255,255,255,0.8);
        font-size: 0.95rem;
        transition: all 0.2s;
        outline: none;
        resize: vertical;
    }
    .form-glass:focus {
        border-color: #0d9488;
        box-shadow: 0 0 0 4px rgba(13,148,136,0.1);
    }
    .form-group label {
        display: block;
        font-size: 0.82rem;
        font-weight: 700;
        color: #374151;
        margin-bottom: 6px;
    }
    .status-bar {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
    }
    .status-item {
        background: #fff;
        border-radius: 14px;
        padding: 14px 20px;
        border: 1px solid #e2e8f0;
        display: flex;
        flex-direction: column;
        gap: 6px;
        min-width: 160px;
        flex: 1;
    }
    .status-label { font-size: 0.78rem; color: #64748b; font-weight: 700; }
    .status-value { font-size: 1rem; font-weight: 800; color: #0f172a; }
```

```
    .status-badge, .type-badge {
        padding: 4px 12px;
        border-radius: 8px;
        font-size: 0.85rem;
        font-weight: 700;
        display: inline-flex;
        align-items: center;
        gap: 6px;
        width: fit-content;
    }
    .status-badge.inprogress { background: #fef3c7; color: #92400e; }
    .status-badge.completed { background: #d1fae5; color: #047857; }
    .status-badge.cancelled { background: #fee2e2; color: #b91c1c; }
    .status-badge.noshow { background: #f1f5f9; color: #64748b; }
    .type-badge.general { background: #dbeafe; color: #1e40af; }
    .type-badge.ortho { background: #fef3c7; color: #92400e; }
    .btn-glass {
        padding: 10px 22px;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.9rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s;
        display: inline-flex;
        align-items: center;
        gap: 8px;
    }
    .btn-save {
        background: linear-gradient(135deg, #0d9488, #0891b2);
        color: #fff;
        box-shadow: 0 4px 16px rgba(13,148,136,0.3);
    }
    .btn-save:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 24px rgba(13,148,136,0.4);
    }
    @@media (max-width: 768px) {
        .overview-grid { grid-template-columns: 1fr; }
        .overview-card.wide { grid-column: auto; }
    }
</style>
<script>
    async function saveOverview() {
        const data = {
            VisitId: @Model.Visit.Id,
            ChiefComplaint: document.getElementById('chiefComplaint').value,
            Examination: document.getElementById('examination').value,
            Notes: document.getElementById('visitNotes').value,
            FollowUpType:
parseInt(document.getElementById('followUpType').value),
            FollowUpDate: document.getElementById('followUpDate').value || null
        };
        // TODO: Add API endpoint to save overview
        Swal.fire({ title: 'تم', text: 'البياناتحفظتم', icon: 'success',
timer: 1500, showConfirmButton: false });
    }
</script>
// _SessionPrescription.cshtml
```

```
@model Dental.Modules.Clinical.ViewModels.SessionViewModel
```

```
<div class="prescription-shell">
    <div class="rx-toolbar">
        <div class="rx-title">
            <i class="bi bi-file-medical"></i>
            <span>الروشتات (@Model.Visit.Prescriptions.Count)</span>
        </div>
        <button class="btn-add-rx" onclick="openRxModal()">
            <i class="bi bi-plus-lg"></i> روشتةكتابة
        </button>
    </div>
    <div class="rx-list" id="rxList">
        @foreach (var rx in Model.Visit.Prescriptions)
        {
            <div class="rx-card">
                <div class="rx-header">
                    <div class="rx-date">
                        <i class="bi bi-calendar3"></i>
                        @rx.PrescriptionDate.ToString("dd/MM/yyyy HH:mm")
                    </div>
                    <div class="rx-status @rx.Status.ToString().ToLower()">
                        @(rx.Status switch
                        {
Dental.Modules.Patients.DTOs.PrescriptionStatus.Active => "نشطة",
Dental.Modules.Patients.DTOs.PrescriptionStatus.Completed => "مكتملة",
Dental.Modules.Patients.DTOs.PrescriptionStatus.Cancelled => "ملغاة",
                            _ => rx.Status.ToString()
                        })
                    </div>
                </div>
                @if (!string.IsNullOrEmpty(rx.Diagnosis))
                {
                    <div class="rx-diagnosis"><strong>التشخيص:</strong>
@rx.Diagnosis</div>
                }
                <div class="rx-items">
                    @foreach (var item in rx.Items)
                    {
                        <div class="rx-item">
                            <div class="rx-drug-name">
                                <i class="bi bi-capsule"></i>
                                @item.MedicationName
                                <span class="rx-dosage">@item.Dosage</span>
                            </div>
                            <div class="rx-instructions">
                                <span class="rx-chip"><i class="bi
bi-clock"></i> @item.Frequency</span>
                                <span class="rx-chip"><i class="bi bi-calendar-
week"></i> @item.Duration</span>
                                @if (!string.IsNullOrEmpty(item.Instructions))
                                {
                                    <span class="rx-chip info"><i class="bi bi-
info-circle"></i> @item.Instructions</span>
                                }
                            </div>
                        </div>
                    }
                </div>
                @if (!string.IsNullOrEmpty(rx.Notes))
                {
```

```
                    <div class="rx-footer">@rx.Notes</div>
                }
                <div class="rx-actions-bar">
                    <span class="rx-by"><i class="bi bi-person-badge"></i> د.
@rx.PrescribedBy</span>
                    <div class="rx-actions">
                        <button class="rx-btn" onclick="printRx(@rx.Id)"
title="طباعة"><i class="bi bi-printer"></i></button>
                        <button class="rx-btn danger" onclick="deleteRx(@rx.Id)"
title="حذف"><i class="bi bi-trash"></i></button>
                    </div>
                </div>
            </div>
        }
        @if (!Model.Visit.Prescriptions.Any())
        {
            <div class="empty-rx">
                <div class="empty-icon"><i class="bi bi-file-medical"></i></div>
                <h4>مسجلةروشتاتتوجدال</h4>
                <p>للمريضجديدةروشتةاكتب</p>
            </div>
        }
    </div>
</div>
<!-- Add Prescription Modal -->
<div class="rx-modal" id="rxModal" style="display:none;">
    <div class="modal-glass" style="max-width: 600px;">
        <div class="modal-head">
            <h5><i class="bi bi-file-earmark-medical"></i> جديدةروشتة</h5>
            <button class="btn-close-white" onclick="closeRxModal()"><i
class="bi bi-x-lg"></i></button>
        </div>
        <div class="modal-body">
            <div class="form-group mb-3">
                <label>التشخيص</label>
                <input type="text" class="form-glass" id="rxDiagnosis"
placeholder="الحالةتشخيص..." />
            </div>
            <div class="rx-items-editor" id="rxItemsEditor">
                <div class="rx-item-row" data-index="0">
                    <div class="form-row-3">
                        <div class="form-group">
                            <label>الدواء <span
class="text-danger">*</span></label>
                            <input type="text" class="form-glass drug-name"
placeholder="الدواءاسم" />
                        </div>
                        <div class="form-group">
                            <label>الجرعة</label>
                            <input type="text" class="form-glass drug-dosage"
placeholder="500mg" />
                        </div>
                        <div class="form-group">
                            <label>التكرار</label>
                            <input type="text" class="form-glass drug-freq"
placeholder="3 [مراتيوميا" />
                        </div>
                    </div>
                    <div class="form-row-2">
                        <div class="form-group">
                            <label>المدة</label>
                            <input type="text" class="form-glass drug-duration"
```

```
placeholder="7 أيام" />
                        </div>
                        <div class="form-group">
                            <label>التعليمات</label>
                            <input type="text" class="form-glass drug-instr"
placeholder="الأكلبعد..." />
                        </div>
                    </div>
                </div>
            </div>
            <button class="btn-add-drug" onclick="addDrugRow()">
                <i class="bi bi-plus-circle"></i> آخردواءإضافة
            </button>
            <div class="form-group mt-3">
                <label>الروشتةمالحظات</label>
                <textarea class="form-glass" id="rxNotes" rows="2"></textarea>
            </div>
        </div>
        <div class="modal-foot">
            <button class="btn-modal btn-cancel"
onclick="closeRxModal()">إلغاء</button>
            <button class="btn-modal btn-save"
onclick="submitPrescription()">الروشتةحفظ</button>
        </div>
    </div>
</div>
<style>
    .prescription-shell {
        padding: 10px;
    }
    .rx-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 24px;
        flex-wrap: wrap;
        gap: 12px;
    }
    .rx-title {
        display: flex;
        align-items: center;
        gap: 10px;
        font-weight: 800;
        color: #0f172a;
        font-size: 1.1rem;
    }
        .rx-title i {
            color: #0d9488;
        }
    .btn-add-rx {
        background: linear-gradient(135deg, #ec4899, #db2777);
        color: #fff;
        border: none;
        padding: 11px 22px;
        border-radius: 12px;
        font-weight: 700;
        cursor: pointer;
```

```
        display: flex;
        align-items: center;
        gap: 8px;
        box-shadow: 0 4px 16px rgba(236,72,153,0.3);
        transition: all 0.3s;
    }
        .btn-add-rx:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(236,72,153,0.4);
        }
    .rx-list {
        display: flex;
        flex-direction: column;
        gap: 16px;
    }
    .rx-card {
        background: linear-gradient(135deg, #fff 0%, #fdf2f8 100%);
        border-radius: 20px;
        padding: 24px;
        border: 1px solid rgba(226,232,240,0.8);
        box-shadow: 0 4px 12px rgba(0,0,0,0.04);
        transition: all 0.3s;
        animation: slideUp 0.4s ease backwards;
    }
        .rx-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(0,0,0,0.08);
        }
    .rx-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 14px;
    }
    .rx-date {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.85rem;
        color: #64748b;
        font-weight: 700;
    }
        .rx-date i {
            color: #0d9488;
        }
    .rx-status {
        padding: 4px 12px;
        border-radius: 8px;
        font-size: 0.78rem;
        font-weight: 700;
    }
        .rx-status.active {
            background: #dbeafe;
            color: #1e40af;
        }
```

```
        .rx-status.completed {
            background: #d1fae5;
            color: #047857;
        }
        .rx-status.cancelled {
            background: #fee2e2;
            color: #b91c1c;
        }
    .rx-diagnosis {
        background: #fff;
        padding: 10px 14px;
        border-radius: 10px;
        font-size: 0.92rem;
        color: #1e293b;
        margin-bottom: 14px;
        border: 1px solid #f1f5f9;
    }
```

```
    .rx-items {
        display: flex;
        flex-direction: column;
        gap: 10px;
    }
    .rx-item {
        background: #fff;
        border-radius: 14px;
        padding: 14px 18px;
        border: 1px solid #f1f5f9;
        transition: all 0.2s;
    }
        .rx-item:hover {
            border-color: #fbcfe8;
        }
    .rx-drug-name {
        font-weight: 800;
        color: #1e293b;
        font-size: 1rem;
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
    }
        .rx-drug-name i {
            color: #ec4899;
        }
    .rx-dosage {
        font-size: 0.8rem;
        font-weight: 700;
        color: #64748b;
        background: #f1f5f9;
        padding: 2px 8px;
        border-radius: 6px;
    }
    .rx-instructions {
        display: flex;
```

```
        gap: 8px;
        flex-wrap: wrap;
    }
    .rx-chip {
        padding: 4px 10px;
        border-radius: 8px;
        font-size: 0.78rem;
        font-weight: 700;
        display: inline-flex;
        align-items: center;
        gap: 4px;
        background: #f0fdfa;
        color: #0f766e;
    }
        .rx-chip.info {
            background: #eff6ff;
            color: #1d4ed8;
        }
        .rx-chip i {
            font-size: 0.85rem;
        }
    .rx-footer {
        margin-top: 14px;
        padding-top: 14px;
        border-top: 1px dashed #e2e8f0;
        font-size: 0.88rem;
        color: #64748b;
        font-style: italic;
    }
    .rx-actions-bar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-top: 14px;
        padding-top: 14px;
        border-top: 1px solid #f1f5f9;
    }
```

```
    .rx-by {
        font-size: 0.82rem;
        color: #64748b;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 6px;
    }
    .rx-actions {
        display: flex;
        gap: 6px;
    }
    .rx-btn {
        width: 34px;
        height: 34px;
        border-radius: 10px;
        border: none;
        background: transparent;
        color: #64748b;
```

```
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.2s;
    }
        .rx-btn:hover {
            background: #f1f5f9;
            color: #0d9488;
        }
        .rx-btn.danger:hover {
            background: #fee2e2;
            color: #dc2626;
        }
    .empty-rx {
        text-align: center;
        padding: 48px;
        color: #64748b;
    }
    .empty-icon {
        width: 64px;
        height: 64px;
        border-radius: 18px;
        background: #fdf2f8;
        color: #ec4899;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.8rem;
        margin: 0 auto 16px;
    }
```

```
    .empty-rx h4 {
        font-weight: 800;
        color: #1e293b;
        margin-bottom: 6px;
    }
    .empty-rx p {
        margin: 0;
        font-size: 0.9rem;
    }
    /* Modal */
    .rx-modal {
        position: fixed;
        inset: 0;
        background: rgba(15,23,42,0.4);
        backdrop-filter: blur(8px);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        animation: fadeIn 0.3s;
    }
    .modal-glass {
        background: rgba(255,255,255,0.95);
        backdrop-filter: blur(20px);
        border-radius: 24px;
```

```
        padding: 28px;
        width: 90%;
        max-width: 600px;
        border: 1px solid rgba(255,255,255,0.5);
        box-shadow: 0 25px 50px rgba(0,0,0,0.15);
        animation: scaleIn 0.3s;
        max-height: 90vh;
        overflow-y: auto;
    }
    .modal-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;
    }
        .modal-head h5 {
            margin: 0;
            font-weight: 800;
            color: #0f172a;
            font-size: 1.1rem;
        }
    .btn-close-white {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        border: none;
        background: #f1f5f9;
        color: #64748b;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .modal-body {
        margin-bottom: 20px;
    }
    .rx-items-editor {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 16px;
    }
    .rx-item-row {
        background: #f8fafc;
        border-radius: 14px;
        padding: 16px;
        border: 1px solid #e2e8f0;
        position: relative;
    }
    .form-row-3 {
        display: grid;
        grid-template-columns: 2fr 1fr 1fr;
        gap: 12px;
        margin-bottom: 12px;
    }
    .form-row-2 {
```

```
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
    }
    .form-group label {
        display: block;
        font-size: 0.82rem;
        font-weight: 700;
        color: #374151;
        margin-bottom: 6px;
    }
    .form-glass {
        width: 100%;
        padding: 10px 14px;
        border: 1px solid #e2e8f0;
        border-radius: 10px;
        background: rgba(255,255,255,0.8);
        font-size: 0.95rem;
        outline: none;
        transition: all 0.2s;
    }
        .form-glass:focus {
            border-color: #ec4899;
            box-shadow: 0 0 0 3px rgba(236,72,153,0.1);
        }
    .btn-add-drug {
        width: 100%;
        padding: 12px;
        border-radius: 12px;
        border: 2px dashed #e2e8f0;
        background: transparent;
        color: #64748b;
        font-weight: 700;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        transition: all 0.2s;
    }
        .btn-add-drug:hover {
            border-color: #ec4899;
            color: #ec4899;
            background: #fdf2f8;
        }
    .modal-foot {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 20px;
    }
    .btn-modal {
        padding: 10px 22px;
        border-radius: 10px;
        font-weight: 700;
        font-size: 0.9rem;
        border: none;
```

```
        cursor: pointer;
        transition: all 0.2s;
    }
    .btn-cancel {
        background: #f1f5f9;
        color: #475569;
    }
    .btn-save {
        background: linear-gradient(135deg, #ec4899, #db2777);
        color: #fff;
        box-shadow: 0 4px 16px rgba(236,72,153,0.3);
    }
        .btn-save:hover {
            transform: translateY(-1px);
        }
    @@keyframes fadeIn {
        from {
            opacity: 0;
        }
        to {
            opacity: 1;
        }
    }
    @@keyframes scaleIn {
        from {
            opacity: 0;
            transform: scale(0.95);
        }
        to {
            opacity: 1;
            transform: scale(1);
        }
    }
    @@keyframes slideUp {
        from {
            opacity: 0;
            transform: translateY(12px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    @@media (max-width: 768px) {
        .form-row-3, .form-row-2 {
            grid-template-columns: 1fr;
        }
    }
</style>
<script>
    let drugIndex = 1;
    function openRxModal() { document.getElementById('rxModal').style.display =
'flex'; }
```

```
    function closeRxModal() {
        document.getElementById('rxModal').style.display = 'none';
        document.getElementById('rxItemsEditor').innerHTML = getDrugRowHtml(0);
        drugIndex = 1;
        document.getElementById('rxDiagnosis').value = '';
        document.getElementById('rxNotes').value = '';
    }
    function getDrugRowHtml(idx) {
        return `
        <div class="rx-item-row" data-index="${idx}">
            <div class="form-row-3">
                <div class="form-group">
                    <label>الدواء <span class="text-danger">*</span></label>
                    <input type="text" class="form-glass drug-name"
placeholder="الدواءاسم" />
                </div>
                <div class="form-group">
                    <label>الجرعة</label>
                    <input type="text" class="form-glass drug-dosage"
placeholder="500mg" />
                </div>
                <div class="form-group">
                    <label>التكرار</label>
                    <input type="text" class="form-glass drug-freq"
placeholder="3 [مراتيوميا" />
                </div>
            </div>
            <div class="form-row-2">
                <div class="form-group">
                    <label>المدة</label>
                    <input type="text" class="form-glass drug-duration"
placeholder="7 أيام" />
                </div>
                <div class="form-group">
                    <label>التعليمات</label>
                    <input type="text" class="form-glass drug-instr"
placeholder="الأكلبعد..." />
                </div>
            </div>
        </div>`;
    }
    function addDrugRow() {
        const container = document.getElementById('rxItemsEditor');
        const div = document.createElement('div');
        div.innerHTML = getDrugRowHtml(drugIndex++);
        container.appendChild(div.firstElementChild);
    }
    async function submitPrescription() {
        const items = [];
        document.querySelectorAll('.rx-item-row').forEach(row => {
            const name = row.querySelector('.drug-name').value.trim();
            if (!name) return;
            items.push({
                MedicationName: name,
                Dosage: row.querySelector('.drug-dosage').value,
                Frequency: row.querySelector('.drug-freq').value,
                Duration: row.querySelector('.drug-duration').value,
                Instructions: row.querySelector('.drug-instr').value
            });
        });
        if (items.length === 0) {
            Swal.fire({ title: 'مطلوب', text: 'الأقلعلىواحددواءأضف', icon:
'warning', confirmButtonColor: '#ec4899' });
```

```
            return;
        }
        const data = {
            VisitId: @Model.Visit.Id,
            Diagnosis: document.getElementById('rxDiagnosis').value,
            Notes: document.getElementById('rxNotes').value,
            Items: items,
            PrescribedBy: '@(User.Identity?.Name ?? "System")'
        };
        // TODO: Add API endpoint /Clinical/AddPrescription
        Swal.fire({ title: 'تم', text: 'بنجاحالروشتةحفظتم', icon: 'success',
timer: 1500, showConfirmButton: false });
        closeRxModal();
        // location.reload();
    }
    function printRx(id) { window.open(`/Clinical/PrintPrescription?id=${id}`,
'_blank'); }
    async function deleteRx(id) {
        const confirm = await Swal.fire({
            title: 'الحذفتأكيد', text: 'متأكد؟أنتهل', icon: 'warning',
            showCancelButton: true, confirmButtonText: 'نعم', cancelButtonText:
'إلغاء', confirmButtonColor: '#dc2626'
        });
        if (!confirm.isConfirmed) return;
        // TODO: Add delete endpoint
        Swal.fire({ title: 'تم', text: 'الحذفتم', icon: 'success', timer: 1500,
showConfirmButton: false });
    }
</script>
```

```
// _SessionXRays.cshtml
```

```
@model Dental.Modules.Clinical.ViewModels.SessionViewModel
```

```
<div class="xrays-shell">
    <div class="xray-toolbar">
        <div class="xray-title">
            <i class="bi bi-image"></i>
            <span>المريضأشعة (@Model.Visit.XRays.Count)</span>
        </div>
        <button class="btn-upload-xray" onclick="openXrayModal()">
            <i class="bi bi-cloud-upload"></i> جديدةأشعةرفع
        </button>
    </div>
    <div class="xray-grid" id="xrayGrid">
        @foreach (var xray in Model.Visit.XRays)
        {
            <div class="xray-card">
                <div class="xray-thumb">
                    <img src="@xray.ImageUrl" alt="@xray.FileName"
loading="lazy" onerror="this.src='/images/placeholder-xray.png'" />
                    <div class="xray-overlay">
                        <button class="xray-action-btn"
onclick="viewXray('@xray.ImageUrl')" title="عرض"><i class="bi
bi-eye"></i></button>
                        <button class="xray-action-btn"
onclick="deleteXray(@xray.Id)" title="حذف"><i class="bi bi-trash"></i></button>
                    </div>
                    <span class="xray-type-badge">@xray.Type</span>
                </div>
                <div class="xray-info">
                    <div class="xray-name">@xray.FileName</div>
```

```
                    <div class="xray-meta">
                        <span><i class="bi bi-calendar"></i>
@xray.TakenDate.ToString("dd/MM/yyyy")</span>
                        @if (!string.IsNullOrEmpty(xray.Description))
                        {
                            <span class="xray-desc">@xray.Description</span>
                        }
                    </div>
                </div>
            </div>
        }
        @if (!Model.Visit.XRays.Any())
        {
            <div class="empty-xray">
                <div class="empty-icon"><i class="bi bi-image"></i></div>
                <h4>مسجلةأشعةتوجدال</h4>
                <p>الأشعةصوررفعيمكنك (OPG, Periapical, CBCT...)</p>
            </div>
        }
    </div>
</div>
<!-- Upload XRay Modal -->
<div class="xray-modal" id="xrayModal" style="display:none;">
    <div class="modal-glass" style="max-width: 480px;">
        <div class="modal-head">
            <h5><i class="bi bi-cloud-upload"></i> جديدةأشعةرفع</h5>
            <button class="btn-close-white" onclick="closeXrayModal()"><i
class="bi bi-x-lg"></i></button>
        </div>
        <div class="modal-body">
            <div class="upload-zone" id="uploadZone">
                <input type="file" id="xrayFile" accept="image/*,.dcm" hidden />
                <div class="upload-placeholder"
onclick="document.getElementById('xrayFile').click()">
                    <i class="bi bi-cloud-arrow-up"></i>
                    <span>هناالصورةاسحبأواضغط</span>
                    <small>PNG, JPG, JPEG, DICOM</small>
                </div>
                <div class="upload-preview" id="uploadPreview"
style="display:none;">
                    <img id="previewImg" src="" alt="Preview" />
                    <button class="remove-file" onclick="clearFile(event)"><i
class="bi bi-x-circle"></i></button>
                </div>
            </div>
            <div class="form-group mt-3">
                <label>الأشعةنوع</label>
                <select class="form-glass" id="xrayType">
                    <option value="Panoramic">OPG - بانوراما</option>
                    <option value="Periapical">Periapical - واحدسن</option>
                    <option value="Bitewing">Bitewing</option>
                    <option value="Occlusal">Occlusal</option>
                    <option value="Cephalometric">Cephalometric - تًقويم</option>
                    <option value="CBCT">CBCT - الأبعادثالثي</option>
                </select>
            </div>
            <div class="form-group mt-3">
                <label>مالحظات</label>
                <textarea class="form-glass" id="xrayDesc" rows="2"
placeholder="الأشعةوصف..."></textarea>
            </div>
        </div>
        <div class="modal-foot">
```

```
            <button class="btn-modal btn-cancel"
onclick="closeXrayModal()">إلغاء</button>
            <button class="btn-modal btn-save"
onclick="submitXray()">رفع</button>
        </div>
    </div>
</div>
<!-- Lightbox -->
<div class="lightbox" id="lightbox" style="display:none;"
onclick="closeLightbox()">
    <button class="lightbox-close" onclick="closeLightbox()"><i class="bi bi-x-
lg"></i></button>
    <img id="lightboxImg" src="" alt="Full size" />
</div>
<style>
    .xrays-shell {
        padding: 10px;
    }
    .xray-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 24px;
        flex-wrap: wrap;
        gap: 12px;
    }
    .xray-title {
        display: flex;
        align-items: center;
        gap: 10px;
        font-weight: 800;
        color: #0f172a;
        font-size: 1.1rem;
    }
        .xray-title i {
            color: #0d9488;
        }
    .btn-upload-xray {
        background: linear-gradient(135deg, #0d9488, #0891b2);
        color: #fff;
        border: none;
        padding: 11px 22px;
        border-radius: 12px;
        font-weight: 700;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 8px;
        box-shadow: 0 4px 16px rgba(13,148,136,0.3);
        transition: all 0.3s;
    }
        .btn-upload-xray:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(13,148,136,0.4);
        }
    .xray-grid {
```

```
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 16px;
    }
    .xray-card {
        background: linear-gradient(135deg, #fff, #f8fafc);
        border-radius: 16px;
        overflow: hidden;
        border: 1px solid rgba(226,232,240,0.8);
        box-shadow: 0 2px 8px rgba(0,0,0,0.03);
        transition: all 0.3s;
    }
        .xray-card:hover {
            transform: translateY(-4px);
            box-shadow: 0 12px 32px rgba(0,0,0,0.1);
        }
    .xray-thumb {
        position: relative;
        aspect-ratio: 4/3;
        background: #f1f5f9;
        overflow: hidden;
    }
        .xray-thumb img {
            width: 100%;
            height: 100%;
            object-fit: cover;
            transition: transform 0.4s;
        }
    .xray-card:hover .xray-thumb img {
        transform: scale(1.05);
    }
    .xray-overlay {
        position: absolute;
        inset: 0;
        background: rgba(15,23,42,0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 12px;
        opacity: 0;
        transition: opacity 0.3s;
    }
    .xray-card:hover .xray-overlay {
        opacity: 1;
    }
    .xray-action-btn {
        width: 40px;
        height: 40px;
        border-radius: 12px;
        border: none;
        background: rgba(255,255,255,0.95);
        color: #0f172a;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
```

```
        font-size: 1.1rem;
        transition: all 0.2s;
    }
        .xray-action-btn:hover {
            transform: scale(1.1);
            background: #fff;
        }
    .xray-type-badge {
        position: absolute;
        top: 10px;
        right: 10px;
        background: rgba(15,23,42,0.7);
        color: #fff;
        padding: 4px 10px;
        border-radius: 8px;
        font-size: 0.72rem;
        font-weight: 700;
        backdrop-filter: blur(4px);
    }
    .xray-info {
        padding: 14px;
    }
    .xray-name {
        font-weight: 700;
        color: #1e293b;
        font-size: 0.9rem;
        margin-bottom: 6px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
    .xray-meta {
        display: flex;
        flex-direction: column;
        gap: 4px;
        font-size: 0.78rem;
        color: #64748b;
    }
        .xray-meta i {
            color: #0d9488;
        }
    .xray-desc {
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
    .empty-xray {
        grid-column: 1 / -1;
        text-align: center;
        padding: 48px;
        color: #64748b;
    }
    .empty-icon {
        width: 64px;
        height: 64px;
```

```
        border-radius: 18px;
        background: #f0fdfa;
        color: #14b8a6;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.8rem;
        margin: 0 auto 16px;
    }
    .empty-xray h4 {
        font-weight: 800;
        color: #1e293b;
        margin-bottom: 6px;
    }
    .empty-xray p {
        margin: 0;
        font-size: 0.9rem;
    }
    /* Modal */
    .xray-modal {
        position: fixed;
        inset: 0;
        background: rgba(15,23,42,0.4);
        backdrop-filter: blur(8px);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        animation: fadeIn 0.3s;
    }
    .modal-glass {
        background: rgba(255,255,255,0.95);
        backdrop-filter: blur(20px);
        border-radius: 24px;
        padding: 28px;
        width: 90%;
        max-width: 480px;
        border: 1px solid rgba(255,255,255,0.5);
        box-shadow: 0 25px 50px rgba(0,0,0,0.15);
        animation: scaleIn 0.3s;
    }
    .modal-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;
    }
        .modal-head h5 {
            margin: 0;
            font-weight: 800;
            color: #0f172a;
            font-size: 1.1rem;
        }
    .btn-close-white {
        width: 36px;
        height: 36px;
        border-radius: 10px;
```

```
        border: none;
        background: #f1f5f9;
        color: #64748b;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .modal-body {
        margin-bottom: 20px;
    }
    .modal-foot {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
    }
    .upload-zone {
        border: 2px dashed #cbd5e1;
        border-radius: 16px;
        padding: 24px;
        text-align: center;
        transition: all 0.3s;
        cursor: pointer;
        position: relative;
        background: #f8fafc;
    }
        .upload-zone:hover {
            border-color: #0d9488;
            background: #f0fdfa;
        }
    .upload-placeholder i {
        font-size: 2.5rem;
        color: #94a3b8;
        display: block;
        margin-bottom: 10px;
    }
    .upload-placeholder span {
        display: block;
        font-weight: 700;
        color: #475569;
        font-size: 0.95rem;
    }
    .upload-placeholder small {
        color: #94a3b8;
        font-size: 0.8rem;
    }
    .upload-preview {
        position: relative;
        display: inline-block;
    }
        .upload-preview img {
            max-height: 180px;
            border-radius: 12px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        }
```

```
    .remove-file {
        position: absolute;
        top: -8px;
        right: -8px;
        width: 28px;
        height: 28px;
        border-radius: 50%;
        border: none;
        background: #dc2626;
        color: #fff;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
    }
    .form-group label {
        display: block;
        font-size: 0.82rem;
        font-weight: 700;
        color: #374151;
        margin-bottom: 6px;
    }
    .form-glass {
        width: 100%;
        padding: 10px 14px;
        border: 1px solid #e2e8f0;
        border-radius: 10px;
        background: rgba(255,255,255,0.8);
        font-size: 0.95rem;
        outline: none;
        transition: all 0.2s;
    }
        .form-glass:focus {
            border-color: #0d9488;
            box-shadow: 0 0 0 3px rgba(13,148,136,0.1);
        }
    .btn-modal {
        padding: 10px 22px;
        border-radius: 10px;
        font-weight: 700;
        font-size: 0.9rem;
        border: none;
        cursor: pointer;
        transition: all 0.2s;
    }
    .btn-cancel {
        background: #f1f5f9;
        color: #475569;
    }
    .btn-save {
        background: linear-gradient(135deg, #0d9488, #0891b2);
        color: #fff;
        box-shadow: 0 4px 16px rgba(13,148,136,0.3);
    }
        .btn-save:hover {
```

```
            transform: translateY(-1px);
        }
    /* Lightbox */
    .lightbox {
        position: fixed;
        inset: 0;
        background: rgba(15,23,42,0.95);
        z-index: 2000;
        display: flex;
        align-items: center;
        justify-content: center;
        animation: fadeIn 0.3s;
    }
        .lightbox img {
            max-width: 90vw;
            max-height: 90vh;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.5);
        }
    .lightbox-close {
        position: absolute;
        top: 20px;
        right: 20px;
        width: 44px;
        height: 44px;
        border-radius: 12px;
        border: none;
        background: rgba(255,255,255,0.1);
        color: #fff;
        font-size: 1.3rem;
        cursor: pointer;
        backdrop-filter: blur(4px);
        transition: all 0.2s;
    }
        .lightbox-close:hover {
            background: rgba(255,255,255,0.2);
        }
    @@keyframes fadeIn {
        from {
            opacity: 0;
        }
        to {
            opacity: 1;
        }
    }
    @@keyframes scaleIn {
        from {
            opacity: 0;
            transform: scale(0.95);
        }
        to {
            opacity: 1;
            transform: scale(1);
        }
    }
</style>
```

```
<script>
    function openXrayModal()
{ document.getElementById('xrayModal').style.display = 'flex'; }
    function closeXrayModal()
{ document.getElementById('xrayModal').style.display = 'none'; clearFile(); }
    const fileInput = document.getElementById('xrayFile');
    const uploadZone = document.getElementById('uploadZone');
    const uploadPreview = document.getElementById('uploadPreview');
    const previewImg = document.getElementById('previewImg');
    uploadZone.addEventListener('dragover', (e) => { e.preventDefault();
uploadZone.style.borderColor = '#0d9488'; });
    uploadZone.addEventListener('dragleave', () =>
{ uploadZone.style.borderColor = '#cbd5e1'; });
    uploadZone.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadZone.style.borderColor = '#cbd5e1';
        if (e.dataTransfer.files.length) {
            fileInput.files = e.dataTransfer.files;
            showPreview(e.dataTransfer.files[0]);
        }
    });
    fileInput.addEventListener('change', () => {
        if (fileInput.files.length) showPreview(fileInput.files[0]);
    });
    function showPreview(file) {
        const reader = new FileReader();
        reader.onload = (e) => {
            previewImg.src = e.target.result;
            uploadPreview.style.display = 'inline-block';
            document.querySelector('.upload-placeholder').style.display =
'none';
        };
        reader.readAsDataURL(file);
    }
    function clearFile(e) {
        if (e) e.stopPropagation();
        fileInput.value = '';
        previewImg.src = '';
        uploadPreview.style.display = 'none';
        document.querySelector('.upload-placeholder').style.display = 'block';
    }
    async function submitXray() {
        const file = fileInput.files[0];
        if (!file) {
            Swal.fire({ title: 'مطلوب', text: '[اخترأوالالأشعةصورة', icon:
'warning', confirmButtonColor: '#0d9488' });
            return;
        }
        const formData = new FormData();
        formData.append('file', file);
        formData.append('VisitId', @Model.Visit.Id);
        formData.append('PatientId', @Model.Patient.Id);
        formData.append('Type', document.getElementById('xrayType').value);
        formData.append('Description',
document.getElementById('xrayDesc').value);
        // TODO: Replace with actual upload endpoint
        Swal.fire({ title: 'الرفعجاري...', allowOutsideClick: false, didOpen:
() => Swal.showLoading() });
```

```
        // const res = await fetch('/Clinical/UploadXRay', { method: 'POST',
body: formData });
        setTimeout(() => {
            Swal.fire({ title: 'تم', text: 'بنجاحالأشعةرفعتم', icon:
'success', timer: 1500, showConfirmButton: false });
            closeXrayModal();
            location.reload();
        }, 1000);
    }
    function viewXray(url) {
        document.getElementById('lightboxImg').src = url;
        document.getElementById('lightbox').style.display = 'flex';
    }
    function closeLightbox() { document.getElementById('lightbox').style.display
= 'none'; }
    async function deleteXray(id) {
        const confirm = await Swal.fire({
            title: 'الحذفتأكيد', text: 'متأكد؟أنتهل', icon: 'warning',
            showCancelButton: true, confirmButtonText: 'نعم', cancelButtonText:
'إلغاء', confirmButtonColor: '#dc2626'
        });
        if (!confirm.isConfirmed) return;
        // TODO: Add delete endpoint
        Swal.fire({ title: 'تم', text: 'الحذفتم', icon: 'success', timer: 1500,
showConfirmButton: false });
    }
</script>
```

```
 // _WeeklyCalendar.cshtml
<!-- Views/Shared/_WeeklyCalendar.cshtml -->
<div class="weekly-calendar-shell">
    <div class="calendar-header">
        <button class="cal-nav" onclick="shiftWeek(-1)"><i class="bi bi-chevron-
right"></i></button>
        <h3 class="cal-title" id="weekRange">الحاليالأسبوع</h3>
        <button class="cal-nav" onclick="shiftWeek(1)"><i class="bi bi-chevron-
left"></i></button>
    </div>
    <div class="week-grid" id="weekGrid">
        <!-- Generated by JS -->
    </div>
</div>
<style>
    .weekly-calendar-shell {
        background: rgba(255,255,255,0.8);
        backdrop-filter: blur(16px);
        border-radius: 20px;
        border: 1px solid rgba(226,232,240,0.6);
        padding: 20px;
        box-shadow: 0 10px 40px rgba(0,0,0,0.06);
    }
    .calendar-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 20px;
    }
    .cal-title {
```

```
        font-size: 1.1rem;
        font-weight: 800;
        color: #0f172a;
        margin: 0;
    }
    .cal-nav {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        border: 1px solid #e2e8f0;
        background: #fff;
        color: #475569;
        cursor: pointer;
        transition: all 0.2s;
        display: flex;
        align-items: center;
        justify-content: center;
    }
        .cal-nav:hover {
            background: #0d9488;
            color: #fff;
            border-color: #0d9488;
            transform: scale(1.1);
        }
    .week-grid {
        display: grid;
        grid-template-columns: repeat(7, 1fr);
        gap: 12px;
    }
    .day-column {
        background: linear-gradient(180deg, #f8fafc 0%, #fff 100%);
        border-radius: 16px;
        border: 1px solid rgba(226,232,240,0.6);
        min-height: 300px;
        overflow: hidden;
        transition: all 0.3s;
    }
        .day-column:hover {
            transform: translateY(-4px);
            box-shadow: 0 12px 32px rgba(0,0,0,0.08);
            border-color: rgba(13,148,136,0.3);
        }
    .day-header {
        padding: 12px;
        text-align: center;
        border-bottom: 1px solid #f1f5f9;
        background: linear-gradient(135deg, rgba(13,148,136,0.05), transparent);
    }
    .day-name {
        font-size: 0.75rem;
        font-weight: 700;
        color: #64748b;
        text-transform: uppercase;
        letter-spacing: 1px;
    }
    .day-number {
```

```
        font-size: 1.4rem;
        font-weight: 800;
        color: #0f172a;
        line-height: 1;
        margin-top: 4px;
    }
    .day-slots {
        padding: 10px;
        display: flex;
        flex-direction: column;
        gap: 8px;
    }
    .patient-slot {
        background: #fff;
        border-radius: 12px;
        padding: 12px;
        border: 1px solid rgba(226,232,240,0.8);
        cursor: pointer;
        transition: all 0.25s cubic-bezier(0.4,0,0.2,1);
        position: relative;
        overflow: hidden;
    }
        .patient-slot::before {
            content: '';
            position: absolute;
            top: 0;
            right: 0;
            width: 4px;
            height: 100%;
            background: linear-gradient(180deg, #0d9488, #0891b2);
            opacity: 0;
            transition: opacity 0.2s;
        }
        .patient-slot:hover {
            transform: translateX(-4px);
            box-shadow: 0 6px 20px rgba(0,0,0,0.08);
            border-color: rgba(13,148,136,0.3);
        }
            .patient-slot:hover::before {
                opacity: 1;
            }
    .slot-time {
        font-size: 0.75rem;
        font-weight: 700;
        color: #0d9488;
        background: rgba(13,148,136,0.08);
        padding: 2px 8px;
        border-radius: 6px;
        display: inline-block;
        margin-bottom: 6px;
    }
    .slot-name {
        font-weight: 700;
        color: #1e293b;
        font-size: 0.9rem;
        margin-bottom: 4px;
    }
```

```
    .slot-phone {
        font-size: 0.8rem;
        color: #64748b;
        display: flex;
        align-items: center;
        gap: 4px;
        font-family: monospace;
        direction: ltr;
        justify-content: flex-end;
    }
    .slot-type {
        margin-top: 6px;
        font-size: 0.7rem;
        font-weight: 700;
        padding: 2px 8px;
        border-radius: 6px;
        display: inline-block;
    }
    .type-general {
        background: #dbeafe;
        color: #1e40af;
    }
    .type-ortho {
        background: #fef3c7;
        color: #92400e;
    }
</style>
<script>
    // Sample data — replace with AJAX fetch
    const weekData = [
        { day: 'السبت', date: '08', slots: [{time:'10:00', name:'محمدأحمد',
phone:'01001234567', type:'general'}, {time:'11:30', name:'عليسارة',
phone:'01009876543', type:'ortho'}] },
        { day: 'الأحد', date: '09', slots: [{time:'09:00', name:'ساميمحمد',
phone:'01005678901', type:'general'}] },
        { day: 'الإثنين', date: '10', slots: [] },
        { day: 'الثالثاء', date: '11', slots: [{time:'12:00', name:'أحمدفاطمة',
phone:'01003334455', type:'ortho'}, {time:'02:00', name:'محمودخالد',
phone:'01006667788', type:'general'}] },
        { day: 'الأربعاء', date: '12', slots: [{time:'10:30', name:'سيدنورا',
phone:'01001112233', type:'general'}] },
        { day: 'الخميس', date: '13', slots: [] },
        { day: 'الجمعة', date: '14', slots: [{time:'05:00', name:'خالدعمر',
phone:'01004445566', type:'ortho'}] }
    ];
    function renderWeek() {
        const grid = document.getElementById('weekGrid');
        grid.innerHTML = weekData.map(d => `
            <div class="day-column">
                <div class="day-header">
                    <div class="day-name">${d.day}</div>
                    <div class="day-number">${d.date}</div>
                </div>
                <div class="day-slots">
                    ${d.slots.length === 0 ? '<div style="text-align:center;
color:#94a3b8; padding:20px 0; font-size:0.85rem;">مواعيدتوجدال</div>' : ''}
                    ${d.slots.map(s => `
                        <div class="patient-slot"
```

```
onclick="window.location.href='/Patients/Details/1'">
                            <div class="slot-time">${s.time}</div>
                            <div class="slot-name">${s.name}</div>
                            <div class="slot-phone"><i class="bi bi-telephone-
fill"></i> ${s.phone}</div>
                            <span class="slot-type ${s.type === 'ortho' ? 'type-
ortho' : 'type-general'}">
                                ${s.type === 'ortho' ? 'عام' : 'تًقويم'}
                            </span>
                        </div>
                    `).join('')}
                </div>
            </div>
        `).join('');
    }
    renderWeek();
</script>
```

```
// appSetting.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\
\mssqllocaldb;Database=X_FamilyDental_DB;Trusted_Connection=True;MultipleActiveR
esultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

```
// Program.cs
```

```
using Dental.Data;
using Dental.Modules.Appointments.InterfacesAppointments;
using Dental.Modules.Appointments.ServicesAppointments;
using Dental.Modules.Patients.InterfacesPatients;
using Dental.Modules.Patients.ServicesPatients;
using Dental.Modules.Payments.InterfacesPayments;
using Dental.Modules.Payments.ServicesPayments;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
```

```
var builder = WebApplication.CreateBuilder(args);
//var connectionString =
builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ??
throw new InvalidOperationException("Connection string
'ApplicationDbContextConnection' not found.");;
```

```
//Using Local pc
var connectionString =
builder.Configuration.GetConnectionString("DefaultConnection") ??
          throw new InvalidOperationException("Connection string
'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString));
```

```
builder.Services.AddAutoMapper(cfg =>{}, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
#region Validation
```

```
builder.Services.AddFluentValidationAutoValidation();
#endregion
```

```
builder.Services.AddControllersWithViews(); // Adds MVC services\
builder.Services.AddSingleton<IAuthorizationPolicyProvider,
PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler,
PermissionAuthorizationHandler>();
```

```
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    serverOptions.Limits.MaxRequestHeadersTotalSize = 32768;
    serverOptions.Limits.MaxRequestBodySize = 104857600;
});
```

```
// Add services to the container.
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(
    options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();
```

```
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
```

```
//Services To ReloadPage after update any process
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

```
builder.Services.AddLocalization(options => options.ResourcesPath =
"Resources"); // Specify resource file location
builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(); // For localizing validation messages
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
```

```
            new CultureInfo("en-US"),
            new CultureInfo("ar") // Add Arabic culture
        };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new
CookieRequestCultureProvider()); // Use cookie to store culture
});
```

```
#region Scoped Services
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
#endregion
builder.Services.AddHttpClient();
```

```
builder.Services.AddMemoryCache();
```

```
var app = builder.Build();
app.UseSession();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("app");
    try
    {
        var userManager =
services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager =
services.GetRequiredService<RoleManager<ApplicationRole>>();
        await Dental.Seeds.DefaultRoles.SeedAsync(userManager, roleManager);
        await Dental.Seeds.DefaultUsers.SeedUserAsync(userManager, roleManager);
        await Dental.Seeds.DefaultUsers.SeedAdminAsync(userManager,
roleManager);
        logger.LogInformation("Finished Seeding Default Data");
        logger.LogInformation("Application Starting");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "An error occurred seeding the DB");
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

```
//app.MapHub<ProgressHub>("/progressHub");
```

```
app.MapControllerRoute(
```

```
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
```

```
app.Run();
```

