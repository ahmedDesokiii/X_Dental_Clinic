
using Dental.Models;
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

        }
      
       
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Gender> Genders => Set<Gender>();
        public DbSet<BloodGroup> BloodGroups => Set<BloodGroup>();
        public DbSet<PatientContact> PatientContacts => Set<PatientContact>();
        public DbSet<PatientMedicalInfo> PatientMedicalInfo => Set<PatientMedicalInfo>();
        public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<PatientTag> PatientTags => Set<PatientTag>();
        public DbSet<Visit> Visits => Set<Visit>();
    }
}