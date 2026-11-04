using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Speichert und stellt Fensterposition/-größe wieder her, damit die App
    /// beim nächsten Start an derselben Stelle öffnet.
    /// </summary>
    public static class WindowStateService
    {
        private static readonly string StateDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RoSpriteEnhancer");
        private static readonly string StateFile = Path.Combine(StateDir, "windowstate.json");

        public static void SaveWindowState(string windowName, double left, double top, double width, double height, string windowState)
        {
            try
            {
                Directory.CreateDirectory(StateDir);
                var state = new WindowStateData
                {
                    WindowName = windowName,
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    WindowState = windowState
                };
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFile, json);
            }
            catch { /* Bestehendes Verhalten nicht beeinträchtigen */ }
        }

        public static bool TryLoadWindowState(string windowName, out double left, out double top, out double width, out double height, out string windowState)
        {
            left = top = width = height = 0;
            windowState = "Normal";
            try
            {
                if (!File.Exists(StateFile)) return false;
                var json = File.ReadAllText(StateFile);
                var state = JsonSerializer.Deserialize<WindowStateData>(json);
                if (state == null || state.WindowName != windowName) return false;

                left = state.Left;
                top = state.Top;
                width = state.Width;
                height = state.Height;
                windowState = state.WindowState;
                return true;
            }
            catch { return false; }
        }

        public static void SaveLastFolders(string inputFolder, string outputFolder)
        {
            try
            {
                Directory.CreateDirectory(StateDir);
                var json = File.ReadAllText(Path.Combine(StateDir, "lastfolders.json"));
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var inputs = new List<string>();
                var outputs = new List<string>();
                if (data.TryGetProperty("InputFolders", out var inEl))
                    inputs = inEl.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (data.TryGetProperty("OutputFolders", out var outEl))
                    outputs = outEl.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                
                // Aktuellen Input-Ordner vorne anhängen (falls nicht leer)
                if (!string.IsNullOrEmpty(inputFolder))
                {
                    inputs.Remove(inputFolder);
                    inputs.Insert(0, inputFolder);
                    if (inputs.Count > 5) inputs.RemoveRange(5, inputs.Count - 5);
                }
                
                // Aktuellen Output-Ordner vorne anhängen (falls nicht leer)
                if (!string.IsNullOrEmpty(outputFolder))
                {
                    outputs.Remove(outputFolder);
                    outputs.Insert(0, outputFolder);
                    if (outputs.Count > 5) outputs.RemoveRange(5, outputs.Count - 5);
                }
                
                var newJson = JsonSerializer.Serialize(new
                {
                    InputFolders = inputs,
                    OutputFolders = outputs
                }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(StateDir, "lastfolders.json"), newJson);
            }
            catch { /* Bestehendes Verhalten nicht beeinträchtigen */ }
        }

        public static (List<string> InputFolders, List<string> OutputFolders) LoadLastFoldersList()
        {
            try
            {
                var file = Path.Combine(StateDir, "lastfolders.json");
                if (!File.Exists(file)) return (new List<string>(), new List<string>());
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var inputs = new List<string>();
                var outputs = new List<string>();
                if (data.TryGetProperty("InputFolders", out var inEl))
                    inputs = inEl.EnumerateArray().Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s) && Directory.Exists(s)).ToList();
                if (data.TryGetProperty("OutputFolders", out var outEl))
                    outputs = outEl.EnumerateArray().Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s) && Directory.Exists(s)).ToList();
                return (inputs, outputs);
            }
            catch { return (new List<string>(), new List<string>()); }
        }

        /// <summary>
        /// Löscht den gespeicherten Fensterzustand für ein bestimmtes Fenster.
        /// </summary>
        public static void ClearWindowState(string windowName)
        {
            try
            {
                if (!File.Exists(StateFile)) return;
                var json = File.ReadAllText(StateFile);
                var state = JsonSerializer.Deserialize<WindowStateData>(json);
                if (state != null && state.WindowName == windowName)
                {
                    // Überschreibe mit leerem Zustand
                    var emptyState = new WindowStateData
                    {
                        WindowName = windowName,
                        Left = 0, Top = 0, Width = 0, Height = 0,
                        WindowState = "Normal"
                    };
                    var emptyJson = JsonSerializer.Serialize(emptyState, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(StateFile, emptyJson);
                }
            }
            catch { /* Bestehendes Verhalten nicht beeinträchtigen */ }
        }

        private class WindowStateData
        {
            public string WindowName { get; set; } = "";
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string WindowState { get; set; } = "Normal";
        }
    }
}
