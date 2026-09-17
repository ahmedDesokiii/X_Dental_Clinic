// ============================================================
// 📁 File: Controllers/MedicationsController.cs
// ============================================================
using Dental.Models.Clinic;
using Dental.Modules.Clinic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Controllers
{
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
                .AsNoTracking()
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

        [HttpGet]
        public async Task<IActionResult> GetActiveMedications()
        {
            var meds = await _context.MedicationCatalogs
                .AsNoTracking()
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
                return Json(new { success = false, message = "No file selected." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xlsm")
                return Json(new { success = false, message = "Please upload an Excel file (.xlsx)." });

            List<ParsedMedication> parsed;
            try
            {
                using var stream = file.OpenReadStream();
                parsed = MedicationImportService.ParseCategorySheet(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse medication excel file");
                return Json(new { success = false, message = "Could not read the file. Make sure the CATEGORY sheet exists and matches the usual layout." });
            }

            if (!parsed.Any())
                return Json(new { success = false, message = "No medications were found in the CATEGORY sheet." });

            // One round-trip instead of a DB hit per row
            var existing = await _context.MedicationCatalogs.ToListAsync();
            var byName = existing
                .GroupBy(m => m.NameEn.Trim().ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            int added = 0, updated = 0, guessed = 0;

            foreach (var item in parsed)
            {
                var key = item.Name.Trim().ToLower();
                if (byName.TryGetValue(key, out var current))
                {
                    current.Category = item.MappedCategory;
                    current.DefaultFrequency = item.Instruction;
                    current.IsActive = true;
                    updated++;
                }
                else
                {
                    var created = new MedicationCatalog
                    {
                        NameEn = item.Name,
                        NameAr = item.Name,
                        Category = item.MappedCategory,
                        DefaultFrequency = item.Instruction,
                        IsActive = true
                    };
                    _context.MedicationCatalogs.Add(created);
                    byName[key] = created;
                    added++;
                }

                if (item.CategoryGuessed) guessed++;
            }

            await _context.SaveChangesAsync();

            var message = $"Added {added} and updated {updated} medication(s).";
            if (guessed > 0)
                message += $" {guessed} row(s) were placed under Other because the sheet category was not recognized — review them in the table.";

            return Json(new { success = true, message, added, updated, guessed });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(int id, MedicationCategory category)
        {
            var med = await _context.MedicationCatalogs.FindAsync(id);
            if (med == null)
                return Json(new { success = false, message = "Medication not found." });

            med.Category = category;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var med = await _context.MedicationCatalogs.FindAsync(id);
            if (med == null)
                return Json(new { success = false, message = "Medication not found." });

            med.IsActive = !med.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = med.IsActive });
        }
    }
}