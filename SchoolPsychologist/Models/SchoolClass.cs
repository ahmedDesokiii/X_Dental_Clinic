using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPsychologist.Models
{
    [Table("Classes")]
    public class SchoolClass
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        public int GradeLevel { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
