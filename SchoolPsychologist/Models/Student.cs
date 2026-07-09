using SchoolPsychologist.Models;

namespace SchoolPsychologist.Models
{
        [Table("Students")]
        public class Student
        {
            [Key]
            public int Id { get; set; }

            [Required, MaxLength(100)]
            //[Index(nameof(Code), IsUnique = true)]
        public string Code { get; set; }
        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(30)]
        public string NationalId { get; set; }

        [Required]
        public Gender Gender { get; set; }

        public Region Region { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(20)]  
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public string? ImagePath { get; set; }

        public int ClassId { get; set; }

        [ForeignKey(nameof(ClassId))]
        public virtual SchoolClass Class { get; set; }

        // 🔗 Parent
        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public virtual Parent Parent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public virtual ICollection<Psy_Case> Cases { get; set; }
        public virtual ICollection<Psy_Report> Reports { get; set; }
        public virtual ICollection<TeacherReport> TeacherReports { get; set; } = new List<TeacherReport>();
        public virtual ICollection<PsychologyReport> PsychologyReports { get; set; } = new List<PsychologyReport>();
        public virtual ICollection<SocialReport> SocialReports { get; set; } = new List<SocialReport>();
    }

    public enum Gender
    {
        [Display(Name = "ذكر")]
        Male = 1,

        [Display(Name = "أنثى")]
        Female = 2
    }
    public enum Region
    {
        [Display(Name = "مسلم")]
        Muslim = 1,

        [Display(Name = "مسيحي")]
        Christian = 2,

        [Display(Name = "يهودي")]
        Jewish = 3
    }

}

