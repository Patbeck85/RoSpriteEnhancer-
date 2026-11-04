using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Definiert eine Tastenkombination mit Modifiern und Taste (plattformunabhängig).
    /// </summary>
    public class KeyboardShortcut
    {
        public string CommandName { get; set; } = "";
        public string Key { get; set; } = "";           // z.B. "O", "S", "F1"
        public string Modifiers { get; set; } = "None"; // z.B. "Control", "Shift", "Alt", "Control+Shift"

        public string DisplayText
        {
            get
            {
                var parts = new List<string>();
                if (Modifiers.Contains("Control")) parts.Add("Strg");
                if (Modifiers.Contains("Shift")) parts.Add("Umschalt");
                if (Modifiers.Contains("Alt")) parts.Add("Alt");
                if (!string.IsNullOrEmpty(Key)) parts.Add(Key);
                return string.Join(" + ", parts);
            }
        }
    }

    /// <summary>
    /// Verwaltet Tastaturkürzel mit Speicherung und Laden aus JSON.
    /// </summary>
    public static class KeyboardShortcutManager
    {
        private static readonly string ShortcutsFile;
        private static Dictionary<string, KeyboardShortcut> _shortcuts = new();

        static KeyboardShortcutManager()
        {
            ShortcutsFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer", "keyboardshortcuts.json");
            LoadDefaults();
            Load();
        }

        private static void LoadDefaults()
        {
            _shortcuts["OpenInput"] = new KeyboardShortcut { CommandName = "OpenInput", Key = "O", Modifiers = "Control" };
            _shortcuts["StartProcess"] = new KeyboardShortcut { CommandName = "StartProcess", Key = "S", Modifiers = "Control" };
            _shortcuts["About"] = new KeyboardShortcut { CommandName = "About", Key = "F1", Modifiers = "None" };
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(ShortcutsFile))
                {
                    var json = File.ReadAllText(ShortcutsFile);
                    var saved = JsonSerializer.Deserialize<Dictionary<string, KeyboardShortcut>>(json);
                    if (saved != null)
                    {
                        foreach (var kvp in saved)
                            _shortcuts[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { /* Bestehendes Verhalten nicht beeinträchtigen */ }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ShortcutsFile)!);
                var json = JsonSerializer.Serialize(_shortcuts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ShortcutsFile, json);
            }
            catch { }
        }

        public static KeyboardShortcut GetShortcut(string commandName)
        {
            return _shortcuts.TryGetValue(commandName, out var sc) ? sc : new KeyboardShortcut { CommandName = commandName };
        }

        public static void SetShortcut(string commandName, string key, string modifiers)
        {
            _shortcuts[commandName] = new KeyboardShortcut { CommandName = commandName, Key = key, Modifiers = modifiers };
            Save();
        }

        public static Dictionary<string, KeyboardShortcut> GetAllShortcuts() => new Dictionary<string, KeyboardShortcut>(_shortcuts);

        /// <summary>
        /// Erkennt Tastenkombinations-Konflikte (doppelte Belegung derselben Taste mit denselben Modifikatoren).
        /// </summary>
        /// <returns>Liste von Konflikten: CommandName1, CommandName2, Key, Modifiers</returns>
        public static List<(string Command1, string Command2, string Key, string Modifiers)> GetConflicts()
        {
            var conflicts = new List<(string, string, string, string)>();
            var shortcuts = _shortcuts.Values.ToList();
            for (int i = 0; i < shortcuts.Count; i++)
            {
                for (int j = i + 1; j < shortcuts.Count; j++)
                {
                    if (shortcuts[i].Key == shortcuts[j].Key &&
                        shortcuts[i].Modifiers == shortcuts[j].Modifiers &&
                        !string.IsNullOrEmpty(shortcuts[i].Key))
                    {
                        conflicts.Add((shortcuts[i].CommandName, shortcuts[j].CommandName, shortcuts[i].Key, shortcuts[i].Modifiers));
                    }
                }
            }
            return conflicts;
        }

        /// <summary>
        /// Gibt eine formatierte Zusammenfassung aller Tastenkürzel zurück.
        /// </summary>
        public static string GetShortcutsSummary()
        {
            var lines = new List<string>();
            foreach (var kvp in _shortcuts.OrderBy(k => k.Key))
            {
                lines.Add($"{kvp.Key}: {kvp.Value.DisplayText}");
            }
            var conflicts = GetConflicts();
            if (conflicts.Count > 0)
            {
                lines.Add("");
                lines.Add("KONFLIKTE:");
                foreach (var c in conflicts)
                {
                    lines.Add($"  {c.Command1} <-> {c.Command2}: {c.Key} ({c.Modifiers})");
                }
            }
            return string.Join(Environment.NewLine, lines);
        }
    }
}