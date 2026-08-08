using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class PatientConfiguration
        : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("ERP_Patients");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.PatientCode)
                .HasMaxLength(20)
                .IsRequired();


            builder.HasIndex(x => x.PatientCode)
                .IsUnique();



            builder.Property(x => x.FullNameAr)
                .HasMaxLength(100)
                .IsRequired();


            builder.Property(x => x.FullNameEn)
                .HasMaxLength(100);



            builder.Property(x => x.NationalId)
                .HasMaxLength(20);


            builder.HasIndex(p => p.PatientCode).IsUnique();
            builder.HasIndex(p => p.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");


            builder.Property(x => x.Mobile)
                .HasMaxLength(20)
                .IsRequired();


            builder.Property(x => x.Phone)
                .HasMaxLength(20);


            builder.Property(x => x.Email)
                .HasMaxLength(150);


            builder.Property(x => x.Address)
                .HasMaxLength(300);



            //builder.Property(x => x.Occupation)
            //    .HasMaxLength(150);



            builder.Property(x => x.Notes)
                .HasMaxLength(500);



            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);



            // Gender Relationship

            builder.HasOne(x => x.Gender)
                .WithMany()
                .HasForeignKey(x => x.GenderId)
                .OnDelete(DeleteBehavior.Restrict);



            // Blood Group Relationship

            builder.HasOne(x => x.BloodGroup)
                .WithMany()
                .HasForeignKey(x => x.BloodGroupId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}