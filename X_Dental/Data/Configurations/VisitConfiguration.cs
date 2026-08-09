using Dental.Models.Patient;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Discount).HasColumnType("decimal(18,2)");
        
        builder.HasOne(v => v.Patient)
               .WithMany(p => p.Visits)
               .HasForeignKey(v => v.PatientId)
               .OnDelete(DeleteBehavior.Cascade);

        
    }
}