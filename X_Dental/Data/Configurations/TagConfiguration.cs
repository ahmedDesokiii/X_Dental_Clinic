using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Data.Configurations
{
    public class TagConfiguration
        : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("ERP_Tags");


            builder.HasKey(x => x.Id);



            builder.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();



            builder.HasIndex(x => x.Code)
                .IsUnique();



            builder.Property(x => x.NameAr)
                .HasMaxLength(100)
                .IsRequired();



            builder.Property(x => x.NameEn)
                .HasMaxLength(100);



            builder.Property(x => x.Color)
                .HasMaxLength(20);



            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);
        }
    }
}