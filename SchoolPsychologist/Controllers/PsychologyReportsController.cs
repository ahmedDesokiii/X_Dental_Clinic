using Microsoft.AspNetCore.Mvc;
using SchoolPsychologist.ViewModels.Students;

namespace SchoolPsychologist.Controllers
{
    public class PsychologyReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PsychologyReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int studentId)
        {
            var model = new PsychologyReportVM
            {
                StudentId = studentId,
                StudentData = _context.Students.FirstOrDefault(x => x.Id == studentId),
                Reports = _context.PsychologyReports
                    .Where(x => x.StudentId == studentId && !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToList(),

                NewReport = new PsychologyReport()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Save(PsychologyReportVM model)
        {
            if (model.NewReport.Id == 0)
            {
                _context.PsychologyReports.Add(model.NewReport);

                // 🔥 Logic ذكي
                if (model.NewReport.Severity == SeverityLevel.حرجة)
                {
                    TempData["Alert"] = "⚠ حالة حرجة تحتاج تدخل فوري";
                }
            }
            else
            {
                var report = _context.PsychologyReports.FirstOrDefault(x => x.Id == model.NewReport.Id);

                if (report != null)
                {
                    report.IssueType = model.NewReport.IssueType;
                    report.Severity = model.NewReport.Severity;
                    report.Status = model.NewReport.Status;
                    report.Description = model.NewReport.Description;
                    report.Recommendations = model.NewReport.Recommendations;
                }
            }

            _context.SaveChanges();

            return RedirectToAction("Index", new { studentId = model.NewReport.StudentId });
        }
    }
}
