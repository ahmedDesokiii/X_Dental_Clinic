using Dental.Models.Common;

namespace Dental.Models.Patient
{
    public class PatientDocument : BaseEntity
    {
        public int PatientId { get; set; }


        public int DocumentTypeId { get; set; }


        public string FileName { get; set; } = null!;


        public string FilePath { get; set; } = null!;


        public string? FileExtension { get; set; }


        public long? FileSize { get; set; }


        public string? Description { get; set; }


        public DateTime UploadedDate { get; set; }
            = DateTime.Now;


        public bool IsActive { get; set; } = true;



        // Navigation

        public virtual Patient Patient { get; set; } = null!;
        public virtual DocumentType DocumentType { get; set; } = null!;
    }
}