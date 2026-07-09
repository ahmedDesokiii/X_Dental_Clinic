using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPsychologist.Models
{
    [Table("SocialReports")]
    public class SocialReport
    {
        [Key]
        public int Id { get; set; }

        // 🔗 Student
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; }

        // الحالة الأسرية
        public FamilyStatus FamilyStatus { get; set; }

        // المستوى الاقتصادي
        public EconomicLevel EconomicLevel { get; set; }

        // العلاقة مع الأسرة
        public RelationshipLevel FamilyRelation { get; set; }

        // الملاحظات
        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
    }

    public enum FamilyStatus
    {
            مستقر = 1,
            مطلق = 2,
            يتيم = 3
    }

    public enum EconomicLevel
    {
        ضعيف = 1,
        متوسط = 2,
        مرتفع = 3
    }

    public enum RelationshipLevel
    {
        ضعيف = 1,
        عادي = 2,
        قوي = 3
    }
}