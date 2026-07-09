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
                $"Permissions.{module}.قائمة الطلاب",
                $"Permissions.{module}.اضافة طالب",
                $"Permissions.{module}.تعديل طالب",
                $"Permissions.{module}.حذف طالب",
                $"Permissions.{module}.تفاصيل طالب",
                $"Permissions.{module}.تقرير معلم",
                $"Permissions.{module}.تقرير نفسي",
                $"Permissions.{module}.تقرير اجتماعي",
                $"Permissions.{module}.تقرير حضور",
            };
        }
        public static List<string> GenerateAllPermissions()
        {
            var allPermissions = new List<string>();
            var Student_Permissions = new List<string>();

            Student_Permissions.AddRange(Generate_Students_PermissionsList(Modules.Students.ToString()));
            
            allPermissions = Student_Permissions
                //.Concat(DW_CRM_Permissions)
                                           .ToList();

            return allPermissions;
        }

        #region Students Modules
        public static class Students
        {
            public const string View_Students = "Permissions.Students.قائمة الطلاب";
            public const string Add_Student = "Permissions.Students.اضافة طالب";
            public const string Edit_Student = "Permissions.Students.تعديل طالب";
            public const string Delete_Student = "Permissions.Students.حذف طالب";
            public const string Details_Student = "Permissions.Students.تفاصيل طالب";
            public const string RepTeacher_Student = "Permissions.Students.تقرير معلم";
            public const string RepPsychology_Student = "Permissions.Students.تقرير نفسي";
            public const string RepSocial_Student = "Permissions.Students.تقرير اجتماعي";
            public const string RepAttendance_Student = "Permissions.Students.تقرير حضور";
        }
        #endregion


    }
}
