using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class PatientDocumentConfiguration
        : IEntityTypeConfiguration<PatientDocument>
    {
        public void Configure(EntityTypeBuilder<PatientDocument> builder)
        {
            builder.ToTable("ERP_PatientDocuments");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.FileName)
                .HasMaxLength(250)
                .IsRequired();



            builder.Property(x => x.FilePath)
                .HasMaxLength(500)
                .IsRequired();



            builder.Property(x => x.FileExtension)
                .HasMaxLength(20);



            builder.Property(x => x.Description)
                .HasMaxLength(500);



            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);



            // Patient Relationship

            builder.HasOne(x => x.Patient)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}