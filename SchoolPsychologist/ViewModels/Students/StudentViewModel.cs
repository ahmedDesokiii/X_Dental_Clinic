
namespace SchoolPsychologist.ViewModels.Students
{
    public class StudentViewModel
    {
        public List<Student>? Students { get; set; }
        public Student? NewStudent { get; set; }

        public List<SchoolClass>? Classes { get; set; }
        public List<Parent>? Parents { get; set; }

    }
}
