namespace SchoolPsychologist.Models
{
    [Table("StudentAttendances")]
    public class StudentAttendance
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }

        public DateTime Date { get; set; }

        // حضور الفصل
        public bool IsPresentClass { get; set; }

        // حضور المصنع
        public bool IsPresentFactory { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; }
    }
}
