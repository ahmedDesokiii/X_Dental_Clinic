
using Dental.Models;
using Dental.Models.Clinic;
using Dental.Models.Inventory;
using Dental.Models.Patient;
using Dental.Models.User;
using Dental.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Dental.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            #region EF - Users | Roles

            builder.Entity<ApplicationUser>().ToTable("ERP_WebUsers");
            builder.Entity<ApplicationRole>().ToTable("ERP_Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("ERP_UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("ERP_UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("ERP_UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("ERP_RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("ERP_UserToken");
            #endregion

            builder.Entity<Visit>()
        .Property(v => v.TotalAmount)
        .HasPrecision(18, 2);

            builder.Entity<Visit>()
                .Property(v => v.Discount)
                .HasPrecision(18, 2);

            builder.Entity<Visit>()
                .Property(v => v.PaidAmount)
                .HasPrecision(18, 2);

            builder.Entity<VisitProcedure>()
                .Property(v => v.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<VisitPayment>()
                .Property(v => v.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ProcedureCatalog>()
                .Property(p => p.DefaultPrice)
                .HasPrecision(18, 2);

            builder.Entity<LaboratoryPriceHistory>()
                .Property(h => h.Price)
                .HasPrecision(18, 2);

            builder.Entity<LaboratoryPriceHistory>()
                .HasOne(h => h.DiagnosisCatalog)
                .WithMany()
                .HasForeignKey(h => h.DiagnosisCatalogId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiagnosisCatalog>()
                .Property(d => d.Price)
                .HasPrecision(18, 2);   // ⭐ لو مش متحدد بالفعل عندك — لو موجود امسح السطر ده

            builder.Entity<VisitDiagnosis>()
                .Property(v => v.LabPriceSnapshot)
                .HasPrecision(18, 2);


            builder.Entity<TreatmentPlanStep>()
                .HasOne(s => s.TreatmentPlan)
                .WithMany(p => p.Steps)
                .HasForeignKey(s => s.TreatmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TreatmentPlanStep>()
                .HasOne(s => s.ProcedureCatalog)
                .WithMany()
                .HasForeignKey(s => s.ProcedureCatalogId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Material>()
    .Property(m => m.PurchasePrice).HasPrecision(18, 2);
            builder.Entity<Material>()
                .Property(m => m.CurrentPrice).HasPrecision(18, 2);
            builder.Entity<Material>()
                .Property(m => m.Quantity).HasPrecision(18, 3);
            builder.Entity<Material>()
                .Property(m => m.MinimumStockLevel).HasPrecision(18, 3);

            builder.Entity<Material>()
                .HasIndex(m => m.Code)
                .IsUnique();

            builder.Entity<Material>()
                .HasOne(m => m.Supplier)
                .WithMany()
                .HasForeignKey(m => m.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<StockTransaction>()
                .Property(t => t.Quantity).HasPrecision(18, 3);
            builder.Entity<StockTransaction>()
                .Property(t => t.UnitPrice).HasPrecision(18, 2);

            builder.Entity<StockTransaction>()
                .HasOne(t => t.Material)
                .WithMany(m => m.Transactions)
                .HasForeignKey(t => t.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StockTransaction>()
                .HasOne(t => t.Supplier)
                .WithMany()
                .HasForeignKey(t => t.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<MaterialHistory>()
                .Property(h => h.QuantityBefore).HasPrecision(18, 3);
            builder.Entity<MaterialHistory>()
                .Property(h => h.QuantityAfter).HasPrecision(18, 3);
            builder.Entity<MaterialHistory>()
                .Property(h => h.PriceBefore).HasPrecision(18, 2);
            builder.Entity<MaterialHistory>()
                .Property(h => h.PriceAfter).HasPrecision(18, 2);

            builder.Entity<MaterialHistory>()
                .HasOne(h => h.Material)
                .WithMany(m => m.History)
                .HasForeignKey(h => h.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MedicationCatalog>()
                .HasIndex(m => m.NameAr);

            builder.Entity<PatientDocument>()
           .HasOne(d => d.DocumentType)
           .WithMany()
           .HasForeignKey(d => d.DocumentTypeId)
           .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocumentType>().HasData(
              new DocumentType { Id = 1, NameAr = "بطاقة الرقم القومي", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 2, NameAr = "تقرير أشعة", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 3, NameAr = "تأمين طبي", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 4, NameAr = "موافقة علاج", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
              new DocumentType { Id = 5, NameAr = "أخرى", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) }
  );

            builder.Entity<OrthoCase>().Property(x => x.TotalRequired).HasPrecision(18, 2);
            builder.Entity<OrthoTotalChange>().Property(x => x.OldTotal).HasPrecision(18, 2);
            builder.Entity<OrthoTotalChange>().Property(x => x.NewTotal).HasPrecision(18, 2);


        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Gender> Genders { get; set; }
        public DbSet<BloodGroup> BloodGroups { get; set; }
        public DbSet<PatientContact> PatientContacts { get; set; }
        public DbSet<PatientDocument> PatientDocuments { get; set; }
        public DbSet<PatientMedicalInfo> PatientMedicalInfos { get; set; }
        public DbSet<PatientTag> PatientTags { get; set; }
        public DbSet<PatientXRay> PatientXRays { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<MedicationCatalog> MedicationCatalogs { get; set; }
        public DbSet<TreatmentPlan> TreatmentPlans { get; set; }
        public DbSet<TreatmentPlanStep> TreatmentPlanSteps { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<LaboratoryPriceHistory> LaboratoryPriceHistories { get; set; }

    // ⭐ NEW using

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<MaterialHistory> MaterialHistories { get; set; }

    // ⭐ NEW
    public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<DiagnosisCatalog> DiagnosisCatalogs { get; set; }
        public DbSet<ProcedureCatalog> ProcedureCatalogs { get; set; }
        public DbSet<VisitDiagnosis> VisitDiagnoses { get; set; }
        public DbSet<VisitProcedure> VisitProcedures { get; set; }
        public DbSet<VisitPayment> VisitPayments { get; set; }
        public DbSet<DentalChartEntry> DentalChartEntries { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }


        public DbSet<OrthoCase> OrthoCases => Set<OrthoCase>();
        public DbSet<OrthoVisitEntry> OrthoVisitEntries => Set<OrthoVisitEntry>();
        public DbSet<OrthoProcedureCatalog> OrthoProcedureCatalogs => Set<OrthoProcedureCatalog>();
        public DbSet<OrthoTotalChange> OrthoTotalChanges => Set<OrthoTotalChange>();
    }
}