namespace SchoolPsychologist.ViewModels.Students
{
    public class TeacherReportVM
    {
        public List<TeacherReport>? Reports { get; set; }
        public TeacherReport? NewReport { get; set; }
        public int StudentId { get; set; }
        public Student? StudentData { get; set; }

    }
}
