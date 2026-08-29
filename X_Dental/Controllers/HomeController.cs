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

                // Remaining = TotalAmount - PaidAmount
                TotalDue = await _context.Visits
                    .Where(v => v.IsActive)
                    .SumAsync(v =>
                        (decimal?)(v.TotalAmount - v.PaidAmount)
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


                GeneralPatients = await _context.Patients.CountAsync(p => p.IsActive && p.PatientType == PatientType.General),
                OrthoPatients = await _context.Patients.CountAsync(p => p.IsActive && p.PatientType == PatientType.Orthodontics),
                BothPatients = await _context.Patients.CountAsync(p => p.IsActive && p.PatientType == PatientType.Both),

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
                    .ToListAsync()
            };

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
                        Label = date.ToString(
                            "dd MMM",
                            new CultureInfo("ar-EG")
                        ),
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
                    Label = date.ToString(
                        "MMM yyyy",
                        new CultureInfo("ar-EG")
                    ),
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
                        Label = date.ToString(
                            "ddd",
                            new CultureInfo("ar-EG")
                        ),
                        Count = appointmentsGrouped
                            .FirstOrDefault(x => x.Date == date)
                            ?.Count ?? 0
                    }
                );
            }

            return View(model);
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