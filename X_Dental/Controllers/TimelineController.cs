using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dental.Controllers
{
    /// <summary>
    /// 🕒 F2 — ملف المريض على خط زمني واحد:
    /// الزيارات بتشخيصاتها، المدفوعات (لصاحب الصلاحية)، الأشعة، الروشتات، المواعيد،
    /// المستندات، جلسات التقويم، وفتح الملف.
    /// الصلاحية: تفاصيل مريض (Patients.Details_Patient) — ModulePermissionFilter.
    /// </summary>
    public class TimelineController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _auth;

        public TimelineController(ApplicationDbContext context, IAuthorizationService auth)
        {
            _context = context;
            _auth = auth;
        }

        public class TimelineEvent
        {
            public string Type { get; set; } = "";
            public DateTime Date { get; set; }
            public string Title { get; set; } = "";
            public string? Subtitle { get; set; }
            public List<string> Items { get; set; } = new();
            public decimal? Amount { get; set; }
            public string? Badge { get; set; }
            public string? Tone { get; set; }
            public string? Url { get; set; }
            public string? Image { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int patientId)
        {
            var patient = await _context.Patients.AsNoTracking()
                .Where(p => p.Id == patientId)
                .Select(p => new { p.Id, p.CreatedDate, p.PatientCode })
                .FirstOrDefaultAsync();
            if (patient == null) return Json(new { success = false, message = "المريض غير موجود." });

            var canMoney = (await _auth.AuthorizeAsync(User, Permissions.Payments.View)).Succeeded;
            var events = new List<TimelineEvent>();

            // ── الزيارات + التشخيصات ──
            var visits = await _context.Visits.AsNoTracking()
                .Where(v => v.PatientId == patientId && v.IsActive)
                .Select(v => new
                {
                    v.Id, v.VisitDate, v.Category, v.Status, v.ChiefComplaint, v.Notes,
                    v.TotalAmount, v.Discount, v.PaidAmount,
                    Diagnoses = v.Diagnoses.Where(d => !d.IsDeleted)
                        .Select(d => new { d.Diagnosis.NameAr, d.ToothNumber, d.Surface }).ToList()
                })
                .ToListAsync();

            foreach (var v in visits)
            {
                var isOrtho = v.Category == VisitCategory.Orthodontics;
                var items = v.Diagnoses
                    .GroupBy(d => d.NameAr)
                    .Select(g =>
                    {
                        var teeth = g.Where(x => x.ToothNumber.HasValue).Select(x => x.ToothNumber!.Value).OrderBy(t => t).ToList();
                        return teeth.Count == 0 ? g.Key : $"{g.Key} — سن {string.Join("، ", teeth)}";
                    })
                    .ToList();
                if (!string.IsNullOrWhiteSpace(v.ChiefComplaint)) items.Insert(0, "الشكوى: " + v.ChiefComplaint);

                var remaining = v.TotalAmount - v.Discount - v.PaidAmount;
                events.Add(new TimelineEvent
                {
                    Type = "visit",
                    Date = v.VisitDate,
                    Title = isOrtho ? "جلسة تقويم" : "زيارة",
                    Subtitle = v.Status switch
                    {
                        VisitStatus.Completed => "مكتملة",
                        VisitStatus.InProgress => "جارية",
                        VisitStatus.Cancelled => "ملغاة",
                        VisitStatus.NoShow => "لم يحضر",
                        _ => null
                    },
                    Items = items,
                    Amount = canMoney && v.TotalAmount > 0 ? v.TotalAmount : null,
                    Badge = canMoney && v.TotalAmount > 0 ? (remaining > 0 ? $"متبقي {remaining:N0}" : "مدفوعة") : null,
                    Tone = isOrtho ? "ortho" : "visit",
                    Url = Url.Action("Session", "Clinical", new { patientId, visitId = v.Id })
                });
            }

            // ── المدفوعات (إيصال = حدث واحد) ──
            if (canMoney)
            {
                var pays = await _context.VisitPayments.AsNoTracking()
                    .Where(p => p.Visit.PatientId == patientId && !p.IsDeleted)
                    .Select(p => new { p.Id, p.PaymentDate, p.Amount, p.Discount, p.Method, p.ReceiptNo, p.Description })
                    .ToListAsync();

                foreach (var g in pays.GroupBy(p => string.IsNullOrWhiteSpace(p.ReceiptNo) ? $"id:{p.Id}" : $"{p.ReceiptNo}|{p.PaymentDate:yyyyMMdd}"))
                {
                    var amount = g.Sum(x => x.Amount);
                    var discount = g.Sum(x => x.Discount);
                    var first = g.OrderBy(x => x.Id).First();
                    var items = new List<string>();
                    if (discount > 0) items.Add($"خصم {discount:N0}");
                    if (g.Count() > 1) items.Add($"اتوزعت على {g.Count()} زيارات");
                    var desc = g.Select(x => x.Description).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d) && !d!.StartsWith("سداد"));
                    if (!string.IsNullOrWhiteSpace(desc)) items.Add(desc!);

                    events.Add(new TimelineEvent
                    {
                        Type = "payment",
                        Date = g.Max(x => x.PaymentDate),
                        Title = amount < 0 ? "استرداد" : amount == 0 ? "خصم" : "دفعة",
                        Subtitle = first.ReceiptNo,
                        Items = items,
                        Amount = amount != 0 ? amount : discount,
                        Tone = amount < 0 ? "refund" : "payment"
                    });
                }
            }

            // ── الأشعة (صور نفس اليوم = حدث واحد) ──
            var xrays = await _context.PatientXRays.AsNoTracking()
                .Where(x => x.PatientId == patientId && !x.IsDeleted)
                .Select(x => new { x.TakenDate, x.FilePath, x.Notes, x.ToothNumber })
                .ToListAsync();
            foreach (var g in xrays.GroupBy(x => x.TakenDate.Date))
            {
                events.Add(new TimelineEvent
                {
                    Type = "xray",
                    Date = g.Max(x => x.TakenDate),
                    Title = g.Count() > 1 ? $"{g.Count()} صور أشعة" : "صورة أشعة",
                    Items = g.Select(x => x.Notes ?? (x.ToothNumber.HasValue ? $"سن {x.ToothNumber}" : "")).Where(s => s != "").Distinct().ToList(),
                    Image = Url.Content("~" + g.First().FilePath),
                    Tone = "xray"
                });
            }

            // ── الروشتات ──
            var rx = await _context.Prescriptions.AsNoTracking()
                .Where(p => p.Visit.PatientId == patientId && !p.IsDeleted)
                .Select(p => new { p.Id, p.PrescriptionDate, p.Diagnosis, Meds = p.Items.Where(i => !i.IsDeleted).Select(i => i.MedicationName).ToList() })
                .ToListAsync();
            foreach (var p in rx)
            {
                events.Add(new TimelineEvent
                {
                    Type = "rx",
                    Date = p.PrescriptionDate,
                    Title = "روشتة",
                    Subtitle = p.Diagnosis,
                    Items = p.Meds,
                    Tone = "rx",
                    Url = Url.Action("PrintPrescription", "Clinical", new { id = p.Id })
                });
            }

            // ── المواعيد ──
            var appts = await _context.Appointments.AsNoTracking()
                .Where(a => a.PatientId == patientId && !a.IsDeleted)
                .Select(a => new { a.AppointmentDate, a.Status, a.Reason, a.Notes, a.IsReminderSent })
                .ToListAsync();
            foreach (var a in appts)
            {
                var future = a.AppointmentDate.Date >= DateTime.Today;
                events.Add(new TimelineEvent
                {
                    Type = "appointment",
                    Date = a.AppointmentDate,
                    Title = future ? "موعد قادم" : "موعد",
                    Subtitle = a.Status switch
                    {
                        AppointmentStatus.Scheduled => "محجوز",
                        AppointmentStatus.Confirmed => "مؤكد",
                        AppointmentStatus.CheckedIn => "حضر",
                        AppointmentStatus.InProgress => "في الكشف",
                        AppointmentStatus.Completed => "تم",
                        AppointmentStatus.Cancelled => "اتلغى",
                        AppointmentStatus.NoShow => "ما جاش",
                        _ => null
                    },
                    Items = new[] { a.Reason, a.Notes }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList(),
                    Badge = a.IsReminderSent ? "اتبعتله تذكير" : null,
                    Tone = future ? "upcoming" : (a.Status is AppointmentStatus.NoShow or AppointmentStatus.Cancelled ? "missed" : "appointment")
                });
            }

            // ── المستندات ──
            var docs = await _context.PatientDocuments.AsNoTracking()
                .Where(d => d.PatientId == patientId && d.IsActive && !d.IsDeleted)
                .Select(d => new { d.UploadedDate, d.FileName, d.Description })
                .ToListAsync();
            foreach (var g in docs.GroupBy(d => d.UploadedDate.Date))
            {
                events.Add(new TimelineEvent
                {
                    Type = "document",
                    Date = g.Max(x => x.UploadedDate),
                    Title = g.Count() > 1 ? $"{g.Count()} مستندات" : "مستند",
                    Items = g.Select(x => x.Description ?? x.FileName).ToList(),
                    Tone = "document"
                });
            }

            // ── تسجيلات التقويم (بتظهر جوه نفس يوم الزيارة) ──
            var orthoEntries = await (from e in _context.OrthoVisitEntries.AsNoTracking()
                                      join c in _context.OrthoCases.AsNoTracking() on e.OrthoCaseId equals c.Id
                                      join pr in _context.OrthoProcedureCatalogs.AsNoTracking() on e.OrthoProcedureId equals (int?)pr.Id into prj
                                      from pr in prj.DefaultIfEmpty()
                                      where c.PatientId == patientId && !e.IsDeleted
                                      select new { e.VisitId, e.Arch, Proc = pr != null ? pr.NameAr : null, e.Comment })
                                     .ToListAsync();
            foreach (var g in orthoEntries.GroupBy(e => e.VisitId))
            {
                var ev = events.FirstOrDefault(x => x.Type == "visit" && x.Url != null && x.Url.EndsWith($"visitId={g.Key}"));
                var lines = g.Select(e => $"{(e.Arch == OrthoArch.Upper ? "الفك العلوي" : e.Arch == OrthoArch.Lower ? "الفك السفلي" : "الفكين")}: {e.Proc ?? "—"}"
                                          + (string.IsNullOrWhiteSpace(e.Comment) ? "" : $" ({e.Comment})"));
                ev?.Items.AddRange(lines);
            }

            // ── فتح الملف ──
            events.Add(new TimelineEvent
            {
                Type = "created",
                Date = patient.CreatedDate,
                Title = "فتح ملف المريض",
                Subtitle = patient.PatientCode,
                Tone = "created"
            });

            var ordered = events.OrderByDescending(e => e.Date).ToList();
            return Json(new
            {
                success = true,
                canMoney,
                counts = ordered.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count()),
                events = ordered.Select(e => new
                {
                    e.Type, date = e.Date.ToString("yyyy-MM-ddTHH:mm:ss"), e.Title, e.Subtitle, e.Items,
                    e.Amount, e.Badge, e.Tone, e.Url, e.Image
                })
            });
        }
    }
}
