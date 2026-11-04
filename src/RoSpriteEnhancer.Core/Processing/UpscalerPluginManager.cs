using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// Interface für externe Skalierungs-Plugins.
    /// Plugins werden als separate DLLs im plugins/-Ordner geladen.
    /// </summary>
    public interface IUpscalerPlugin
    {
        /// <summary>Einzigartige Plugin-ID (z. B. "my_upscaler_v1")</summary>
        string PluginId { get; }

        /// <summary>Anzeigename des Plugins</summary>
        string DisplayName { get; }

        /// <summary>Version des Plugins</summary>
        string Version { get; }

        /// <summary>Autor des Plugins</summary>
        string Author { get; }

        /// <summary>Beschreibung des Plugins</summary>
        string Description { get; }

        /// <summary>
        /// Skaliert ein Bitmap um den angegebenen Faktor.
        /// </summary>
        /// <param name="source">Quell-Bild (wird nicht vom Plugin entsorgt)</param>
        /// <param name="scaleFactor">Skalierungsfaktor (z. B. 2.0 für 2x)</param>
        /// <returns>Skaliertes Bild (Aufrufer muss entsorgen)</returns>
        Bitmap Scale(Bitmap source, double scaleFactor);

        /// <summary>
        /// Prüft ob das Plugin für die aktuelle Plattform verfügbar ist.
        /// </summary>
        bool IsAvailable();
    }

    /// <summary>
    /// Metadaten für ein geladenes Plugin.
    /// </summary>
    public class PluginInfo
    {
        public IUpscalerPlugin Plugin { get; set; } = null!;
        public string AssemblyPath { get; set; } = "";
        public string PluginId => Plugin.PluginId;
        public string DisplayName => Plugin.DisplayName;
        public string Version => Plugin.Version;
        public string Author => Plugin.Author;
        public string Description => Plugin.Description;
        public bool IsAvailable => Plugin.IsAvailable();
    }

    /// <summary>
    /// Verwaltet externe Upscaler-Plugins. Lädt DLLs aus einem konfigurierbaren
    /// Plugins-Ordner und stellt sie der Anwendung zur Verfügung.
    /// </summary>
    public static class UpscalerPluginManager
    {
        private static readonly List<PluginInfo> _plugins = new();
        private static readonly List<Assembly> _loadedAssemblies = new();
        private static string? _pluginsDirectory;

        /// <summary>Alle geladenen Plugins</summary>
        public static IReadOnlyList<PluginInfo> Plugins => _plugins;

        /// <summary>
        /// Initialisiert das Plugin-System und lädt alle Plugins aus dem angegebenen Ordner.
        /// </summary>
        public static void Initialize(string pluginsDirectory)
        {
            _pluginsDirectory = pluginsDirectory;
            _plugins.Clear();
            _loadedAssemblies.Clear();

            if (!Directory.Exists(pluginsDirectory))
            {
                Directory.CreateDirectory(pluginsDirectory);
                return;
            }

            foreach (var dll in Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    LoadPluginFromAssembly(dll);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PluginManager] Konnte Plugin nicht laden: {dll} — {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[PluginManager] {_plugins.Count} Plugin(s) geladen aus {pluginsDirectory}");
        }

        private static void LoadPluginFromAssembly(string assemblyPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                _loadedAssemblies.Add(assembly);

                foreach (var type in assembly.GetTypes())
                {
                    if (!typeof(IUpscalerPlugin).IsAssignableFrom(type)) continue;
                    if (type.IsAbstract || type.IsInterface) continue;

                    var plugin = (IUpscalerPlugin)Activator.CreateInstance(type)!;
                    _plugins.Add(new PluginInfo
                    {
                        Plugin = plugin,
                        AssemblyPath = assemblyPath
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PluginManager] Fehler beim Laden von {assemblyPath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sucht ein Plugin anhand seiner ID.
        /// </summary>
        public static IUpscalerPlugin? GetPlugin(string pluginId)
        {
            foreach (var p in _plugins)
            {
                if (p.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                    return p.Plugin;
            }
            return null;
        }

        /// <summary>
        /// Gibt alle verfügbaren (IsAvailable == true) Plugins zurück.
        /// </summary>
        public static IReadOnlyList<PluginInfo> GetAvailablePlugins()
        {
            var result = new List<PluginInfo>();
            foreach (var p in _plugins)
            {
                if (p.IsAvailable) result.Add(p);
            }
            return result;
        }

        /// <summary>
        /// Prüft ob ein Plugin mit der angegebenen ID registriert ist.
        /// </summary>
        public static bool IsPluginRegistered(string pluginId)
        {
            return GetPlugin(pluginId) != null;
        }
    }
}