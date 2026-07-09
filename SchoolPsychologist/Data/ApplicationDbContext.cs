
using Microsoft.EntityFrameworkCore;
using SchoolPsychologist.DTOs;
using SchoolPsychologist.Models;
using SchoolPsychologist.Models.User;
using SchoolPsychologist.Models.User;
using System.Reflection.Emit;

namespace SchoolPsychologist.Data
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
            #region EF - Users | Roles

            builder.Entity<ApplicationUser>().ToTable("ERP_WebUsers");
            builder.Entity<ApplicationRole>().ToTable("ERP_Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("ERP_UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("ERP_UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("ERP_UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("ERP_RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("ERP_UserToken");
            #endregion

            #region No Keys
                builder.Entity<PsychologyCaseDto>().HasNoKey().ToView(null);
            #endregion
        }
        #region DataSets [Student - Class - Psy_Cases - Psy_Reports]
        public DbSet<StudentAttendance> StudentAttendances { get; set; }
        public DbSet<TeacherReport> TeacherReports { get; set; }
        public DbSet<PsychologyReport> PsychologyReports { get; set; }
        public DbSet<SocialReport> SocialReports { get; set; }
        public  DbSet<Student> Students { get; set; }

        public DbSet<Parent> Parents { get; set; }
        public  DbSet<SchoolClass> Classes { get; set; }
        public DbSet<Psy_Case> Psy_Cases { get; set; }
        public DbSet<Psy_Report> Psy_Reports { get; set; }
        #endregion
        #region DataSets DTOs
        public DbSet<PsychologyCaseDto> PsychologyCaseDtos { get; set; }
        #endregion

       
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}