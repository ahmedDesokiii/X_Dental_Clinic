    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
namespace SchoolPsychologist.Models
{
    
        [Table("TeacherReports")]
        public class TeacherReport
        {
            [Key]
            public int Id { get; set; }

            // 🔗 Student
            public int StudentId { get; set; }

            [ForeignKey(nameof(StudentId))]
            public virtual Student Student { get; set; }

            // اسم المدرس
            [MaxLength(150)]
            public string TeacherName { get; set; }

            // المادة
            [MaxLength(100)]
            public string Subject { get; set; }

            // التقييم العام
            public RatingLevel Rating { get; set; }

            // سلوك الطالب
            public BehaviorLevel Behavior { get; set; }

            // ملاحظات
            [MaxLength(1000)]
            public string? Notes { get; set; }

            public DateTime ReportDate { get; set; } = DateTime.Now;

            public bool IsDeleted { get; set; } = false;
        }

        public enum RatingLevel
        {
            ضعيف = 1,
            مقبول = 2,
            جيد = 3,
            ممتاز = 4
        }

        public enum BehaviorLevel
        {
            سيئ = 1,
            عادي = 2,
            جيد = 3
        }
    
}
