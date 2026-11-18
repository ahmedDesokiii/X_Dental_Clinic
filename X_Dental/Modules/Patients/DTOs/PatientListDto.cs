using Dental.Models.Patient;

namespace Dental.Modules.Patients.DTOs
{
    public class PatientListDto
    {
        public int Id { get; set; }
        public string? PatientCode { get; set; }
        public string? FullNameAr { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public int? Age { get; set; }
        public string? GenderName { get; set; }
        public bool IsActive { get; set; } = true;

        // ✅ جديد - خام، مش للعرض المباشر، مستخدمين في الفلتر الأمامي لو احتجناه
        public bool HasGeneralVisits { get; set; }
        public bool HasOrthodonticsVisits { get; set; }

        public PatientType PatientType { get; set; }


        public DateTime? NextSession { get; set; }

        // 👥 H2
        public DateTime? LastVisit { get; set; }
        /// <summary>المتبقي على المريض (null لو المستخدم ملوش صلاحية المدفوعات)</summary>
        public decimal? Balance { get; set; }

        public string TypeBadge => PatientType switch
        {
            PatientType.Orthodontics => "<span class=\"badge bg-info-subtle text-info fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-braces\"></i> تقويم</span>",
            PatientType.Both => "<span class=\"badge bg-warning-subtle text-warning fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-heart-pulse\"></i> كلا الملفين</span>",
            _ => "<span class=\"badge bg-success-subtle text-success fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-app-indicator\"></i> عام</span>"
        };

        public string GenderBadge => GenderName switch
        {
            "ذكر" => "<span class=\"badge bg-primary-subtle text-primary fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-gender-male\"></i> ذكر</span>",
            "انثى" => "<span class=\"badge bg-danger-subtle text-danger fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-gender-female\"></i> انثى</span>",
            _ => "<span class=\"badge bg-danger-subtle text-danger fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-gender-female\"></i> انثى</span>"
        };

        public string StatusBadge => IsActive
            ? "<span class=\"badge bg-success-subtle text-success fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-check-circle-fill\"></i> نشط</span>"
            : "<span class=\"badge bg-danger-subtle text-danger fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-x-circle-fill\"></i> غير نشط</span>";

        // ✅ جديد - بادچ النوع (ممكن يبان الاتنين مع بعض لو المريض عنده الملفين)
        public string CategoryBadge => PatientType switch
        {
            PatientType.Orthodontics =>
                "<span class=\"badge bg-warning-subtle text-warning fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-braces\"></i> تقويم</span>",

            PatientType.Both =>
                "<span class=\"badge bg-info-subtle text-info fw-bold px-2 py-1 rounded-3 me-1\"><i class=\"bi bi-app-indicator\"></i> عام</span>" +
                "<span class=\"badge bg-warning-subtle text-warning fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-braces\"></i> تقويم</span>",

            _ =>
                "<span class=\"badge bg-info-subtle text-info fw-bold px-2 py-1 rounded-3\"><i class=\"bi bi-app-indicator\"></i> عام</span>"
        };
    }
}