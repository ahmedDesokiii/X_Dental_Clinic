using Microsoft.EntityFrameworkCore;

namespace Dental.Core.Maintenance
{
    public class BackupOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>فولدر على نفس جهاز SQL Server — لازم حساب خدمة SQL Server يقدر يكتب فيه.</summary>
        public string Folder { get; set; } = @"C:\DentalBackups";

        /// <summary>ساعة النسخة اليومية (0–23) بتوقيت الجهاز.</summary>
        public int Hour { get; set; } = 23;

        /// <summary>عدد الأيام اللي بنحتفظ فيها بالنسخ القديمة.</summary>
        public int KeepDays { get; set; } = 14;
    }

    /// <summary>
    /// 🛡️ A5 — نسخة احتياطية يومية أوتوماتيك للداتابيز (BACKUP DATABASE).
    /// - بتشتغل كل يوم في الساعة المحددة في appsettings → "Backup".
    /// - لو آخر نسخة أقدم من يوم (الجهاز كان مقفول وقت الميعاد) بتعمل نسخة بعد التشغيل بدقيقتين.
    /// - بتمسح النسخ الأقدم من KeepDays.
    /// ⚠️ النسخة على نفس الجهاز بتحمي من الأخطاء ومسح البيانات، مش من عطل الهارد —
    ///    انسخي الفولدر ده لمكان تاني (Google Drive / فلاشة) بشكل دوري.
    /// </summary>
    public class DatabaseBackupService : BackgroundService
    {
        private const string Prefix = "dental_";
        private readonly IServiceProvider _services;
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly BackupOptions _options;

        public DatabaseBackupService(IServiceProvider services, ILogger<DatabaseBackupService> logger, IConfiguration config)
        {
            _services = services;
            _logger = logger;
            _options = config.GetSection("Backup").Get<BackupOptions>() ?? new BackupOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Database backup is disabled (Backup:Enabled=false)");
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                if (LastBackupAge() > TimeSpan.FromHours(24))
                    await RunBackupAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    var next = now.Date.AddHours(Math.Clamp(_options.Hour, 0, 23));
                    if (next <= now) next = next.AddDays(1);

                    await Task.Delay(next - now, stoppingToken);
                    await RunBackupAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task<string?> RunBackupAsync(CancellationToken ct = default)
        {
            try
            {
                Directory.CreateDirectory(_options.Folder);
                var file = Path.Combine(_options.Folder, $"{Prefix}{DateTime.Now:yyyy-MM-dd_HHmm}.bak");

                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

                await db.Database.ExecuteSqlRawAsync(@"
DECLARE @db sysname = DB_NAME();
DECLARE @sql nvarchar(max) = N'BACKUP DATABASE ' + QUOTENAME(@db) + N' TO DISK = @p WITH INIT, CHECKSUM';
EXEC sp_executesql @sql, N'@p nvarchar(4000)', @p = {0};", new object[] { file }, ct);

                _logger.LogInformation("Database backup created: {File}", file);
                Cleanup();
                return file;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup FAILED (folder: {Folder})", _options.Folder);
                return null;
            }
        }

        private TimeSpan LastBackupAge()
        {
            try
            {
                if (!Directory.Exists(_options.Folder)) return TimeSpan.MaxValue;
                var last = new DirectoryInfo(_options.Folder)
                    .GetFiles($"{Prefix}*.bak")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                return last == null ? TimeSpan.MaxValue : DateTime.Now - last.LastWriteTime;
            }
            catch { return TimeSpan.MaxValue; }
        }

        private void Cleanup()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-Math.Max(1, _options.KeepDays));
                foreach (var f in new DirectoryInfo(_options.Folder).GetFiles($"{Prefix}*.bak"))
                    if (f.LastWriteTime < cutoff) f.Delete();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not clean old backups in {Folder}", _options.Folder);
            }
        }
    }
}
