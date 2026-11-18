using Dental.Core.Settings;

namespace Dental.Controllers
{
    /// <summary>⚙️ D1 — إعدادات العيادة (مدير النظام فقط)</summary>
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private static readonly string[] LogoExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

        private readonly IClinicSettingsService _settings;
        private readonly IWebHostEnvironment _env;

        public SettingsController(IClinicSettingsService settings, IWebHostEnvironment env)
        {
            _settings = settings;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index() => View(_settings.Get());

        [HttpPost]
        public async Task<IActionResult> Save(ClinicSettings model, IFormFile? logo, bool removeLogo = false)
        {
            if (string.IsNullOrWhiteSpace(model.ClinicName) || string.IsNullOrWhiteSpace(model.DoctorName))
                return Json(new { success = false, message = "اسم العيادة واسم الطبيب مطلوبين." });

            var current = _settings.Get();
            model.LogoPath = current.LogoPath;
            model.WhatsAppCountryCode = new string((model.WhatsAppCountryCode ?? "20").Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(model.WhatsAppCountryCode)) model.WhatsAppCountryCode = "20";
            if (string.IsNullOrWhiteSpace(model.ReminderTemplate)) model.ReminderTemplate = new ClinicSettings().ReminderTemplate;

            if (removeLogo) model.LogoPath = null;

            if (logo != null && logo.Length > 0)
            {
                var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
                if (!LogoExtensions.Contains(ext))
                    return Json(new { success = false, message = "اللوجو لازم يكون صورة (PNG / JPG / WEBP)." });
                if (logo.Length > 2 * 1024 * 1024)
                    return Json(new { success = false, message = "حجم اللوجو أكبر من 2 ميجا." });

                var dir = Path.Combine(_env.WebRootPath, "images");
                Directory.CreateDirectory(dir);
                var name = $"clinic-logo-{DateTime.Now:yyyyMMddHHmmss}{ext}";
                await using (var fs = new FileStream(Path.Combine(dir, name), FileMode.Create))
                    await logo.CopyToAsync(fs);
                model.LogoPath = $"/images/{name}";
            }

            await _settings.SaveAsync(model);
            return Json(new { success = true, message = "تم حفظ إعدادات العيادة." });
        }
    }
}
