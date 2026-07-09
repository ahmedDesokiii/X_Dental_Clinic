namespace SchoolPsychologist.ViewModels.Students
{
    public class PsychologyReportVM
    {
        public List<PsychologyReport>? Reports { get; set; }
        public PsychologyReport? NewReport { get; set; }
        public int StudentId { get; set; }
        public Student? StudentData { get; set; }
        
    }
}
