namespace SchoolPsychologist.ViewModels.Students
{
    public class SocialReportVM
    {
        public List<SocialReport>? Reports { get; set; }
        public SocialReport? NewReport { get; set; }
        public int StudentId { get; set; }
        public Student? StudentData { get; set; }

    }
}
