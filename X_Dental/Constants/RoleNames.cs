namespace Dental.Constants
{
    /// <summary>👥 E1 — أسماء الأدوار بالعربي للعرض.</summary>
    public static class RoleNames
    {
        public static string Display(string? role) => role switch
        {
            "Admin" => "مدير النظام",
            "Doctor" => "طبيب",
            "Reception" => "استقبال",
            "User" => "مستخدم",
            null or "" => "—",
            _ => role
        };

        /// <summary>أدوار النظام: ما ينفعش تتمسح. (Admin كمان ما ينفعش يتغير اسمه.)</summary>
        public static bool IsSystem(string? role) => role is "Admin" or "Doctor" or "Reception" or "User";
    }
}
