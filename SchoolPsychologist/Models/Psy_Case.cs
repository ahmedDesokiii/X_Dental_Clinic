

namespace SchoolPsychologist.Models
{

        [Table("Psy_Cases")]
        public class Psy_Case
        {
            [Key]
            public int Id { get; set; }

            // الطالب
            public int StudentId { get; set; }
            public virtual Student Student { get; set; }

            // نوع الحالة (تنمر - قلق - سلوك عدواني...)
            [MaxLength(100)]
            public string Type { get; set; }

            // الحالة الحالية
            [MaxLength(50)]
            public string Status { get; set; } // Open - InProgress - Closed

            // درجة الخطورة
            [MaxLength(50)]
            public string Priority { get; set; } // Low - Medium - High - Critical

            public DateTime CreatedAt { get; set; } = DateTime.Now;

            public DateTime? FollowUpDate { get; set; }

            public bool IsDeleted { get; set; } = false;
        

    }
}

