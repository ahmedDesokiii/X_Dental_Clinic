using Microsoft.AspNetCore.Mvc;
using SchoolPsychologist.ViewModels.Students;

namespace SchoolPsychologist.Controllers
{
    public class TeacherReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List
        public IActionResult Index(int studentId)
        {
            var model = new TeacherReportVM
            {
                StudentId = studentId,
                StudentData = _context.Students.FirstOrDefault(x => x.Id == studentId),
                Reports = _context.TeacherReports
                    .Where(x => x.StudentId == studentId && !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToList(),

                NewReport = new TeacherReport()
            };

            return View(model);
        }

        // Save
        [HttpPost]
        public IActionResult Save(TeacherReportVM model)
        {
            if (model.NewReport.Id == 0)
            {
                _context.TeacherReports.Add(model.NewReport);
            }
            else
            {
                var report = _context.TeacherReports.FirstOrDefault(x => x.Id == model.NewReport.Id);

                if (report != null)
                {
                    report.TeacherName = model.NewReport.TeacherName;
                    report.Subject = model.NewReport.Subject;
                    report.Rating = model.NewReport.Rating;
                    report.Behavior = model.NewReport.Behavior;
                    report.Notes = model.NewReport.Notes;
                }
            }

            _context.SaveChanges();

            return RedirectToAction("Index", new { studentId = model.NewReport.StudentId });
        }
    }
}
