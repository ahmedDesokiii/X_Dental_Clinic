namespace Dental.Constants
{
    public class Permissions
    {
        public static List<string> GeneratePermissionsList(string module)
        {
            var permissionsList = GenerateAllPermissions();
            return permissionsList;
        }
        public static List<string> Generate_Students_PermissionsList(string module)
        {
            return new List<string>()
            {
                $"Permissions.{module}.قائمة المرضي",
                $"Permissions.{module}.اضافة مريض",
                $"Permissions.{module}.تعديل مريض",
                $"Permissions.{module}.حذف مريض",
                $"Permissions.{module}.تفاصيل مريض",
            };
        }
        public static List<string> GenerateAllPermissions()
        {
            var allPermissions = new List<string>();
            var Patient_Permissions = new List<string>();

            Patient_Permissions.AddRange(Generate_Students_PermissionsList(Modules.Patients.ToString()));
            
            allPermissions = Patient_Permissions
                //.Concat(DW_CRM_Permissions)
                                           .ToList();

            return allPermissions;
        }

        #region Patients Modules
        public static class Patients
        {
            public const string View_Patients = "Permissions.Patients.قائمة المرضي";
            public const string Add_Patient = "Permissions.Patients.اضافة مريض";
            public const string Edit_Patient = "Permissions.Patients.تعديل مريض";
            public const string Delete_Patient = "Permissions.Patients.حذف مريض";
            public const string Details_Patient = "Permissions.Patients.تفاصيل مريض";
            
        }
        #endregion


    }
}
