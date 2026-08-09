using System.Reflection;

namespace Dental.Constants
{
    public static class EnumHelper
    {
        // ========== Enums ==========
        public enum PatientType { General = 1, Orthodontics = 2, Both = 3 }
        public enum VisitStatus { InProgress = 1, Completed = 2, Cancelled = 3 }
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
    }
}
