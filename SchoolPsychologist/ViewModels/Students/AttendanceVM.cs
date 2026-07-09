namespace SchoolPsychologist.ViewModels.Students
{
    public class AttendanceVM
    {
        public int ClassId { get; set; }

        public List<SchoolClass> Classes { get; set; }

        public List<StudentAttendanceItem> Students { get; set; }
    }

    public class StudentAttendanceItem
    {
        public int StudentId { get; set; }
        public string Code { get; set; }

        public string StudentName { get; set; }
        public string NationalId { get; set; }
        public Gender Gender { get; set; }

        public Region Region { get; set; }

        public DateTime? BirthDate { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? ImagePath { get; set; }
        public bool IsPresentClass { get; set; }
        public bool IsPresentFactory { get; set; }
    }
}
