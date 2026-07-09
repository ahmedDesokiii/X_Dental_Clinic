using SchoolPsychologist.ViewModels.Students;

public class SocialReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SocialReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(int studentId)
    {
        var model = new SocialReportVM
        {
            StudentId = studentId,
            StudentData = _context.Students.FirstOrDefault(x => x.Id == studentId),
            Reports = _context.SocialReports
                .Where(x => x.StudentId == studentId && !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .ToList(),

            NewReport = new SocialReport()
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Save(SocialReportVM model)
    {
        if (model.NewReport.Id == 0)
        {
            _context.SocialReports.Add(model.NewReport);

            // 🔥 Logic بسيط
            if (model.NewReport.EconomicLevel == EconomicLevel.ضعيف)
            {
                TempData["Alert"] = "⚠ الحالة الاقتصادية ضعيفة";
            }
        }
        else
        {
            var report = _context.SocialReports.FirstOrDefault(x => x.Id == model.NewReport.Id);

            if (report != null)
            {
                report.FamilyStatus = model.NewReport.FamilyStatus;
                report.EconomicLevel = model.NewReport.EconomicLevel;
                report.FamilyRelation = model.NewReport.FamilyRelation;
                report.Notes = model.NewReport.Notes;
            }
        }

        _context.SaveChanges();

        return RedirectToAction("Index", new { studentId = model.NewReport.StudentId });
    }
}