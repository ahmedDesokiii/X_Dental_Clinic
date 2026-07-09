using System.Reflection;

namespace Dental.Constants
{
    public static class EnumHelper
    {
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
