using Microsoft.AspNetCore.Mvc;
using SchoolPsychologist.ViewModels.Students;

namespace SchoolPsychologist.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? classId)
        {
            var model = new AttendanceVM
            {
                Classes = _context.Classes.ToList(),
                ClassId = classId ?? 0,
                Students = new List<StudentAttendanceItem>()
            };

            if (classId.HasValue)
            {
                var today = DateTime.Today;

                model.Students = _context.Students
                    .Where(s => s.ClassId == classId && !s.IsDeleted)
                    .Select(s => new StudentAttendanceItem
                    {
                        StudentId = s.Id,
                        StudentName = s.FullName,
                        Phone = s.Phone,
                        Code = s.Code,
                        NationalId = s.NationalId,
                        ImagePath = s.ImagePath,
                        BirthDate = s.BirthDate,
                        Address = s.Address,
                        IsPresentClass = _context.StudentAttendances
                            .Any(a => a.StudentId == s.Id && a.Date == today && a.IsPresentClass),

                        IsPresentFactory = _context.StudentAttendances
                            .Any(a => a.StudentId == s.Id && a.Date == today && a.IsPresentFactory)
                    }).ToList();
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Save(AttendanceVM model)
        {
            var today = DateTime.Today;

            foreach (var item in model.Students)
            {
                var attendance = _context.StudentAttendances
                    .FirstOrDefault(a => a.StudentId == item.StudentId && a.Date == today);

                if (attendance == null)
                {
                    attendance = new StudentAttendance
                    {
                        StudentId = item.StudentId,
                        Date = today
                    };

                    _context.StudentAttendances.Add(attendance);
                }

                attendance.IsPresentClass = item.IsPresentClass;
                attendance.IsPresentFactory = item.IsPresentFactory;
            }

            _context.SaveChanges();

            return RedirectToAction("Index", new { classId = model.ClassId });
        }
    }
}
