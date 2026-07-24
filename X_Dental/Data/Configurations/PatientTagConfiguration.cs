using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class PatientTagConfiguration
        : IEntityTypeConfiguration<PatientTag>
    {
        public void Configure(EntityTypeBuilder<PatientTag> builder)
        {
            builder.ToTable("ERP_PatientTags");


            builder.HasKey(x => x.Id);



            builder.HasOne(x => x.Patient)
                .WithMany(x => x.PatientTags)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            builder.HasOne(x => x.Tag)
                .WithMany(x => x.PatientTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);



            // Prevent duplicate tag for same patient

            builder.HasIndex(x => new
            {
                x.PatientId,
                x.TagId
            })
            .IsUnique();

        }
    }
}