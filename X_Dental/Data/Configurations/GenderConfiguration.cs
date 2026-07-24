using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class GenderConfiguration
        : IEntityTypeConfiguration<Gender>
    {
        public void Configure(EntityTypeBuilder<Gender> builder)
        {
            builder.ToTable("ERP_Genders");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.Code)
                .HasMaxLength(20)
                .IsRequired();



            builder.HasIndex(x => x.Code)
                .IsUnique();



            builder.Property(x => x.NameAr)
                .HasMaxLength(100)
                .IsRequired();



            builder.Property(x => x.NameEn)
                .HasMaxLength(100);



            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);
        }
    }
}