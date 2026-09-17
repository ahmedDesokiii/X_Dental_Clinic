using ClosedXML.Excel;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Clinic.Services
{
    // ============================================================
    // 📁 File: Modules/Clinic/Services/MedicationImportService.cs
    // ============================================================
    // بيقرأ شيت "CATEGORY" من ملف الإكسيل بتاع الأدوية.
    // شكل الشيت: كل عمود = كاتيجوري (أو كاتيجوري + شكل الدوا زي SYRP/VIAL/TABLET)،
    // وتحت كل عمود صفوف متبادلة: اسم الدوا ثم الجرعة، اسم الدوا ثم الجرعة... إلخ.
    // ============================================================

    public class ParsedMedication
    {
        public string Name { get; set; } = string.Empty;
        public string? Instruction { get; set; }
        public string ExcelCategoryLabel { get; set; } = string.Empty;
        public MedicationCategory MappedCategory { get; set; }
        public bool CategoryGuessed { get; set; } // true = الكاتيجوري اتحطت تقريبيًا ومحتاجة مراجعة يدوية
    }

    public static class MedicationImportService
    {
        private static readonly HashSet<string> FormLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "SYRP", "VIAL", "TABLET"
        };

        // ⚠️ الماب ده مبني على تحليل ملف الإكسيل اللي اتبعت فعليًا.
        // أي كاتيجوري في ملف مستقبلي مش موجودة هنا هتترحّل كـ Other
        // مع CategoryGuessed = true عشان تتراجع من شاشة الأدوية.
        private static readonly Dictionary<string, MedicationCategory> CategoryMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "ANALGESIC_SYRP", MedicationCategory.ANALGESIC_SYRP},
            { "ANALGESIC_VIAL", MedicationCategory.ANALGESIC_VIAL},
            { "ANALGESIC_TABLET", MedicationCategory.ANALGESIC_TABLET},
            { "ANTIBIOTIC_SYRP", MedicationCategory.ANTIBIOTIC_SYRP},
            { "ANTIBIOTIC_VIAL", MedicationCategory.ANTIBIOTIC_VIAL},
            { "ANTIBIOTIC_TABLET", MedicationCategory.ANTIBIOTIC_TABLET},
            { "Anti_Edematus", MedicationCategory.Anti_Edematus},
            { "Mouth_Wash", MedicationCategory.MOUTH_WASH},
            { "TOOTH_PASTE", MedicationCategory.TOOTH_PASTE},
            { "OTHER", MedicationCategory.Other },
        };

        public static List<ParsedMedication> ParseCategorySheet(Stream fileStream)
        {
            var result = new List<ParsedMedication>();

            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheets.FirstOrDefault(w =>
                    w.Name.Trim().Equals("CATEGORY", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.First();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow < 2 || lastCol < 1) return result;

            for (int col = 1; col <= lastCol; col++)
            {
                bool hasData = false;
                for (int r = 2; r <= lastRow; r++)
                {
                    if (!string.IsNullOrWhiteSpace(ws.Cell(r, col).GetString()))
                    {
                        hasData = true;
                        break;
                    }
                }
                if (!hasData) continue; // عمود فاصل فاضي

                var header = ws.Cell(1, col).GetString().Trim();
                var row2 = ws.Cell(2, col).GetString().Trim();

                int startRow = 2;
                if (FormLabels.Contains(row2))
                    startRow = 3; // الصف ده عنوان شكل الدوا (SYRP/VIAL/TABLET) مش اسم دوا فعلي

                for (int r = startRow; r <= lastRow; r += 2)
                {
                    var name = ws.Cell(r, col).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(name)) break; // خلصت بيانات العمود ده

                    var instruction = r + 1 <= lastRow ? ws.Cell(r + 1, col).GetString().Trim() : null;
                    var categoryLabel = string.IsNullOrWhiteSpace(header) ? "(بدون تصنيف في الملف)" : header;
                    var isKnown = !string.IsNullOrWhiteSpace(header) && CategoryMap.ContainsKey(header);
                    var mapped = isKnown ? CategoryMap[header] : MedicationCategory.Other;

                    result.Add(new ParsedMedication
                    {
                        Name = name,
                        Instruction = string.IsNullOrWhiteSpace(instruction) ? null : instruction,
                        ExcelCategoryLabel = categoryLabel,
                        MappedCategory = mapped,
                        CategoryGuessed = !isKnown
                    });
                }
            }

            return result;
        }
    }
}