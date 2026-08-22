using AutoMapper;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;    // ✅ الـ DTOs الجديدة
using Dental.Modules.Clinical.ViewModels;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
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


        public ClinicalController(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment env)
        {
            _context = context;
            _mapper = mapper;
            _env = env;
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

            return View(model);
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
                    var diagExists = await _context.DiagnosisCatalogs.AnyAsync(d => d.Id == tooth.DiagnosisCatalogId.Value);
                    if (diagExists)
                    {
                        var vd = new VisitDiagnosis
                        {
                            VisitId = req.VisitId,
                            DiagnosisCatalogId = tooth.DiagnosisCatalogId.Value,
                            ToothNumber = tooth.ToothNumber,
                            Surface = tooth.DiagnosisSurface,
                            Notes = tooth.DiagnosisNotes,
                            IsPrimary = tooth.DiagnosisIsPrimary
                        };
                        _context.VisitDiagnoses.Add(vd);
                    }
                }

                // 3) الأشعة — لو المستخدم رفع صورة لهذه السنة
                if (!string.IsNullOrWhiteSpace(tooth.XRayBase64) && !string.IsNullOrWhiteSpace(tooth.XRayFileName))
                {
                    try
                    {
                        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "xrays");
                        Directory.CreateDirectory(uploadsRoot);

                        var safeName = $"{req.PatientId}_{tooth.ToothNumber}_{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(tooth.XRayFileName)}";
                        var fullPath = Path.Combine(uploadsRoot, safeName);

                        var base64Data = tooth.XRayBase64.Contains(",")
                            ? tooth.XRayBase64.Substring(tooth.XRayBase64.IndexOf(",") + 1)
                            : tooth.XRayBase64;

                        await System.IO.File.WriteAllBytesAsync(fullPath, Convert.FromBase64String(base64Data));

                        var xray = new PatientXRay
                        {
                            PatientId = req.PatientId,
                            VisitId = req.VisitId,
                            FileName = tooth.XRayFileName,
                            FilePath = $"/uploads/xrays/{safeName}",
                            TakenDate = DateTime.Now,
                            Notes = $"سن {tooth.ToothNumber} — {tooth.XRayType} — {tooth.XRayNotes}"
                        };
                        _context.PatientXRays.Add(xray);
                    }
                    catch
                    {
                        // لو فشل رفع الصورة منمنعش حفظ باقي بيانات السن
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
                IsPrimary = req.IsPrimary
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
            return Json(new { success = true, data = vp, remaining = 0});
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment([FromBody] AddPaymentRequest req)
        {
            var payment = new VisitPayment
            {
                VisitId = req.VisitId,
                Amount = req.Amount,
                Method = req.Method,               // ✅ من Models.Patients
                Description = req.Description,
                ReceiptNo = $"REC-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}"
            };
            _context.VisitPayments.Add(payment);

            var visit = await _context.Visits.FindAsync(req.VisitId);
            if (visit != null) visit.PaidAmount += req.Amount;

            await _context.SaveChangesAsync();
            return Json(new { success = true, data = payment, remaining = 0 });
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
    }


}