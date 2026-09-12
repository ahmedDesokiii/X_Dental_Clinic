using System.Reflection;

namespace Dental.Constants
{
    public static class EnumHelper
    {
        // ========== Enums ==========
        public enum PatientType { General = 1, Orthodontics = 2, Both = 3 }
       // public enum VisitStatus { InProgress = 1, Completed = 2, Cancelled = 3 }
        //public enum ProcedureStatus { Planned = 1, InProgress = 2, Completed = 3 }
        public enum DiagnosisCategory { Caries = 1, Periodontal = 2, Endodontic = 3, Prosthodontic = 4, Orthodontic = 5, Other = 6 }


        public static List<SelectListItem> GetEnumSelectList<T>()
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new SelectListItem
                {
                    Value = Convert.ToInt32(e).ToString(),
                    Text = GetDisplayName(e)
                }).ToList();
        }

        private static string GetDisplayName<T>(T enumValue)
        {
            var member = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First();

            var displayAttr = member.GetCustomAttribute<DisplayAttribute>();

            return displayAttr?.Name ?? enumValue.ToString();
        }



            public enum MedicationCategory
            {
                Antibiotic = 1,      // مضاد حيوي
                PainKiller = 2,      // مسكن
                AntiInflammatory = 3,// مضاد التهاب
                MouthWash = 4,       // غسول فم
                Vitamin = 5,         // فيتامينات
                LocalAnesthetic = 6, // بنج موضعي
                Other = 99           // أخرى
            }

            public enum TreatmentPlanStatus
            {
                Draft = 1,        // مسودة — لسه ما تعرضتش على المريض
                Approved = 2,     // المريض وافق
                InProgress = 3,   // جاري التنفيذ
                Completed = 4,    // اكتملت كل المراحل
                Cancelled = 5     // اتلغت
            }

            public enum TreatmentStepStatus
            {
                Planned = 1,     // مجدولة
                InProgress = 2,  // شغال عليها دلوقتي
                Done = 3,        // خلصت
                Skipped = 4      // اتلغت/اتخطت
            }
        


}
}
