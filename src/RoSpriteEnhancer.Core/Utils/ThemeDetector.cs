using Microsoft.Win32;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Erkennt das aktuelle Windows-Systemdesign (Hell oder Dunkel)
    /// über die Windows-Registry.
    /// </summary>
    public static class ThemeDetector
    {
        /// <summary>
        /// Gibt zurück, ob Windows das Dunkle Design verwendet.
        /// Prüft HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme.
        /// </summary>
        public static bool IsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                        return intValue == 0; // 0 = Dunkel, 1 = Hell
                }
            }
            catch
            {
                // Bei Fehlern standardmäßig Hell annehmen
            }
            return false;
        }

        /// <summary>
        /// Gibt den erkannten Design-Namen zurück.
        /// </summary>
        public static string GetThemeName() => IsDarkMode() ? "Dark" : "Light";
    }

    /// <summary>
    /// Verfügbare Design-Modi für die Anwendung.
    /// </summary>
    public enum AppThemeMode
    {
        System,   // Automatische Erkennung des Windows-Designs
        Light,    // Immer helles Design
        Dark,     // Immer dunkles Design
    }
}