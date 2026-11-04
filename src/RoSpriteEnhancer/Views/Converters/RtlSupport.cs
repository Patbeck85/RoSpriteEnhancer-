using System;
using System.Globalization;
using System.Windows;

namespace RoSpriteEnhancer.Views.Converters
{
    /// <summary>
    /// Erkennt Right-to-Left (RTL) Sprachen und wendet die entsprechende
    /// UI-Richtung an (FlowDirection = RightToLeft).
    /// Unterstützt Arabisch, Hebräisch und andere RTL-Sprachen.
    /// </summary>
    public static class RtlSupport
    {
        /// <summary>
        /// Liste von Kultur-Codes, die RTL-Schreibrichtung verwenden.
        /// </summary>
        private static readonly string[] RtlCultures = new[]
        {
            "ar", "ar-SA", "ar-EG", "ar-AE", "ar-BH", "ar-DZ", "ar-IQ", "ar-JO", "ar-KW", "ar-LB", "ar-LY", "ar-MA", "ar-OM", "ar-QA", "ar-SY", "ar-TN", "ar-YE",
            "he", "he-IL",
            "fa", "fa-IR",
            "ur", "ur-PK",
            "yi",
            "sd", "sd-PK",
            "ug", "ug-CN"
        };

        /// <summary>
        /// Prüft, ob die angegebene Kultur eine RTL-Schreibrichtung verwendet.
        /// </summary>
        public static bool IsRtlCulture(CultureInfo culture)
        {
            if (culture == null) return false;

            string name = culture.Name ?? "";
            string twoLetter = culture.TwoLetterISOLanguageName ?? "";

            foreach (var rtl in RtlCultures)
            {
                if (name.Equals(rtl, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (twoLetter.Equals(rtl, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            try
            {
                return culture.TextInfo.IsRightToLeft;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob die aktuelle UI-Kultur eine RTL-Sprache ist.
        /// </summary>
        public static bool IsCurrentUICultureRtl()
        {
            return IsRtlCulture(CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Wendet die FlowDirection auf ein FrameworkElement an basierend auf der aktuellen UI-Kultur.
        /// Bei RTL-Sprachen wird RightToLeft verwendet, sonst LeftToRight.
        /// </summary>
        public static void ApplyFlowDirection(FrameworkElement element)
        {
            if (element == null) return;
            element.FlowDirection = IsCurrentUICultureRtl()
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        /// <summary>
        /// Gibt die empfohlene FlowDirection für die aktuelle UI-Kultur zurueck.
        /// </summary>
        public static FlowDirection GetFlowDirection()
        {
            return IsCurrentUICultureRtl()
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }
}