using System.Text.RegularExpressions;

namespace Dental.Core.Security
{
    /// <summary>
    /// 🔒 A2 — أسماء ملفات آمنة للأشعة.
    /// قبل كده اسم الملف كان بييجي من المتصفح زي ما هو، فممكن حد يرفع ملف .html أو .js
    /// ويتفتح من جوه الموقع. دلوقتي بنقبل امتدادات الصور/PDF بس، وبننضف باقي الاسم.
    /// </summary>
    public static class UploadSafety
    {
        public static readonly string[] XRayExtensions =
            { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tif", ".tiff", ".pdf" };

        /// <returns>اسم آمن للحفظ، أو null لو الامتداد مش مسموح.</returns>
        public static string? XRayFileName(int patientId, object tooth, string? originalName)
        {
            var name = Path.GetFileName(originalName ?? string.Empty);
            var ext = Path.GetExtension(name).ToLowerInvariant();
            if (!XRayExtensions.Contains(ext)) return null;

            var stem = Path.GetFileNameWithoutExtension(name);
            stem = Regex.Replace(stem, @"[^\p{L}\p{N}_\-]+", "_").Trim('_');
            if (stem.Length > 60) stem = stem[..60];
            if (stem.Length == 0) stem = "xray";

            return $"{patientId}_{tooth}_{DateTime.Now:yyyyMMddHHmmssfff}_{stem}{ext}";
        }
    }
}
