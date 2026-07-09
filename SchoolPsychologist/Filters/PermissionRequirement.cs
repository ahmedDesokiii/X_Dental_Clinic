namespace SchoolPsychologist.Filters
{
    public class PermissionRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public string Permission { get; private set; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
