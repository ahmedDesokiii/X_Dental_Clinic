using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class BloodGroupConfiguration
        : IEntityTypeConfiguration<BloodGroup>
    {
        public void Configure(EntityTypeBuilder<BloodGroup> builder)
        {
            builder.ToTable("ERP_BloodGroups");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.Code)
                .HasMaxLength(10)
                .IsRequired();



            builder.HasIndex(x => x.Code)
                .IsUnique();



            builder.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();



            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);
        }
    }
}