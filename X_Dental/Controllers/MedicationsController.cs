using Dental.Models.Clinic;
using Dental.Modules.Clinic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Controllers
{
    // ============================================================
    // 📁 File: Controllers/MedicationsController.cs
    // ============================================================
    // إدارة كاتالوج الأدوية (MedicationCatalog): عرض، استيراد من إكسيل،
    // تعديل الكاتيجوري، تفعيل/إلغاء تفعيل.
    // ============================================================

    public class MedicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MedicationsController> _logger;

        public MedicationsController(ApplicationDbContext context, ILogger<MedicationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Categories = GetEnumSelectList<MedicationCategory>();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicationsData()
        {
            var meds = await _context.MedicationCatalogs
                .OrderBy(m => m.Category).ThenBy(m => m.NameEn)
                .Select(m => new
                {
                    id = m.Id,
                    nameEn = m.NameEn,
                    nameAr = m.NameAr,
                    category = (int)m.Category,
                    categoryName = m.Category.ToString(),
                    defaultFrequency = m.DefaultFrequency,
                    defaultDosage = m.DefaultDosage,
                    defaultDuration = m.DefaultDuration,
                    isActive = m.IsActive
                })
                .ToListAsync();

            return Json(new { success = true, data = meds });
        }

        // ⭐ يستخدمه مودال الروشتة: قايمة الأدوية الفعّالة بكاتيجوريتها وجرعتها الافتراضية
        [HttpGet]
        public async Task<IActionResult> GetActiveMedications()
        {
            var meds = await _context.MedicationCatalogs
                .Where(m => m.IsActive)
                .OrderBy(m => m.Category).ThenBy(m => m.NameEn)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.NameEn,
                    category = (int)m.Category,
                    categoryName = m.Category.ToString(),
                    defaultFrequency = m.DefaultFrequency,
                    defaultDosage = m.DefaultDosage,
                    defaultDuration = m.DefaultDuration
                })
                .ToListAsync();

            return Json(new { success = true, data = meds });
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "لم يتم اختيار ملف." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xlsm")
                return Json(new { success = false, message = "الملف المطلوب Excel (.xlsx) فقط." });

            List<ParsedMedication> parsed;
            try
            {
                using var stream = file.OpenReadStream();
                parsed = MedicationImportService.ParseCategorySheet(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse medication excel file");
                return Json(new { success = false, message = "تعذر قراءة الملف — تأكدي إن شيت CATEGORY موجود وبنفس الشكل المعتاد." });
            }

            if (!parsed.Any())
                return Json(new { success = false, message = "لم يتم العثور على أي أدوية داخل شيت CATEGORY." });

            int added = 0, updated = 0, guessed = 0;

            foreach (var item in parsed)
            {
                var existing = await _context.MedicationCatalogs
                    .FirstOrDefaultAsync(m => m.NameEn.ToLower() == item.Name.ToLower());

                if (existing != null)
                {
                    existing.Category = item.MappedCategory;
                    existing.DefaultFrequency = item.Instruction;
                    existing.IsActive = true;
                    updated++;
                }
                else
                {
                    _context.MedicationCatalogs.Add(new MedicationCatalog
                    {
                        NameEn = item.Name,
                        NameAr = item.Name,
                        Category = item.MappedCategory,
                        DefaultFrequency = item.Instruction,
                        IsActive = true
                    });
                    added++;
                }

                if (item.CategoryGuessed) guessed++;
            }

            await _context.SaveChangesAsync();

            var message = $"تم إضافة {added} دواء جديد وتحديث {updated}.";
            if (guessed > 0)
                message += $" ⚠️ {guessed} صنف اتحط تحت تصنيف تقريبي (أخرى) لأن اسم الكاتيجوري في الملف مش متعرّف عليه — راجعيهم من الجدول.";

            return Json(new { success = true, message, added, updated, guessed });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(int id, MedicationCategory category)
        {
            var med = await _context.MedicationCatalogs.FindAsync(id);
            if (med == null)
                return Json(new { success = false, message = "الدواء غير موجود." });

            med.Category = category;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var med = await _context.MedicationCatalogs.FindAsync(id);
            if (med == null)
                return Json(new { success = false, message = "الدواء غير موجود." });

            med.IsActive = !med.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = med.IsActive });
        }
    }
}