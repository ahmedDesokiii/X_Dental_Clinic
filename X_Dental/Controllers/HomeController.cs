using Dental.Models.Clinic;
using Dental.Models.Inventory;
using Dental.Models.Patient;
using Dental.Modules.Patients.Enums;
using Dental.ViewModels.Dashboards;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace Dental.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var model = new DashboardViewModel
            {
                // =========================
                // KPIs
                // =========================
                TotalPatients = await _context.Patients
                    .CountAsync(p => p.IsActive),
                TotalVisits = await _context.Visits
                    .CountAsync(v => v.IsActive),
                TodayAppointments = await _context.Appointments
                    .CountAsync(a =>
                        a.AppointmentDate >= today &&
                        a.AppointmentDate < today.AddDays(1) &&
                        a.Status != AppointmentStatus.Cancelled),
                UpcomingAppointments = await _context.Appointments
                    .CountAsync(a =>
                        a.AppointmentDate >= today &&
                        a.Status != AppointmentStatus.Cancelled),
                TotalRevenue = await _context.VisitPayments
                    .Where(p => !p.IsDeleted)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                // 🐞 F1: المتبقي الصحيح = الإجمالي - الخصم - المدفوع (الخصم ما كانش بيتطرح)،
                //        وبنجمع المتبقي الموجب بس (الرصيد الدائن لمريض ما يقللش ديون غيره)
                TotalDue = await _context.Visits
                    .Where(v => v.IsActive && v.TotalAmount - v.Discount - v.PaidAmount > 0)
                    .SumAsync(v =>
                        (decimal?)(v.TotalAmount - v.Discount - v.PaidAmount)
                    ) ?? 0,
                NewPatientsThisMonth = await _context.Patients
                    .CountAsync(p =>
                        p.CreatedDate.Year == today.Year &&
                        p.CreatedDate.Month == today.Month),
                GeneralVisits = await _context.Visits
                    .CountAsync(v =>
                        v.IsActive &&
                        v.Category == VisitCategory.General),
                OrthoVisits = await _context.Visits
                    .CountAsync(v =>
                        v.IsActive &&
                        v.Category == VisitCategory.Orthodontics),

                GeneralPatients = await _context.Patients.CountAsync(p =>
                    p.IsActive && p.PatientType == PatientType.General),
                OrthoPatients = await _context.Patients.CountAsync(p =>
                    p.IsActive && p.PatientType == PatientType.Orthodontics),
                BothPatients = await _context.Patients.CountAsync(p =>
                    p.IsActive && p.PatientType == PatientType.Both),

                // =========================
                // Recent Appointments
                // =========================
                RecentAppointments = await _context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Patient)
                    .Where(a =>
                        a.AppointmentDate >= today &&
                        a.Status != AppointmentStatus.Cancelled)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(6)
                    .Select(a => new RecentAppointmentVM
                    {
                        Id = a.Id,
                        PatientName = a.Patient.FullNameAr,
                        Mobile = a.Patient.Mobile,
                        AppointmentDate = a.AppointmentDate,
                        Reason = a.Reason,
                        Status = a.Status
                    })
                    .ToListAsync(),
                // =========================
                // Recent Visits
                // =========================
                RecentVisits = await _context.Visits
                    .AsNoTracking()
                    .Include(v => v.Patient)
                    .Where(v => v.IsActive)
                    .OrderByDescending(v => v.VisitDate)
                    .Take(5)
                    .Select(v => new RecentVisitVM
                    {
                        Id = v.Id,
                        PatientName = v.Patient.FullNameAr,
                        VisitDate = v.VisitDate,
                        ChiefComplaint = v.ChiefComplaint,
                        Category = v.Category.ToString(),
                        TotalAmount = v.TotalAmount,
                        PaidAmount = v.PaidAmount
                    })
                    .ToListAsync(),
                // =========================
                // Top Procedures
                // =========================
                TopProcedures = await _context.VisitProcedures
                    .AsNoTracking()
                    .Where(p =>
                        p.Visit.IsActive &&
                        p.CreatedDate >= today.AddMonths(-1))
                    .GroupBy(p => p.Procedure.NameAr)
                    .Select(g => new TopProcedureVM
                    {
                        Name = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync(),

                // =========================
                // Low Stock Alerts
                // =========================
                LowStockMaterials = await _context.Materials
                    .AsNoTracking()
                    .Where(m => m.Quantity <= m.MinimumStockLevel)
                    .OrderBy(m => m.Quantity)
                    .Take(5)
                    .Select(m => new LowStockMaterialVM
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Code = m.Code,
                        Quantity = m.Quantity,
                        MinimumStockLevel = m.MinimumStockLevel
                    })
                    .ToListAsync(),
                // =========================
                // Near-Expiry Alerts (30-day window — adjust if you use a different threshold)
                // =========================
                NearExpiryMaterials = await _context.Materials
                    .AsNoTracking()
                    .Where(m => m.ExpiryDate != null &&
                        m.ExpiryDate >= today &&
                        m.ExpiryDate <= today.AddDays(30))
                    .OrderBy(m => m.ExpiryDate)
                    .Take(5)
                    .Select(m => new NearExpiryMaterialVM
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Code = m.Code,
                        ExpiryDate = m.ExpiryDate!.Value
                    })
                    .ToListAsync(),
                // =========================
                // Top Overdue Patients
                // =========================
                TopOverduePatients = await _context.Visits
                    .AsNoTracking()
                    .Where(v => v.IsActive && (v.TotalAmount - v.Discount - v.PaidAmount) > 0)   // 🐞 F1: الخصم
                    .GroupBy(v => new { v.PatientId, v.Patient.FullNameAr, v.Patient.Mobile })
                    .Select(g => new OverduePatientVM
                    {
                        PatientId = g.Key.PatientId,
                        PatientName = g.Key.FullNameAr,
                        Mobile = g.Key.Mobile,
                        AmountDue = g.Sum(v => v.TotalAmount - v.Discount - v.PaidAmount)
                    })
                    .OrderByDescending(x => x.AmountDue)
                    .Take(5)
                    .ToListAsync()
            };

            // Fill in DaysLeft in-memory (simple date subtraction, no need for SQL translation)
            foreach (var m in model.NearExpiryMaterials)
                m.DaysLeft = (m.ExpiryDate.Date - today).Days;

            // ==================================================
            // Chart: Visits - Last 7 Days
            // ==================================================
            var start7 = today.AddDays(-6);
            var visitsGrouped = await _context.Visits
                .AsNoTracking()
                .Where(v =>
                    v.IsActive &&
                    v.VisitDate >= start7 &&
                    v.VisitDate < today.AddDays(1))
                .GroupBy(v => v.VisitDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            for (int i = 0; i < 7; i++)
            {
                var date = start7.AddDays(i);
                model.VisitsLast7Days.Add(
                    new Dental.ViewModels.Dashboards.DateCountPoint
                    {
                        Label = date.ToString("dd MMM", CultureInfo.InvariantCulture),
                        Count = visitsGrouped
                            .FirstOrDefault(x => x.Date == date)
                            ?.Count ?? 0
                    }
                );
            }
            // ==================================================
            // Chart: Revenue - Last 6 Months
            // ==================================================
            var start6 = today.AddMonths(-5);
            var startMonth = new DateTime(
                start6.Year,
                start6.Month,
                1
            );
            var revenueMonths = await _context.VisitPayments
                .AsNoTracking()
                .Where(p =>
                    !p.IsDeleted &&
                    p.PaymentDate >= startMonth &&
                    p.PaymentDate < startMonth.AddMonths(6))
                .GroupBy(p => new
                {
                    p.PaymentDate.Year,
                    p.PaymentDate.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();
            for (int i = 0; i < 6; i++)
            {
                var date = startMonth.AddMonths(i);
                model.RevenueLast6Months.Add(new DateAmountPoint
                {
                    Label = date.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Amount = revenueMonths
                        .FirstOrDefault(x =>
                            x.Year == date.Year &&
                            x.Month == date.Month)
                        ?.Total ?? 0
                });
            }
            // ==================================================
            // Chart: Appointments - Next 7 Days
            // ==================================================
            var appointmentsGrouped = await _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.AppointmentDate >= today &&
                    a.AppointmentDate < today.AddDays(7) &&
                    a.Status != AppointmentStatus.Cancelled)
                .GroupBy(a => a.AppointmentDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);
                model.AppointmentNext7Days.Add(
                    new Dental.ViewModels.Dashboards.DateCountPoint
                    {
                        Label = date.ToString("ddd", CultureInfo.InvariantCulture),
                        Count = appointmentsGrouped
                            .FirstOrDefault(x => x.Date == date)
                            ?.Count ?? 0
                    }
                );
            }
            return View(model);
        }

        // ════════════════════════════════════════════════════════════
        // 📊 F1 — نبض العيادة: كل قسم بيرجع بس لو المستخدم عنده صلاحيته
        // ════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetInsights()
        {
            var auth = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
            async Task<bool> Can(string p) => (await auth.AuthorizeAsync(User, p)).Succeeded;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var result = new Dictionary<string, object?>();

            // ── 1) الإيرادات ──
            if (await Can(Permissions.Payments.Reports))
            {
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var lastMonthStart = monthStart.AddMonths(-1);
                var lastMonthSameDay = lastMonthStart.AddDays(Math.Min(today.Day, DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month)));

                var pays = _context.VisitPayments.AsNoTracking().Where(p => !p.IsDeleted && p.Visit.IsActive);

                var todayTotal = await pays.Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow).SumAsync(p => (decimal?)p.Amount) ?? 0;
                var todayReceipts = await pays.Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow && p.Amount > 0)
                    .Select(p => p.ReceiptNo ?? p.Id.ToString()).Distinct().CountAsync();
                var monthTotal = await pays.Where(p => p.PaymentDate >= monthStart && p.PaymentDate < tomorrow).SumAsync(p => (decimal?)p.Amount) ?? 0;
                var lastMonthToDate = await pays.Where(p => p.PaymentDate >= lastMonthStart && p.PaymentDate < lastMonthSameDay).SumAsync(p => (decimal?)p.Amount) ?? 0;
                var monthDiscount = await pays.Where(p => p.PaymentDate >= monthStart && p.PaymentDate < tomorrow).SumAsync(p => (decimal?)p.Discount) ?? 0;

                // آخر 14 يوم (للرسم الصغير)
                var from14 = today.AddDays(-13);
                var daily = await pays.Where(p => p.PaymentDate >= from14 && p.PaymentDate < tomorrow)
                    .GroupBy(p => p.PaymentDate.Date)
                    .Select(g => new { Day = g.Key, Total = g.Sum(x => x.Amount) })
                    .ToListAsync();
                var spark = Enumerable.Range(0, 14).Select(i =>
                {
                    var d = from14.AddDays(i);
                    return new { day = d.ToString("dd/MM"), total = daily.FirstOrDefault(x => x.Day == d)?.Total ?? 0 };
                });

                result["revenue"] = new
                {
                    today = todayTotal,
                    todayReceipts,
                    month = monthTotal,
                    lastMonthToDate,
                    changePct = lastMonthToDate > 0 ? Math.Round((monthTotal - lastMonthToDate) * 100 / lastMonthToDate, 1) : (decimal?)null,
                    monthDiscount,
                    monthName = today.ToString("MMMM", new CultureInfo("ar-EG")),
                    spark
                };
            }

            // ── 2) المتأخرات (أكبر مديونيات) ──
            if (await Can(Permissions.Payments.View))
            {
                var open = _context.Visits.AsNoTracking()
                    .Where(v => v.IsActive && v.TotalAmount - v.Discount - v.PaidAmount > 0);

                var totalDue = await open.SumAsync(v => (decimal?)(v.TotalAmount - v.Discount - v.PaidAmount)) ?? 0;
                var debtors = await open
                    .GroupBy(v => v.PatientId)
                    .Select(g => new
                    {
                        PatientId = g.Key,
                        Due = g.Sum(v => v.TotalAmount - v.Discount - v.PaidAmount),
                        LastVisit = g.Max(v => v.VisitDate)
                    })
                    .OrderByDescending(x => x.Due)
                    .Take(6)
                    .ToListAsync();
                var ids = debtors.Select(d => d.PatientId).ToList();
                var names = await _context.Patients.AsNoTracking().Where(p => ids.Contains(p.Id))
                    .Select(p => new { p.Id, p.FullNameAr, p.Mobile }).ToDictionaryAsync(p => p.Id);
                var debtorsCount = await open.Select(v => v.PatientId).Distinct().CountAsync();

                result["receivables"] = new
                {
                    total = totalDue,
                    patients = debtorsCount,
                    top = debtors.Select(d => new
                    {
                        patientId = d.PatientId,
                        name = names.GetValueOrDefault(d.PatientId)?.FullNameAr,
                        mobile = names.GetValueOrDefault(d.PatientId)?.Mobile,
                        due = d.Due,
                        daysSince = (int)(today - d.LastVisit.Date).TotalDays
                    })
                };
            }

            // ── 3) مرضى تقويم فاتوا ميعاد الضبط (أكتر من 35 يوم من غير زيارة ومن غير موعد جاي) ──
            if (await Can(Permissions.Clinical.View))
            {
                const int OverdueDays = 35;
                var activeOrtho = await _context.OrthoCases.AsNoTracking()
                    .Where(c => !c.IsDeleted && c.Status == OrthoCaseStatus.Active)
                    .Select(c => c.PatientId).Distinct().ToListAsync();

                var lastVisits = await _context.Visits.AsNoTracking()
                    .Where(v => v.IsActive && activeOrtho.Contains(v.PatientId))
                    .GroupBy(v => v.PatientId)
                    .Select(g => new { PatientId = g.Key, Last = g.Max(v => v.VisitDate) })
                    .ToListAsync();

                var withFuture = await _context.Appointments.AsNoTracking()
                    .Where(a => !a.IsDeleted && a.AppointmentDate >= today && activeOrtho.Contains(a.PatientId)
                             && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
                    .Select(a => a.PatientId).Distinct().ToListAsync();

                var overdue = lastVisits
                    .Where(x => (today - x.Last.Date).TotalDays > OverdueDays && !withFuture.Contains(x.PatientId))
                    .OrderByDescending(x => (today - x.Last.Date).TotalDays)
                    .ToList();
                var oIds = overdue.Take(6).Select(x => x.PatientId).ToList();
                var oNames = await _context.Patients.AsNoTracking().Where(p => oIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.FullNameAr, p.Mobile }).ToDictionaryAsync(p => p.Id);

                result["orthoOverdue"] = new
                {
                    count = overdue.Count,
                    activeCases = activeOrtho.Count,
                    thresholdDays = OverdueDays,
                    top = overdue.Take(6).Select(x => new
                    {
                        patientId = x.PatientId,
                        name = oNames.GetValueOrDefault(x.PatientId)?.FullNameAr,
                        mobile = oNames.GetValueOrDefault(x.PatientId)?.Mobile,
                        daysSince = (int)(today - x.Last.Date).TotalDays
                    })
                };
            }

            // ── 4) مواعيد النهارده وتذكيرات بكرة ──
            if (await Can(Permissions.Appointments.View))
            {
                var after = tomorrow.AddDays(1);
                var todayAppts = await _context.Appointments.AsNoTracking()
                    .Where(a => !a.IsDeleted && a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
                    .GroupBy(a => a.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
                var tmr = await _context.Appointments.AsNoTracking()
                    .Where(a => !a.IsDeleted && a.AppointmentDate >= tomorrow && a.AppointmentDate < after
                             && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed))
                    .Select(a => a.IsReminderSent).ToListAsync();

                int C(params AppointmentStatus[] st) => todayAppts.Where(x => st.Contains(x.Key)).Sum(x => x.Count);
                result["appointments"] = new
                {
                    today = new
                    {
                        waiting = C(AppointmentStatus.Scheduled, AppointmentStatus.Confirmed),
                        inClinic = C(AppointmentStatus.CheckedIn, AppointmentStatus.InProgress),
                        done = C(AppointmentStatus.Completed),
                        missed = C(AppointmentStatus.NoShow, AppointmentStatus.Cancelled)
                    },
                    tomorrow = tmr.Count,
                    tomorrowReminded = tmr.Count(x => x)
                };
            }

            return Json(new { success = true, data = result });
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}