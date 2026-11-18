namespace Dental.Models.Common
{
    /// <summary>
    /// 🔒 A4 — سجل التعديلات: مين عدّل/مسح إيه وإمتى، وإيه القيم قبل وبعد.
    /// بيتملى أوتوماتيك من AuditSaveChangesInterceptor — مفيش كود محتاج يكتب فيه.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        /// <summary>وقت العملية (UTC)</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }
        public string? UserName { get; set; }

        /// <summary>اسم الجدول/الكلاس — مثلاً VisitPayment</summary>
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Modified | Deleted | SoftDeleted | Restored</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>JSON: { "Amount": { "old": 500, "new": 300 } } — أو القيم كاملة في حالة المسح</summary>
        public string? Changes { get; set; }

        public string? Path { get; set; }
    }
}
