using System.Globalization;

namespace ELClass.services
{
    public static class CultureHelper
    {
        public static bool IsArabic =>
           CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar";
    }
}
