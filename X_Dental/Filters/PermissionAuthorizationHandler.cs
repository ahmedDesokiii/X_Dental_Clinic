namespace Dental.Filters
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        public PermissionAuthorizationHandler()
        {

        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null)
                return;

            // 🔒 A3: مدير النظام له كل الصلاحيات تلقائيًا
            if (context.User.IsInRole(Roles.Admin.ToString()))
            {
                context.Succeed(requirement);
                return;
            }

            var canAccess = context.User.Claims.Any(c => c.Type == "Permission" && c.Value == requirement.Permission && c.Issuer == "LOCAL AUTHORITY");

            if (canAccess)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
