namespace SchoolPsychologist.DTOs
{
    public class PsychologyCaseDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
    }

    public class ReportDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsApproved { get; set; }
    }
}
