namespace Dental.Constants
{
    /// <summary>
    /// 🔒 A3 — كل صلاحيات النظام.
    /// الصيغة: Permissions.{Module}.{الاسم الظاهر}
    /// (شاشة "إدارة الصلاحيات" بتجمّعهم حسب الجزء التاني وتعرض الجزء التالت).
    /// ⚠️ ما تغيّرش النص بعد ما يتسجل في الداتابيز — النص نفسه هو المفتاح.
    /// </summary>
    public class Permissions
    {
        public static List<string> GeneratePermissionsList(string module) => GenerateAllPermissions();

        public static List<string> GenerateAllPermissions() => new()
        {
            Patients.View_Patients, Patients.Add_Patient, Patients.Edit_Patient,
            Patients.Delete_Patient, Patients.Details_Patient,

            Clinical.View, Clinical.Manage,

            Appointments.View, Appointments.Manage,

            Payments.View, Payments.Add, Payments.Void, Payments.Reports,

            Diagnosis.View, Diagnosis.Manage,
            Medications.View, Medications.Manage,
            Laboratory.View, Laboratory.Manage,
            Inventory.View, Inventory.Manage,
        };

        /// <summary>صلاحيات الموديولات الجديدة (كل حاجة ما عدا المرضى) — بتتدّي للأدوار القديمة عند الترقية عشان محدش يتقفل عليه.</summary>
        public static List<string> NonPatientPermissions() =>
            GenerateAllPermissions().Where(p => !p.StartsWith("Permissions.Patients.")).ToList();

        // ── المرضى (نفس النصوص القديمة بالظبط) ─────────────
        public static class Patients
        {
            public const string View_Patients = "Permissions.Patients.قائمة المرضي";
            public const string Add_Patient = "Permissions.Patients.اضافة مريض";
            public const string Edit_Patient = "Permissions.Patients.تعديل مريض";
            public const string Delete_Patient = "Permissions.Patients.حذف مريض";
            public const string Details_Patient = "Permissions.Patients.تفاصيل مريض";
        }

        // ── الجلسات والسجل الطبي (Session, Dental chart, Ortho, Treatment plans) ──
        public static class Clinical
        {
            public const string View = "Permissions.Clinical.عرض الجلسات والسجل الطبي";
            public const string Manage = "Permissions.Clinical.بدء وتسجيل الجلسات";
        }

        public static class Appointments
        {
            public const string View = "Permissions.Appointments.عرض المواعيد";
            public const string Manage = "Permissions.Appointments.إضافة وتعديل المواعيد";
        }

        public static class Payments
        {
            public const string View = "Permissions.Payments.كشف حساب المريض";
            public const string Add = "Permissions.Payments.تسجيل دفعة";
            public const string Void = "Permissions.Payments.إلغاء دفعة";
            public const string Reports = "Permissions.Payments.تقارير الإيرادات اليومية";
        }

        public static class Diagnosis
        {
            public const string View = "Permissions.Diagnosis.عرض التشخيصات والأسعار";
            public const string Manage = "Permissions.Diagnosis.إدارة التشخيصات والأسعار";
        }

        public static class Medications
        {
            public const string View = "Permissions.Medications.عرض الأدوية";
            public const string Manage = "Permissions.Medications.إدارة الأدوية";
        }

        public static class Laboratory
        {
            public const string View = "Permissions.Laboratory.عرض المعمل";
            public const string Manage = "Permissions.Laboratory.تعديل أسعار المعمل";
        }

        public static class Inventory
        {
            public const string View = "Permissions.Inventory.عرض المخزن";
            public const string Manage = "Permissions.Inventory.إدارة المخزن";
        }
    }
}
