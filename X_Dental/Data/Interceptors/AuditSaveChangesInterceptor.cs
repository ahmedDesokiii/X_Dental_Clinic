using Dental.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Dental.Data.Interceptors
{
    /// <summary>
    /// 🔒 A4 — قبل أي SaveChanges:
    /// 1) يملى CreatedBy / ModifiedBy / ModifiedDate في أي BaseEntity أوتوماتيك.
    /// 2) يسجل في AuditLogs أي تعديل أو مسح (حقيقي أو IsDeleted) بالقيم القديمة والجديدة.
    /// السجل بيتحفظ في نفس الـ SaveChanges ونفس الـ transaction —
    /// لو العملية اتلغت (rollback) السجل بيتلغي معاها.
    /// الإضافة الجديدة متسجلة بالفعل في CreatedBy/CreatedDate على السطر نفسه.
    /// </summary>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _http;

        // حقول مش محتاجين نسجل تغييرها
        private static readonly HashSet<string> Ignored = new(StringComparer.Ordinal)
        {
            nameof(BaseEntity.ModifiedDate), nameof(BaseEntity.ModifiedBy),
            nameof(BaseEntity.CreatedDate), nameof(BaseEntity.CreatedBy)
        };

        private static readonly JsonSerializerOptions Json = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public AuditSaveChangesInterceptor(IHttpContextAccessor http) => _http = http;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Apply(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Apply(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void Apply(DbContext? context)
        {
            if (context == null) return;

            try
            {
                context.ChangeTracker.DetectChanges();

                var principal = _http.HttpContext?.User;
                int? userId = int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
                var userName = principal?.Identity?.Name;
                var path = _http.HttpContext?.Request?.Path.Value;
                var now = DateTime.UtcNow;

                var logs = new List<AuditLog>();

                foreach (var entry in context.ChangeTracker.Entries().ToList())
                {
                    if (entry.Entity is AuditLog) continue;

                    // 1) Created/Modified by
                    if (entry.Entity is BaseEntity be)
                    {
                        if (entry.State == EntityState.Added && be.CreatedBy == null)
                            be.CreatedBy = userId;

                        if (entry.State == EntityState.Modified)
                        {
                            be.ModifiedBy = userId ?? be.ModifiedBy;
                            if (!entry.Property(nameof(BaseEntity.ModifiedDate)).IsModified)
                                be.ModifiedDate = now;
                        }
                    }

                    // 2) Audit
                    if (!ShouldAudit(entry)) continue;

                    if (entry.State == EntityState.Modified)
                    {
                        var changes = new Dictionary<string, object?>();
                        foreach (var p in entry.Properties)
                        {
                            if (!p.IsModified || p.Metadata.IsPrimaryKey() || Ignored.Contains(p.Metadata.Name)) continue;
                            if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                            changes[p.Metadata.Name] = new { old = p.OriginalValue, @new = p.CurrentValue };
                        }
                        if (changes.Count == 0) continue;

                        var action = "Modified";
                        if (changes.ContainsKey(nameof(BaseEntity.IsDeleted)) && entry.Entity is BaseEntity d)
                            action = d.IsDeleted ? "SoftDeleted" : "Restored";

                        logs.Add(New(entry, action, changes));
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        var snapshot = entry.Properties
                            .Where(p => !Ignored.Contains(p.Metadata.Name))
                            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                        logs.Add(New(entry, "Deleted", snapshot));
                    }
                }

                if (logs.Count > 0)
                    context.Set<AuditLog>().AddRange(logs);

                AuditLog New(EntityEntry e, string action, object changes) => new()
                {
                    Timestamp = now,
                    UserId = userId,
                    UserName = userName,
                    EntityName = e.Metadata.ClrType.Name,
                    EntityId = KeyOf(e),
                    Action = action,
                    Changes = Serialize(changes),
                    Path = path
                };
            }
            catch
            {
                // السجل عمره ما يوقف حفظ بيانات العيادة
            }
        }

        private static bool ShouldAudit(EntityEntry entry)
        {
            var ns = entry.Metadata.ClrType.Namespace ?? string.Empty;
            // بيانات العيادة فقط — مش جداول الـ Identity (اللوجين بيغيّر SecurityStamp كل شوية)
            return ns.StartsWith("Dental.Models", StringComparison.Ordinal)
                   && !ns.StartsWith("Dental.Models.User", StringComparison.Ordinal)
                   && !entry.Metadata.IsOwned();
        }

        private static string KeyOf(EntityEntry e)
        {
            var pk = e.Metadata.FindPrimaryKey();
            if (pk == null) return string.Empty;
            return string.Join(",", pk.Properties.Select(p => e.Property(p.Name).OriginalValue?.ToString() ?? e.Property(p.Name).CurrentValue?.ToString()));
        }

        private static string Serialize(object value)
        {
            try
            {
                var s = JsonSerializer.Serialize(value, Json);
                return s.Length > 8000 ? s[..8000] : s;
            }
            catch { return "{}"; }
        }
    }
}
