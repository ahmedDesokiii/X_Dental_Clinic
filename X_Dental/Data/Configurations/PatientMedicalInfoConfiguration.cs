using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class PatientMedicalInfoConfiguration
        : IEntityTypeConfiguration<PatientMedicalInfo>
    {
        public void Configure(EntityTypeBuilder<PatientMedicalInfo> builder)
        {
            builder.ToTable("ERP_PatientMedicalInfo");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.Allergies)
                .HasMaxLength(1000);



            builder.Property(x => x.ChronicDiseases)
                .HasMaxLength(1000);



            builder.Property(x => x.PreviousSurgeries)
                .HasMaxLength(1000);



            builder.Property(x => x.CurrentMedications)
                .HasMaxLength(1000);



            builder.Property(x => x.MedicalNotes)
                .HasMaxLength(2000);



            builder.Property(x => x.SmokingStatus)
                .HasMaxLength(50);



            builder.Property(x => x.EmergencyNotes)
                .HasMaxLength(1000);



            builder.Property(x => x.IsPregnant)
                .HasDefaultValue(false);



            // One To One Relationship

            builder.HasOne(x => x.Patient)
                .WithOne(x => x.MedicalInfo)
                .HasForeignKey<PatientMedicalInfo>(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}