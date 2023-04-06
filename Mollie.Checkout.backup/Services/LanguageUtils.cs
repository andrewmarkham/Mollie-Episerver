using System.Globalization;
using System.Linq;

namespace Mollie.Checkout.Services
{
    public static class LanguageUtils
    {
        public static string GetLocale(string languageId)
        {
            var cultureInfo = new CultureInfo(languageId);

            return cultureInfo.TextInfo.CultureName;
        }

        public static string GetLocale(string languageId, string iso2CountryCode)
        {
            var validValues = new[]
            {
                "en-US", 
                "nl-NL",
                "nl-BE",
                "fr-FR",
                "fr-BE",
                "de-DE",
                "de-AT",
                "de-CH",
                "es-ES",
                "ca-ES",
                "pt-PT",
                "it-IT",
                "nb-NO",
                "sv-SE",
                "fi-FI",
                "da-DK",
                "is-IS",
                "hu-HU",
                "pl-PL",
                "lv-LV",
                "lt-LT"
            };

            return validValues.Contains($"{languageId}-{iso2CountryCode.ToUpper()}") ? 
                $"{languageId}-{iso2CountryCode.ToUpper()}" : 
                GetLocale(languageId);
        }
    }
}
