// ============================================================
// 📁 File: Modules/Clinical/Services/VisitNotesCodec.cs  (ملف جديد — المرحلة 2)
// ملاحظات المتابعة بتتخزن في عمود موجود (Visits.Examination — nvarchar(max))
// من غير أي تعديل على الداتابيز.
//
// الشكل المخزّن:  {"dcNotes":1,"legacy":"<أي نص قديم كان في العمود>","items":[...]}
//  • لو العمود فيه نص عادي (من الترحيل القديم) → بيتعامل معاه كـ legacy ويفضل محفوظ كما هو.
//  • PlainText() بترجع نص مقروء لأي كود قديم بيقرا العمود (زي ترحيل التقويم).
// ============================================================
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dental.Modules.Clinical.Services
{
    public static class VisitNotesCodec
    {
        private const string Marker = "\"dcNotes\"";

        public class Envelope
        {
            [JsonPropertyName("dcNotes")] public int Version { get; set; } = 1;
            [JsonPropertyName("legacy")] public string? Legacy { get; set; }
            [JsonPropertyName("items")] public List<Item> Items { get; set; } = new();
        }

        public class Item
        {
            [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
            [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
            [JsonPropertyName("vd")] public int? LinkedVisitDiagnosisId { get; set; }
            [JsonPropertyName("at")] public DateTime CreatedAt { get; set; }
            [JsonPropertyName("mod")] public DateTime? ModifiedAt { get; set; }
        }

        private static readonly JsonSerializerOptions Opts = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // عربي مقروء في الداتابيز
        };

        public static bool IsEnvelope(string? raw) =>
            !string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{") && raw.Contains(Marker);

        public static Envelope Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new Envelope();

            if (IsEnvelope(raw))
            {
                try
                {
                    var env = JsonSerializer.Deserialize<Envelope>(raw, Opts);
                    if (env != null) { env.Items ??= new(); return env; }
                }
                catch (JsonException) { /* لو اتبوظ لأي سبب نحافظ على النص كـ legacy */ }
            }

            return new Envelope { Legacy = raw };
        }

        public static string? Serialize(Envelope env)
        {
            // مفيش ملاحظات → نرجّع العمود لحالته الأصلية (النص القديم أو null)
            if (env.Items.Count == 0)
                return string.IsNullOrWhiteSpace(env.Legacy) ? null : env.Legacy;

            return JsonSerializer.Serialize(env, Opts);
        }

        /// <summary>نص مقروء لأي كود قديم بيستخدم العمود كتعليق/فحص.</summary>
        public static string? PlainText(string? raw)
        {
            if (!IsEnvelope(raw)) return raw;
            var env = Parse(raw);
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(env.Legacy)) parts.Add(env.Legacy.Trim());
            parts.AddRange(env.Items.OrderBy(i => i.CreatedAt).Select(i => i.Text.Trim()));
            return parts.Count == 0 ? null : string.Join(" — ", parts);
        }
    }
}
