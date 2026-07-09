using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPsychologist.Models
{
    [Table("Parents")]
    public class Parent
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? AlternatePhone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        // 🔥 صلة القرابة
        [MaxLength(50)]
        public string? Relation { get; set; } // Father - Mother - Guardian

        public bool IsDeleted { get; set; } = false;

        // 🔗 Students
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}