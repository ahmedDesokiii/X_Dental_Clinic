using System.Text.Json;

namespace Dental.Core.Settings
{
    /// <summary>
    /// ⚙️ D1 — بيانات العيادة اللي بتظهر في البرنامج والرسائل والطباعة.
    /// بتتحفظ في App_Data/clinic-settings.json (مش في الداتابيز) — فمحتاجتش migration،
    /// وكل عميل ليه إعداداته من غير ما نغيّر الكود.
    /// </summary>
    public class ClinicSettings
    {
        public string ClinicName { get; set; } = "Dental Suite";
        public string DoctorName { get; set; } = "Dr/ Mohammed El.Kholy";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? FacebookUrl { get; set; } = "https://www.facebook.com/share/1B3xKJX65H/";

        /// <summary>مسار اللوجو جوه wwwroot (مثلاً /images/clinic-logo.png). فاضي = أيقونة السنة الافتراضية.</summary>
        public string? LogoPath { get; set; }

        /// <summary>كود الدولة لروابط واتساب (مصر = 20).</summary>
        public string WhatsAppCountryCode { get; set; } = "20";

        /// <summary>
        /// نص رسالة التذكير. المتغيرات المتاحة:
        /// {patient} {date} {day} {time} {clinic} {doctor} {phone} {address}
        /// </summary>
        public string ReminderTemplate { get; set; } =
            "مرحبًا {patient} 🌿\n" +
            "نذكّرك بموعدك في عيادة {doctor} يوم {day} {date}{time}.\n" +
            "{address}\n" +
            "للتأكيد أو التغيير برجاء الرد على هذه الرسالة أو الاتصال على {phone}.\n" +
            "نتمنى لك دوام الصحة 🦷";
    }

    public interface IClinicSettingsService
    {
        ClinicSettings Get();
        Task SaveAsync(ClinicSettings settings);
    }

    public class ClinicSettingsService : IClinicSettingsService
    {
        private readonly string _file;
        private readonly ILogger<ClinicSettingsService> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private ClinicSettings? _cached;

        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ClinicSettingsService(IWebHostEnvironment env, ILogger<ClinicSettingsService> logger)
        {
            _file = Path.Combine(env.ContentRootPath, "App_Data", "clinic-settings.json");
            _logger = logger;
        }

        public ClinicSettings Get()
        {
            if (_cached != null) return _cached;
            try
            {
                if (File.Exists(_file))
                    _cached = JsonSerializer.Deserialize<ClinicSettings>(File.ReadAllText(_file), Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read clinic settings from {File}", _file);
            }
            return _cached ??= new ClinicSettings();
        }

        public async Task SaveAsync(ClinicSettings settings)
        {
            await _lock.WaitAsync();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
                var tmp = _file + ".tmp";
                await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(settings, Json));
                File.Move(tmp, _file, overwrite: true);
                _cached = settings;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
