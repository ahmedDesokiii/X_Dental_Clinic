using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPsychologist.Models;
using SchoolPsychologist.ViewModels.Students;

namespace SchoolPsychologist.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================= LIST =================
        [Authorize(Permissions.Students.View_Students)]
        public IActionResult ListStudents()
        {
            var model = new StudentViewModel
            {
                Students = _context.Students
                    .Where(x => !x.IsDeleted)
                    .Include(x => x.Class)
                    .Include(x => x.Parent)
                    .ToList(),

                NewStudent = new Student(),

                Parents = _context.Parents.ToList(),
                Classes = _context.Classes.ToList()
            };

            return View(model);
        }

        // ================= SELECTED =================
        [Authorize(Permissions.Students.Details_Student)]
        public IActionResult Selected(int id)
        {
            var model = new StudentViewModel
            {
                NewStudent = _context.Students
                    .Include(x => x.Class)
                    .Include(x => x.Parent)
                    .FirstOrDefault(x => x.Id == id),

                Classes = _context.Classes.ToList(),
                Parents = _context.Parents.ToList()
            };

            return View("ListStudents", model);
        }

        // ================= SAVE =================

        [HttpPost]
        public IActionResult Save(StudentViewModel model, IFormFile? file)
        {
            string? imagePath = null;

            // ================= Upload Image =================
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/students");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                imagePath = "/uploads/students/" + fileName;
            }

            // ================= CREATE =================
            if (model.NewStudent.Id == 0)
            {
                if (_context.Students.Any(x => x.NationalId == model.NewStudent.NationalId))
                {
                    TempData["Error"] = "الرقم القومي مستخدم من قبل";
                    return RedirectToAction("ListStudents");
                }

                if (imagePath != null)
                    model.NewStudent.ImagePath = imagePath;

                _context.Students.Add(model.NewStudent);

                TempData["Success"] = "تم إضافة الطالب بنجاح";
            }
            else
            {
                // ================= UPDATE =================
                var student = _context.Students.FirstOrDefault(x => x.Id == model.NewStudent.Id);

                if (student == null)
                {
                    TempData["Error"] = "الطالب غير موجود";
                    return RedirectToAction("ListStudents");
                }

                student.FullName = model.NewStudent.FullName;
                student.NationalId = model.NewStudent.NationalId;
                student.Gender = model.NewStudent.Gender;
                student.ClassId = model.NewStudent.ClassId;
                student.Phone = model.NewStudent.Phone;
                student.Address = model.NewStudent.Address;
                student.Region = model.NewStudent.Region;
                student.BirthDate = model.NewStudent.BirthDate;

                // 🔥 تحديث الصورة + حذف القديمة
                if (imagePath != null)
                {
                    if (!string.IsNullOrEmpty(student.ImagePath))
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, student.ImagePath.TrimStart('/'));

                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    student.ImagePath = imagePath;
                }

                TempData["Success"] = "تم التعديل بنجاح";
            }

            _context.SaveChanges();
            return RedirectToAction("ListStudents");
        }

        // ================= DELETE (Soft) =================
        [Authorize(Permissions.Students.Delete_Student)]
        public IActionResult Delete(int id)
        {
            var student = _context.Students.FirstOrDefault(x => x.Id == id);

            if (student != null)
            {
                student.IsDeleted = true;
                _context.SaveChanges();
            }

            return RedirectToAction("ListStudents");
        }


    }
}

   