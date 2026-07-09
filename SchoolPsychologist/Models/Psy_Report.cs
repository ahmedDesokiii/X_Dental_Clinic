namespace SchoolPsychologist.Models
{
   
        [Table("Psy_Reports")]
        public class Psy_Report
        {
            [Key]
            public int Id { get; set; }

            // مرتبط بحالة
            public int? CaseId { get; set; }
            public virtual Psy_Case Case { get; set; }

            // عنوان التقرير
            [MaxLength(200)]
            public string Title { get; set; }

            // محتوى التقرير
            public string Description { get; set; }

            // هل تم الاعتماد
            public bool IsApproved { get; set; } = false;

            public DateTime CreatedAt { get; set; } = DateTime.Now;

            public bool IsDeleted { get; set; } = false;
       

    }
}

