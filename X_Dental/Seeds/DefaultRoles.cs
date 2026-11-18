using Dental.Models.User;

namespace Dental.Seeds
{
    public static class DefaultRoles
    {
        private const string SeedClaimType = "SeedVersion";
        private const string PermissionsSeedV1 = "permissions-v1";

        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // Admin و User زي الأول (لو مش موجودين)
            foreach (var name in new[] { Roles.Admin.ToString(), Roles.User.ToString() })
            {
                if (!await roleManager.RoleExistsAsync(name))
                    await roleManager.CreateAsync(new ApplicationRole(name));
            }
        }

        /// <summary>
        /// 🔒 A3 — تهيئة الصلاحيات مرة واحدة بس (بعلامة SeedVersion على دور Admin):
        /// 1) أي دور قديم (غير Admin) ياخد كل صلاحيات الموديولات الجديدة
        ///    عشان محدش يتقفل عليه بعد التحديث — صلاحيات المرضى بتاعته زي ما هي.
        /// 2) إنشاء دورين جاهزين: طبيب (كل الصلاحيات) واستقبال (صلاحيات محدودة).
        /// بعد كده المدير يعدّل من شاشة "إدارة الصلاحيات" والسيستم مش هيرجع يكتب فوق تعديلاته.
        /// </summary>
        public static async Task SeedPermissionsAsync(RoleManager<ApplicationRole> roleManager)
        {
            var admin = await roleManager.FindByNameAsync(Roles.Admin.ToString());
            if (admin == null) return;

            var adminClaims = await roleManager.GetClaimsAsync(admin);
            if (adminClaims.Any(c => c.Type == SeedClaimType && c.Value == PermissionsSeedV1))
                return;

            var doctorName = Roles.Doctor.ToString();
            var receptionName = Roles.Reception.ToString();

            // 1) الأدوار الموجودة قبل التحديث
            var existingRoles = roleManager.Roles
                .Where(r => r.Name != Roles.Admin.ToString())
                .ToList();
            foreach (var role in existingRoles)
                await GrantAsync(roleManager, role, Permissions.NonPatientPermissions());

            // 2) طبيب
            if (!await roleManager.RoleExistsAsync(doctorName))
            {
                await roleManager.CreateAsync(new ApplicationRole(doctorName) { Details = "طبيب — كل الصلاحيات ما عدا إدارة المستخدمين" });
                var doctor = await roleManager.FindByNameAsync(doctorName);
                if (doctor != null) await GrantAsync(roleManager, doctor, Permissions.GenerateAllPermissions());
            }

            // 3) استقبال
            if (!await roleManager.RoleExistsAsync(receptionName))
            {
                await roleManager.CreateAsync(new ApplicationRole(receptionName) { Details = "استقبال — المرضى، المواعيد، تحصيل الدفعات" });
                var reception = await roleManager.FindByNameAsync(receptionName);
                if (reception != null) await GrantAsync(roleManager, reception, new[]
                {
                    Permissions.Patients.View_Patients,
                    Permissions.Patients.Add_Patient,
                    Permissions.Patients.Edit_Patient,
                    Permissions.Patients.Details_Patient,
                    Permissions.Clinical.View,
                    Permissions.Appointments.View,
                    Permissions.Appointments.Manage,
                    Permissions.Payments.View,
                    Permissions.Payments.Add,
                    Permissions.Diagnosis.View,
                    Permissions.Medications.View,
                });
            }

            await roleManager.AddClaimAsync(admin, new Claim(SeedClaimType, PermissionsSeedV1));
        }

        private static async Task GrantAsync(RoleManager<ApplicationRole> roleManager, ApplicationRole role, IEnumerable<string> permissions)
        {
            var current = (await roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var p in permissions)
                if (!current.Contains(p))
                    await roleManager.AddClaimAsync(role, new Claim("Permission", p));
        }
    }
}
