using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPsychologist.Models
{
    [Table("PsychologyReports")]
    public class PsychologyReport
    {
        [Key]
        public int Id { get; set; }

        // 🔗 Student
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; }

        // نوع المشكلة
        public IssueType IssueType { get; set; }

        // درجة الخطورة
        public SeverityLevel Severity { get; set; }

        // الحالة الحالية
        public CaseStatus Status { get; set; }

        // وصف الحالة
        [MaxLength(1000)]
        public string Description { get; set; }

        // توصيات
        [MaxLength(1000)]
        public string? Recommendations { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
    }

    public enum IssueType
    {
        سلوكي = 1,
        عاطفي = 2,
        أكاديمي = 3,
        اجتماعي = 4
    }

    public enum SeverityLevel
    {
        ملاحظات = 1,
        متوسطة = 2,
        عظمي = 3,
        حرجة = 4
    }

    public enum CaseStatus
    {
        مفتوحة = 1,
        انتظار = 2,
        مغلقة = 3
    }
}