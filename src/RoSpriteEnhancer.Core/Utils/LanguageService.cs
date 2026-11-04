using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Verwaltet die Anwendungs-Sprache und stellt übersetzte Texte bereit.
    /// Standardsprache: English (en).
    /// </summary>
    public static class LanguageService
    {
        private static readonly string LocalesDir;
        private static Dictionary<string, string> _strings = new();
        private static string _currentLanguageCode = "en";
        private static string _currentLanguageName = "English";

        static LanguageService()
        {
            LocalesDir = Path.Combine(AppContext.BaseDirectory, "Locales");
        }

        public static string CurrentLanguageCode => _currentLanguageCode;
        public static string CurrentLanguageName => _currentLanguageName;

        public static event EventHandler? LanguageChanged;

        /// <summary>
        /// Lädt eine Sprachdatei und setzt sie als aktuelle Sprache.
        /// </summary>
        public static bool LoadLanguage(string languageCode)
        {
            try
            {
                var file = Path.Combine(LocalesDir, $"{languageCode}.json");
                if (!File.Exists(file))
                {
                    // Fallback auf English
                    file = Path.Combine(LocalesDir, "en.json");
                    if (!File.Exists(file)) return false;
                    languageCode = "en";
                }

                var json = File.ReadAllText(file);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        dict[prop.Name] = prop.Value.GetString() ?? "";
                    }
                }

                _strings = dict;
                _currentLanguageCode = languageCode;
                _currentLanguageName = dict.TryGetValue("language", out var langName) ? langName : languageCode;

                LanguageChanged?.Invoke(null, EventArgs.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gibt den übersetzten Text für einen Key zurück, oder den Key selbst falls nicht gefunden.
        /// </summary>
        public static string GetString(string key)
        {
            return _strings.TryGetValue(key, out var value) ? value : key;
        }

        /// <summary>
        /// Gibt den übersetzten Text mit Format-Argumenten zurück.
        /// </summary>
        public static string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        /// <summary>
        /// Gibt alle verfügbaren Sprachcodes zurück.
        /// </summary>
        public static IEnumerable<string> GetAvailableLanguages()
        {
            if (!Directory.Exists(LocalesDir)) yield break;
            foreach (var file in Directory.GetFiles(LocalesDir, "*.json"))
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
        }
    }
}
