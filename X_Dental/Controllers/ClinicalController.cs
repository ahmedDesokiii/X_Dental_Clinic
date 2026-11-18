using AutoMapper;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;    // ✅ الـ DTOs الجديدة
using Dental.Modules.Clinical.Interfaces;
using Dental.Modules.Clinical.ViewModels;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using Dental.Modules.Payments.DTOs;
using Dental.Modules.Payments.InterfacesPayments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Controllers
{
    public class ClinicalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ClinicalController> _logger;
        private readonly IClinicalRecordService _records;
        private readonly IOrthoService _ortho;
        private readonly IVisitNoteService _notes;
        private readonly IPaymentService _payments;


        public ClinicalController(
            ApplicationDbContext context,
            IMapper mapper,
            IWebHostEnvironment env,
            ILogger<ClinicalController> logger,
            IClinicalRecordService records,
            IOrthoService ortho,
            IVisitNoteService notes,
            IPaymentService payments)
        {
            _context = context;
            _mapper = mapper;
            _env = env;
            _logger = logger;                      // ⬅ وضيف السطر ده
            _records = records;
            _ortho = ortho;               // ⭐ NEW
            _notes = notes;
            _payments = payments;
        }



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
                .Include(v => v.Payments.Where(p => !p.IsDeleted))   // ⭐ الدفعات الملغية متظهرش في سجل الزيارة
                                                                     //.Include(v => v.TreatmentSessions)
                .Include(v => v.XRays)
                .Include(v => v.Prescriptions)
                .FirstOrDefaultAsync(v => v.Id == visitId);

            if (patient == null || visit == null) return NotFound();

            var isOrtho = visit.Category == VisitCategory.Orthodontics;
            var orthoCase = isOrtho ? await _ortho.GetActiveCaseAsync(patientId) : null;
            var orthoAnchorId = orthoCase?.AnchorVisitId ?? 0;
            ViewBag.IsOrthoSession = isOrtho;

            ViewBag.DiagnosisList = await _context.DiagnosisCatalogs.Where(d => d.IsActive).ToListAsync();
            ViewBag.ProcedureList = await _context.ProcedureCatalogs.Where(p => p.IsActive).ToListAsync();
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
                    .Where(d => d.PatientId == patientId && !d.IsDeleted)
                    .OrderByDescending(d => d.Date)
                    .ToListAsync()


            };
            // ⭐ Ortho: المبلغ المطلوب والمدفوعات كلها على الـ Anchor Visit → تاب الدفع يعرض أرقام الحالة كلها
            if (isOrtho && orthoCase != null && orthoAnchorId != visit.Id)
            {
                var anchor = await _context.Visits
                    .AsNoTracking()
                    .Include(v => v.Payments.Where(p => !p.IsDeleted))   // ⭐ استبعاد الملغي
                    .FirstOrDefaultAsync(v => v.Id == orthoAnchorId);
                if (anchor != null)
                {
                    var a = _mapper.Map<VisitDto>(anchor);
                    model.Visit.TotalAmount = a.TotalAmount;
                    model.Visit.PaidAmount = a.PaidAmount;
                    model.Visit.Discount = a.Discount;
                    model.Visit.Payments = a.Payments;
                    // لو Remaining في VisitDto خاصية عادية (مش محسوبة) ضيف: model.Visit.Remaining = a.Remaining;
                    // ولو أي خاصية من اللي فوق read-only وطلع Compile Error → شيل السطر ده وابعتلي الـ VisitDto
                }
            }

            // ⭐ الرصيد السابق: متبقي كل الزيارات التانية من نفس النوع (العام يشوف رصيد العام بس)
            // التقويم: الحالة كلها على الـ Anchor اللي معروض أصلاً → مفيش رصيد سابق منفصل
            if (!isOrtho)
            {
                var category = visit.Category;
                var currentVisitId = visit.Id;
                model.PreviousBalance = await _context.Visits
                    .Where(v => v.PatientId == patientId
                             && v.IsActive
                             && v.Id != currentVisitId
                             && v.Category == category)
                    .SumAsync(v => (decimal?)(v.TotalAmount - v.Discount - v.PaidAmount)) ?? 0m;
            }

            // 📁 لو المريض "عام + تقويم" — اجمع مدفوعات كل الزيارات مقسّمة حسب النوع
            if (patient.PatientType == Models.Patient.PatientType.Both)
            {
                var categoryTotals = await _context.Visits
                    .Where(v => v.PatientId == patientId && v.IsActive)
                    .GroupBy(v => v.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        TotalAmount = g.Sum(v => v.TotalAmount),
                        Discount = g.Sum(v => v.Discount),
                        PaidAmount = g.Sum(v => v.PaidAmount)
                    })
                    .ToListAsync();

                model.CategoryBreakdown = categoryTotals.Select(c => new CategoryPaymentSummaryDto
                {
                    CategoryName = c.Category == VisitCategory.Orthodontics ? "تقويم" : "عام",
                    Icon = c.Category == VisitCategory.Orthodontics ? "bi-braces" : "bi-app-indicator",
                    TotalAmount = c.TotalAmount,
                    Discount = c.Discount,
                    PaidAmount = c.PaidAmount,
                    Remaining = c.TotalAmount - c.Discount - c.PaidAmount
                }).ToList();
            }


            ViewBag.PreviousSessions = await GetPreviousSessionsAsync(patientId);
            ViewBag.VisitNotes = await _notes.GetByPatientAsync(patientId);

            ViewBag.XRayGroups = await GetXRaysGroupedAsync(patientId);

            ViewBag.PatientPaymentsHistory = await (
         from p in _context.VisitPayments
         join v in _context.Visits on p.VisitId equals v.Id
         where v.PatientId == patientId && p.VisitId != visitId && p.VisitId != orthoAnchorId
               && !p.IsDeleted && v.IsActive   // ⭐ الدفعات الملغية (Void) متتحسبش في المدفوعات السابقة
         orderby p.PaymentDate descending
         select new
         {
             p.Amount,
             p.Discount,
             Method = p.Method.ToString(),
             p.Description,
             p.PaymentDate,
             p.ReceiptNo,          // ⭐ المرحلة 5 — تجميع الإيصال الواحد في صف واحد
             p.VisitId,
             VisitTotalAmount = v.TotalAmount,
             VisitPaidAmount = v.PaidAmount,
             VisitDiscount = v.Discount,
             VisitRemaining = v.Remaining
         }
     ).ToListAsync();



            // ⭐ خريطة أحدث حالة/تشخيص لكل سن — تُستخدم في الـ Tooltip عند الهوفر
            var statusLabelsForTooltip = new Dictionary<Dental.Models.Patient.ToothStatus, string>
{
    { Dental.Models.Patient.ToothStatus.Healthy, "Healthy" },
    { Dental.Models.Patient.ToothStatus.Decayed, "Decayed" },
    { Dental.Models.Patient.ToothStatus.Treated, "Treated" },
    { Dental.Models.Patient.ToothStatus.Missing, "Missing" },
    { Dental.Models.Patient.ToothStatus.Extracted, "Extracted" },
    { Dental.Models.Patient.ToothStatus.RootCanal, "RootCanal" },
    { Dental.Models.Patient.ToothStatus.Crown, "Crown" },
    { Dental.Models.Patient.ToothStatus.Filling, "Filling" }
};

            var latestPerTooth = model.DentalHistory
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




            return View(model);
        }










        //[HttpGet]
        //public async Task<IActionResult> Session(int patientId, int visitId)
        //{

        //    var patient = await _context.Patients
        //        .Include(p => p.Gender)
        //        .Include(p => p.BloodGroup)
        //        .Include(p => p.MedicalInfo)
        //        .FirstOrDefaultAsync(p => p.Id == patientId);

        //    var visit = await _context.Visits
        //        .Include(v => v.Diagnoses).ThenInclude(d => d.Diagnosis)
        //        .Include(v => v.Procedures).ThenInclude(p => p.Procedure)
        //        .Include(v => v.Payments)
        //        .Include(v => v.TreatmentSessions)
        //        .Include(v => v.Procedures)
        //        .Include(v => v.Payments)
        //        .Include(v => v.Procedures)
        //        .Include(v => v.XRays)
        //        .Include(v => v.Prescriptions)
        //        .FirstOrDefaultAsync(v => v.Id == visitId);

        //    if (patient == null || visit == null) return NotFound();

        //    // Master Data للـ dropdowns
        //    ViewBag.DiagnosisList = await _context.DiagnosisCatalogs
        //        .Where(d => d.IsActive).ToListAsync();
        //    ViewBag.ProcedureList = await _context.ProcedureCatalogs
        //        .Where(p => p.IsActive).ToListAsync();
        //    ViewBag.ToothList = Enumerable.Range(11, 8)
        //        .Concat(Enumerable.Range(21, 8))
        //        .Concat(Enumerable.Range(31, 8))
        //        .Concat(Enumerable.Range(41, 8))
        //        .ToList();

        //    var model = new SessionViewModel
        //    {
        //        Patient = _mapper.Map<PatientDetailsDto>(patient),
        //        Visit = _mapper.Map<VisitDto>(visit),
        //        DentalHistory = await _context.DentalChartEntries
        //            .Where(d => d.PatientId == patientId)
        //            .OrderByDescending(d => d.Date)
        //            .ToListAsync()
        //    };

        //    return View(model);
        //}



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
    { Dental.Models.Patient.ToothStatus.Healthy, "Healthy" },
    { Dental.Models.Patient.ToothStatus.Decayed, "Decayed" },
    { Dental.Models.Patient.ToothStatus.Treated, "Treated" },
    { Dental.Models.Patient.ToothStatus.Missing, "Missing" },
    { Dental.Models.Patient.ToothStatus.Extracted, "Extracted" },
    { Dental.Models.Patient.ToothStatus.RootCanal, "RootCanal" },
    { Dental.Models.Patient.ToothStatus.Crown, "Crown" },
    { Dental.Models.Patient.ToothStatus.Filling, "Filling" }
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


        public class UpdateVisitDiagnosisRequest
        {
            public int Id { get; set; }
            public int DiagnosisCatalogId { get; set; }
            public string? Notes { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVisitDiagnosis([FromBody] UpdateVisitDiagnosisRequest req)
        {
            var result = await _records.UpdateRecordAsync(new ChartUpdateRecordRequest
            {
                VisitDiagnosisId = req.Id,
                DiagnosisCatalogId = req.DiagnosisCatalogId,
                Notes = req.Notes
            });
            return Json(new { success = result.Success, message = result.Message, units = result.Data });
        }


        [HttpPost]
        public async Task<IActionResult> SaveClinicalRecord([FromBody] SaveClinicalRecordRequest req)
        {
            // ---------- Validation ----------
            if (req == null || req.ToothRecords == null || !req.ToothRecords.Any())
                return Json(new { success = false, message = "لا توجد أي بيانات لحفظها." });

            if (req.PatientId <= 0)
                return Json(new { success = false, message = "معرّف المريض غير صالح." });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == req.PatientId && p.IsActive);
            if (patient == null)
                return Json(new { success = false, message = "المريض غير موجود." });

            var visit = await _context.Visits.FindAsync(req.VisitId);
            if (visit == null)
                return Json(new { success = false, message = "الزيارة/السيشن غير موجودة." });

            var savedSummary = new List<object>();

            // كل سنة (سن) بييجي منها: Dental Chart + Diagnosis (اختياري) + XRay (اختياري) + Prescription (اختياري)
            foreach (var tooth in req.ToothRecords)
            {
                if (tooth.ToothNumber < 11 || tooth.ToothNumber > 48)
                    return Json(new { success = false, message = $"رقم السن {tooth.ToothNumber} غير صالح." });

                // 1) الدينتال شارت — دايمًا بيتحفظ لأنه هو أساس السجل
                var chartEntry = new DentalChartEntry
                {
                    PatientId = req.PatientId,
                    VisitId = req.VisitId,
                    ToothNumber = tooth.ToothNumber,
                    Status = tooth.Status,
                    SurfacesJson = System.Text.Json.JsonSerializer.Serialize(tooth.Surfaces),
                    ProcedureCode = tooth.ProcedureCode,
                    Notes = tooth.Notes,
                    Date = DateTime.Now
                };
                _context.DentalChartEntries.Add(chartEntry);

                // 2) التشخيص — لو المستخدم اختار تشخيص لهذه السنة
                if (tooth.DiagnosisCatalogId.HasValue && tooth.DiagnosisCatalogId.Value > 0)
                {
                    var diagEntity = await _context.DiagnosisCatalogs
                        .FirstOrDefaultAsync(d => d.Id == tooth.DiagnosisCatalogId.Value);

                    if (diagEntity != null)
                    {
                        var vd = new VisitDiagnosis
                        {
                            VisitId = req.VisitId,
                            DiagnosisCatalogId = tooth.DiagnosisCatalogId.Value,
                            ToothNumber = tooth.ToothNumber,
                            Surface = tooth.DiagnosisSurface,
                            Notes = tooth.DiagnosisNotes,
                            IsPrimary = tooth.DiagnosisIsPrimary,
                            LabPriceSnapshot = await GetCurrentLabPriceAsync(diagEntity)   // ⭐ lab price, not diagnosis price
                        };
                        _context.VisitDiagnoses.Add(vd);
                    }
                }

                // 3) الأشعة — لو المستخدم رفع صورة لهذه السنة
                // 3) لو المستخدم رفع صور لهذه السنة — الأشعة (تدعم أكتر من صورة)
                foreach (var xr in tooth.XRays)
                {
                    if (string.IsNullOrWhiteSpace(xr.Base64) || string.IsNullOrWhiteSpace(xr.FileName))
                        continue;

                    try
                    {
                        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "xrays");
                        Directory.CreateDirectory(uploadsRoot);

                        var safeName = Dental.Core.Security.UploadSafety.XRayFileName(req.PatientId, tooth.ToothNumber, xr.FileName);
                        if (safeName == null) continue;   // 🔒 A2: امتداد غير مسموح
                        var fullPath = Path.Combine(uploadsRoot, safeName);

                        var base64Data = xr.Base64.Contains(",")
                            ? xr.Base64.Substring(xr.Base64.IndexOf(",") + 1)
                            : xr.Base64;

                        await System.IO.File.WriteAllBytesAsync(fullPath, Convert.FromBase64String(base64Data));

                        var xray = new PatientXRay
                        {
                            PatientId = req.PatientId,
                            VisitId = req.VisitId,
                            FileName = xr.FileName,
                            FilePath = $"/uploads/xrays/{safeName}",
                            TakenDate = DateTime.Now,
                            Notes = $"سن {tooth.ToothNumber}" + (string.IsNullOrWhiteSpace(xr.Notes) ? "" : $" — {xr.Notes}")
                        };
                        _context.PatientXRays.Add(xray);
                    }
                    catch
                    {
                        // لو فشل رفع صورة واحدة منمنعش باقي بيانات السن
                    }
                }

                // 4) الروشتة — لو المستخدم ضاف أدوية مرتبطة بهذه السنة
                if (tooth.PrescriptionItems != null && tooth.PrescriptionItems.Any(i => !string.IsNullOrWhiteSpace(i.MedicationName)))
                {
                    var prescription = new Prescription
                    {
                        VisitId = req.VisitId,
                        PrescriptionDate = DateTime.Now,
                        Diagnosis = $"سن {tooth.ToothNumber}",
                        Notes = tooth.Notes,
                        Status = PrescriptionStatus.Active,
                        PrescribedBy = User?.Identity?.Name ?? "System"
                    };
                    foreach (var item in tooth.PrescriptionItems)
                    {
                        if (string.IsNullOrWhiteSpace(item.MedicationName)) continue;
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
                }

                savedSummary.Add(new { tooth = tooth.ToothNumber, status = tooth.Status.ToString() });
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, savedTeeth = savedSummary.Count, details = savedSummary });
        }




        [HttpPost]
        public async Task<IActionResult> CompleteVisit(int visitId)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit == null)
                return Json(new { success = false, message = "الزيارة غير موجودة." });

            visit.Status = VisitStatus.Completed;
            visit.EndDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }






        [HttpPost]
        public async Task<IActionResult> AddDiagnosis([FromBody] AddDiagnosisRequest req)
        {
            // التحقق من صحة البيانات
            if (req == null || req.DiagnosisId <= 0 || req.VisitId <= 0)
                return Json(new { success = false, message = "بيانات غير صالحة." });

            var diagnosis = await _context.DiagnosisCatalogs
                .FirstOrDefaultAsync(d => d.Id == req.DiagnosisId && d.IsActive);
            if (diagnosis == null)
                return Json(new { success = false, message = "التشخيص غير موجود." });

            var vd = new VisitDiagnosis
            {
                VisitId = req.VisitId,
                DiagnosisCatalogId = req.DiagnosisId,
                ToothNumber = req.ToothNumber,
                Surface = req.Surface,
                Notes = req.Notes,
                IsPrimary = req.IsPrimary,
                LabPriceSnapshot = await GetCurrentLabPriceAsync(diagnosis)   // ⭐ NEW
            };

            _context.VisitDiagnoses.Add(vd);
            await _context.SaveChangesAsync();

            return Json(new { success = true, data = vd });
        }


        [HttpPost]
        public async Task<IActionResult> AddProcedure([FromBody] AddProcedureRequest req)
        {
            var proc = await _context.ProcedureCatalogs.FindAsync(req.ProcedureId);
            if (proc == null) return Json(new { success = false, message = "Procedure not found" });

            var vp = new VisitProcedure
            {
                VisitId = req.VisitId,
                ProcedureCatalogId = req.ProcedureId,
                ToothNumber = req.ToothNumber,
                Surface = req.Surface,
                Quantity = req.Quantity,
                UnitPrice = req.Price ?? proc.DefaultPrice,
                Status = ProcedureStatus.Planned,  // ✅ من Models.Patients
                Notes = req.Notes
            };
            _context.VisitProcedures.Add(vp);

            var visit = await _context.Visits.FindAsync(req.VisitId);
            if (visit != null) visit.TotalAmount += vp.TotalPrice;

            await _context.SaveChangesAsync();
            return Json(new { success = true, data = vp, remaining = 0 });
        }

        // ⭐ المرحلة 5 — Endpoint قديم: بقى يمر على نفس محرك الدفع الموحّد بدل ما يكتب الحركة بنفسه
        [HttpPost]
        public async Task<IActionResult> AddPayment([FromBody] AddPaymentRequest req)
        {
            var result = await _payments.AddPaymentAsync(new AddPaymentDto
            {
                VisitId = req.VisitId,
                Amount = req.Amount,
                Method = req.Method,
                Description = req.Description
            });
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message, receiptNo = result.Data!.ReceiptNo, remaining = result.Data.VisitRemaining });
        }





        // ============================================================
        // ⭐ المرحلة 2 — ملاحظات المتابعة (بتتحفظ فورًا، مستقلة عن FinishSession)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetVisitNotes(int patientId)
        {
            var data = await _notes.GetByPatientAsync(patientId);
            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkableDiagnoses(int patientId)
        {
            var data = await _notes.GetLinkableDiagnosesAsync(patientId);
            return Json(new { success = true, data });
        }

        [HttpPost]
        public async Task<IActionResult> SaveVisitNote([FromBody] SaveVisitNoteRequest req)
        {
            if (req == null)
                return Json(new { success = false, message = "بيانات غير صالحة." });
            var result = await _notes.SaveAsync(req);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVisitNote(int visitId, string id)
        {
            var result = await _notes.DeleteAsync(visitId, id);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> FinishSession()
        {
            string rawBody;
            using (var reader = new StreamReader(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            FinishSessionRequest req;
            try
            {
                req = System.Text.Json.JsonSerializer.Deserialize<FinishSessionRequest>(rawBody, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FinishSession JSON parse failed. Raw body: {Body}", rawBody);
                return Json(new { success = false, message = "خطأ في قراءة بيانات السيشن: " + ex.Message });
            }

            if (req == null || req.PatientId <= 0 || req.VisitId <= 0)
                return Json(new { success = false, message = "بيانات غير صالحة." });

            var visit = await _context.Visits.FindAsync(req.VisitId);
            if (visit == null)
                return Json(new { success = false, message = "الزيارة غير موجودة." });

            // ⭐ Multi-Unit validation قبل ما نفتح الـ Transaction
            var unitGroups = req.ToothRecords
                .Where(t => t.GroupId.HasValue)
                .GroupBy(t => t.GroupId!.Value)
                .ToList();

            foreach (var g in unitGroups)
            {
                if (g.Select(t => t.DiagnosisCatalogId).Distinct().Count() > 1)
                    return Json(new { success = false, message = "المجموعة الواحدة لازم يكون ليها تشخيص واحد." });

                if (!IsContiguousGroup(g.Select(t => t.ToothNumber)))
                    return Json(new { success = false, message = "أسنان المجموعة لازم تكون متجاورة في نفس الفك." });
            }

            // ⭐ تحميل التشخيصات مرة واحدة بدل Query لكل سن
            var diagIds = req.ToothRecords
                .Where(t => t.DiagnosisCatalogId.HasValue && t.DiagnosisCatalogId.Value > 0)
                .Select(t => t.DiagnosisCatalogId!.Value)
                .Distinct()
                .ToList();
            var diagLookup = await _context.DiagnosisCatalogs
                .Where(d => diagIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ================= 1) أسنان السيشن =================
                foreach (var tooth in req.ToothRecords)
                {
                    if (tooth.ToothNumber < 11 || tooth.ToothNumber > 48)
                        continue;

                    _context.DentalChartEntries.Add(new DentalChartEntry
                    {
                        PatientId = req.PatientId,
                        VisitId = req.VisitId,
                        ToothNumber = tooth.ToothNumber,
                        Status = tooth.Status,
                        SurfacesJson = System.Text.Json.JsonSerializer.Serialize(tooth.Surfaces),
                        ProcedureCode = tooth.ProcedureCode,
                        Notes = tooth.Notes,
                        GroupId = tooth.GroupId,          // ⭐ Multi-Unit
                        Date = DateTime.Now
                    });

                    // التشخيص + ربط سعره بإجمالي الزيارة
                    // ⭐ السعر للسن الواحد → كل سن في المجموعة بيضيف سعره (الإجمالي = السعر × عدد الوحدات)
                    if (tooth.DiagnosisCatalogId.HasValue
                        && diagLookup.TryGetValue(tooth.DiagnosisCatalogId.Value, out var diag))
                    {
                        _context.VisitDiagnoses.Add(new VisitDiagnosis
                        {
                            VisitId = req.VisitId,
                            DiagnosisCatalogId = diag.Id,
                            ToothNumber = tooth.ToothNumber,
                            Surface = tooth.DiagnosisSurface,
                            Notes = tooth.DiagnosisNotes,
                            IsPrimary = tooth.DiagnosisIsPrimary,
                            GroupId = tooth.GroupId,      // ⭐ Multi-Unit
                            PriceSnapshot = diag.Price,
                            LabPriceSnapshot = await GetCurrentLabPriceAsync(diag)   // ⭐ lab price, not diagnosis price
                        });

                        visit.TotalAmount += diag.Price; // ⭐ الربط بالمدفوعات
                    }

                    // الأشعة (أكتر من صورة، بملاحظة مستقلة لكل واحدة)
                    foreach (var xr in tooth.XRays)
                    {
                        if (string.IsNullOrWhiteSpace(xr.Base64) || string.IsNullOrWhiteSpace(xr.FileName))
                            continue;

                        try
                        {
                            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "xrays");
                            Directory.CreateDirectory(uploadsRoot);

                            var safeName = Dental.Core.Security.UploadSafety.XRayFileName(req.PatientId, tooth.ToothNumber, xr.FileName);
                        if (safeName == null) continue;   // 🔒 A2: امتداد غير مسموح
                            var fullPath = Path.Combine(uploadsRoot, safeName);

                            var base64Data = xr.Base64.Contains(",")
                                ? xr.Base64.Substring(xr.Base64.IndexOf(",") + 1)
                                : xr.Base64;

                            await System.IO.File.WriteAllBytesAsync(fullPath, Convert.FromBase64String(base64Data));

                            _context.PatientXRays.Add(new PatientXRay
                            {
                                PatientId = req.PatientId,
                                VisitId = req.VisitId,
                                ToothNumber = tooth.ToothNumber,
                                FileName = xr.FileName,
                                FilePath = $"/uploads/xrays/{safeName}",
                                TakenDate = DateTime.Now,
                                Notes = $"سن {tooth.ToothNumber}" + (string.IsNullOrWhiteSpace(xr.Notes) ? "" : $" — {xr.Notes}")
                            });
                        }
                        catch
                        {
                            // فشل رفع صورة واحدة ميوقفش باقي بيانات السن
                        }
                    }

                    // الروشتة
                    if (tooth.PrescriptionItems != null && tooth.PrescriptionItems.Any(i => !string.IsNullOrWhiteSpace(i.MedicationName)))
                    {
                        var prescription = new Prescription
                        {
                            VisitId = req.VisitId,
                            PrescriptionDate = DateTime.Now,
                            Diagnosis = $"سن {tooth.ToothNumber}",
                            Notes = tooth.Notes,
                            Status = PrescriptionStatus.Active,
                            PrescribedBy = User?.Identity?.Name ?? "System"
                        };

                        foreach (var item in tooth.PrescriptionItems)
                        {
                            if (string.IsNullOrWhiteSpace(item.MedicationName)) continue;
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
                    }
                }

                // ================= 2) مدفوعات السيشن =================
                // ⭐ المرحلة 5 — نفس محرك الدفع الموحّد بتاع الـ Details وكشف الحساب (PaymentService.AllocateAsync):
                //  - الخصم على الزيارة الحالية.
                //  - المبلغ يغطي متبقي الزيارة الحالية الأول.
                //  - الزيادة تسدد الزيارات القديمة من نفس النوع (الأقدم فالأحدث) بنفس رقم الإيصال.
                //  - لو فضل حاجة بعد سداد كل القديم → تفضل على الزيارة الحالية (رصيد دائن للمريض).
                decimal settledOld = 0;
                var sessionDate = DateTime.Now;
                foreach (var pay in req.Payments.Where(p => p.Amount > 0 || p.Discount > 0))
                {
                    var alloc = await _payments.AllocateAsync(
                        visit,
                        Math.Max(0m, pay.Amount),
                        Math.Max(0m, pay.Discount),
                        pay.Method,
                        pay.Description,
                        sessionDate);
                    settledOld += alloc.SettledOld;
                }

                // ================= 3) إنهاء الزيارة =================
                visit.Status = VisitStatus.Completed;
                visit.EndDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, settledOld });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error finishing session");
                return Json(new { success = false, message = "حدث خطأ أثناء حفظ وإنهاء السيشن." });
            }
        }




        [HttpPost]
        public async Task<IActionResult> SaveDentalChart([FromBody] SaveChartRequest req)
        {
            // ⭐ Validation
            if (req == null || req.Teeth == null || !req.Teeth.Any())
                return Json(new { success = false, message = "لا توجد أسنان محددة." });

            if (req.PatientId <= 0)
                return Json(new { success = false, message = "معرف المريض غير صالح." });

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == req.PatientId && p.IsActive);
            if (!patientExists)
                return Json(new { success = false, message = "المريض غير موجود." });

            foreach (var tooth in req.Teeth)
            {
                if (tooth.Number < 11 || tooth.Number > 48)
                    return Json(new { success = false, message = $"رقم السن {tooth.Number} غير صالح." });

                var entry = new DentalChartEntry
                {
                    PatientId = req.PatientId,
                    VisitId = req.VisitId,
                    ToothNumber = tooth.Number,
                    Status = tooth.Status,
                    SurfacesJson = System.Text.Json.JsonSerializer.Serialize(tooth.Surfaces),
                    ProcedureCode = tooth.ProcedureCode,
                    Notes = tooth.Notes,
                    Date = DateTime.Now
                };
                _context.DentalChartEntries.Add(entry);
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ⭐ NEW: Add Prescription Endpoint
        [HttpPost]
        public async Task<IActionResult> AddPrescription([FromBody] AddPrescriptionRequest req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
                return Json(new { success = false, message = "الروشتة فارغة." });

            if (req.VisitId <= 0)
                return Json(new { success = false, message = "معرف الزيارة غير صالح." });

            var visit = await _context.Visits.FindAsync(req.VisitId);
            if (visit == null)
                return Json(new { success = false, message = "الزيارة غير موجودة." });

            var prescription = new Prescription
            {
                VisitId = req.VisitId,
                PrescriptionDate = DateTime.Now,
                Diagnosis = req.Diagnosis,
                Notes = req.Notes,
                Status = PrescriptionStatus.Active,
                PrescribedBy = req.PrescribedBy ?? "System"
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
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(request.PatientId);
                if (patient == null)
                    return Json(new { success = false, message = "Patient not found" });

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

        // ============================================================
        // PATCH: PatientsController.cs
        // استبدل method GetXRaysGroupedAsync بالكامل بالنسخة دي
        // ============================================================
        // السبب: الشرط القديم (ToothNumber != null) كان بيخفي كل الأشعة المرحّلة
        // من النظام القديم لأن PatientRays_T مفيهوش رقم سن أصلاً.
        // ============================================================

        private async Task<List<XRayGroupDto>> GetXRaysGroupedAsync(int patientId)
        {
            var xrays = await _context.PatientXRays
                .AsNoTracking()
                .Where(x => x.PatientId == patientId && !x.IsDeleted)
                .OrderByDescending(x => x.TakenDate)
                .ToListAsync();

            if (!xrays.Any())
                return new List<XRayGroupDto>();

            // تجميع:
            //  - الأشعة اللي ليها سن → مجموعة لكل سن
            //  - الأشعة بدون سن (NULL أو 0) → مجموعة واحدة ToothNumber = 0 ("بدون سن")
            return xrays
                .GroupBy(x => x.ToothNumber.HasValue && x.ToothNumber.Value > 0
                    ? x.ToothNumber.Value
                    : 0)
                .OrderBy(g => g.Key == 0 ? int.MaxValue : g.Key) // بدون سن في الآخر
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
                })
                .ToList();
        }


        // ⭐ سعر المعمل وقت التشخيص (Snapshot)
        // لو التشخيص لسه ملوش سعر معمل → 0 = "مفيش Snapshot"،
        // والحالة تمشي مع آخر سعر معمل لحد ما يتحدد (مش بناخد سعر المريض أبدًا)
        private async Task<decimal> GetCurrentLabPriceAsync(DiagnosisCatalog diag)
        {
            if (!diag.IsLaboratoryRelated) return 0;

            var lastLabPrice = await _context.LaboratoryPriceHistories
                .Where(h => h.DiagnosisCatalogId == diag.Id)
                .OrderByDescending(h => h.EffectiveDate)
                .Select(h => (decimal?)h.Price)
                .FirstOrDefaultAsync();

            return lastLabPrice ?? 0;
        }

        // ════════════════════════════════════════════════════════════
        // ⭐ Multi-Unit helpers
        // ════════════════════════════════════════════════════════════
        // ترتيب الأسنان الفعلي على امتداد كل فك (من الطرف للطرف)
        private static readonly int[] UpperArchOrder =
            Enumerable.Range(11, 8).Reverse().Concat(Enumerable.Range(21, 8)).ToArray(); // 18..11, 21..28
        private static readonly int[] LowerArchOrder =
            Enumerable.Range(41, 8).Reverse().Concat(Enumerable.Range(31, 8)).ToArray(); // 48..41, 31..38

        // الأسنان لازم تكون في نفس الفك ومتجاورة من غير فراغات (بيعدّي خط المنتصف عادي: 11 ↔ 21)
        private static bool IsContiguousGroup(IEnumerable<int> teeth)
        {
            var list = teeth.Distinct().ToList();
            if (list.Count < 2) return true;

            foreach (var arch in new[] { UpperArchOrder, LowerArchOrder })
            {
                var idx = list.Select(t => Array.IndexOf(arch, t)).ToList();
                if (idx.All(i => i >= 0))
                {
                    idx.Sort();
                    return idx[^1] - idx[0] == idx.Count - 1;
                }
            }
            return false; // أسنان من فكين مختلفين
        }

        // ─────────────────────────────────────────────
        // 🖨️ Print Prescription (+ QR)
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PrintPrescription(int id)
        {
            var prescription = await _context.Prescriptions
                .AsNoTracking()
                .Include(p => p.Items)
                .Include(p => p.Visit)
                    .ThenInclude(v => v.Patient)
                        .ThenInclude(pt => pt.Gender)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
                return NotFound();

            // بيانات العيادة — عدّلها حسب عيادتك
            ViewBag.ClinicName = "عيادة الأسنان";
            ViewBag.ClinicAddress = "العنوان هنا";
            ViewBag.ClinicPhone = "01xxxxxxxxx";
            ViewBag.ClinicLogo = "/images/clinic-logo.png";

            // رابط التحقق + توكن بسيط (يمكن تغيير السر لاحقاً)
            var verifyToken = GenerateRxVerifyToken(id, prescription.PrescriptionDate);
            var verifyUrl = Url.Action("VerifyPrescription", "Clinical",
                new { id, t = verifyToken }, Request.Scheme);

            ViewBag.VerifyUrl = verifyUrl;
            ViewBag.VerifyToken = verifyToken;

            return View(prescription);
        }

        // ─────────────────────────────────────────────
        // ✅ Verify Prescription (يفتح من QR)
        // ─────────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]   // 🔓 بيتفتح من QR الروشتة بدون تسجيل دخول
        public async Task<IActionResult> VerifyPrescription(int id, string t)
        {
            var prescription = await _context.Prescriptions
                .AsNoTracking()
                .Include(p => p.Items)
                .Include(p => p.Visit)
                    .ThenInclude(v => v.Patient)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
                return View("VerifyPrescription", null); // صفحة "غير موجودة"

            var expected = GenerateRxVerifyToken(id, prescription.PrescriptionDate);
            if (string.IsNullOrEmpty(t) || !string.Equals(t, expected, StringComparison.Ordinal))
            {
                ViewBag.InvalidToken = true;
                return View("VerifyPrescription", null);
            }

            ViewBag.ClinicName = "عيادة الأسنان";
            return View(prescription);
        }

        // توكن تحقق بسيط (SHA256) — غيّر السر في الإنتاج
        private static string GenerateRxVerifyToken(int prescriptionId, DateTime date)
        {
            const string secret = "DentalClinic-Rx-Verify-2024"; // غيّره لسري قوي
            var raw = $"{prescriptionId}|{date:yyyyMMdd}|{secret}";
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant(); // 16 حرف كافية
        }
    }
}