using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class PatientContactConfiguration
        : IEntityTypeConfiguration<PatientContact>
    {
        public void Configure(EntityTypeBuilder<PatientContact> builder)
        {
            builder.ToTable("ERP_PatientContacts");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.Name)
                .HasMaxLength(150);



            builder.Property(x => x.Phone)
                .HasMaxLength(20)
                .IsRequired();



            builder.Property(x => x.Email)
                .HasMaxLength(150);



            builder.Property(x => x.Address)
                .HasMaxLength(300);



            builder.Property(x => x.IsPrimary)
                .HasDefaultValue(false);



            builder.Property(x => x.IsEmergency)
                .HasDefaultValue(false);



            // Patient Relationship

            builder.HasOne(x => x.Patient)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}