

namespace SchoolPsychologist.Controllers
{
    [Authorize]
    public class PsychologyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PsychologyController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var model = new PsychologyDashboardViewModel();

            // ================= KPI =================
            model.TotalCases = _context.Psy_Cases.Count();
            model.OpenCases = _context.Psy_Cases.Count(x => x.Status == "Open");
            model.ClosedCases = _context.Psy_Cases.Count(x => x.Status == "Closed");

            model.TotalReports = _context.Psy_Reports.Count();

            // ================= Alerts =================
            model.HasCriticalCases = _context.Psy_Cases.Any(x => x.Priority == "Critical" && x.Status == "Open");
            model.HasPendingReports = _context.Psy_Reports.Any(x => x.IsApproved == false);
            model.HasMissingFollowUp = _context.Psy_Cases.Any(x => x.FollowUpDate == null);

            // ================= Latest Cases =================
            model.LatestCases = _context.Psy_Cases
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Select(x => new PsychologyCaseDto
                {
                    Id = x.Id,
                    StudentName = x.Student.FullName,
                    Type = x.Type,
                    Status = x.Status,
                    Priority = x.Priority
                }).ToList();

            // ================= Latest Reports =================
            model.LatestReports = _context.Psy_Reports
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Select(x => new ReportDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    IsApproved = x.IsApproved
                }).ToList();

            // ================= Charts =================
            model.CasesByStatus = _context.Psy_Cases
                .GroupBy(x => x.Status)
                .Select(g => new ChartSummary
                {
                    Label = g.Key,
                    Value = g.Count()
                }).ToList();

            model.CasesByType = _context.Psy_Cases
                .GroupBy(x => x.Type)
                .Select(g => new ChartSummary
                {
                    Label = g.Key,
                    Value = g.Count()
                }).ToList();

            return View(model);
        }
    }
}
