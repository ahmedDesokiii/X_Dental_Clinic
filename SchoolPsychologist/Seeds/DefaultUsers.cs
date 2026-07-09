

using SchoolPsychologist.Models.User;

namespace SchoolPsychologist.Seeds
{
    public static class DefaultUsers
    {
        //Default Parent
        public static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            //Seed Default User
            var defaultUser = new ApplicationUser
            {

                UserName = "user",
                Email = "user@first.com",
                NormalizedUserName = "user".ToUpper(),
                NormalizedEmail = "user@first.com".ToUpper(),
                EmailConfirmed = true,
                PhoneNumber = "01000000000",
                FullName_Ar = "مستخدم",
                FullName_En = "user",
                IdentityNum = "user000000"


            };
            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "$Aa123123");
                    await userManager.AddToRoleAsync(defaultUser, Roles.User.ToString());
                }
                //await roleManager.SeedClaimsForUser();
            }
        }

        //Default Admin
        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            //Seed Default User
            var defaultUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@first.com",
                NormalizedUserName = "admin".ToUpper(),
                NormalizedEmail = "admin@first.com".ToUpper(),
                EmailConfirmed = true,
                PhoneNumber = "02000000000",
                FullName_Ar = "مدير النظام",
                FullName_En = "admin",
                IdentityNum = "admin000000"

            };
            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "$Aa123123");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
                }
                //await roleManager.SeedClaimsForAdmin();
            }
        }


        private async static Task SeedClaimsForAdmin(this RoleManager<IdentityRole> roleManager)
        {
            var adminRole = await roleManager.FindByNameAsync("Admin");
            await roleManager.AddPermissionClaim(adminRole, "");
        }
        private async static Task SeedClaimsForUser(this RoleManager<IdentityRole> roleManager)
        {
            var userRole = await roleManager.FindByNameAsync("User");
            await roleManager.AddPermissionClaim(userRole, "");
        }

        public static async Task AddPermissionClaim(this RoleManager<IdentityRole> roleManager, IdentityRole role, string module)
        {
            var allClaims = await roleManager.GetClaimsAsync(role);
            var allPermissions = Permissions.GeneratePermissionsList(module);
            foreach (var permission in allPermissions)
            {
                if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                }
            }
        }
    }
}
