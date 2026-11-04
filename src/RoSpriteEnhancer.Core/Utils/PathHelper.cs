using System.IO;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Zentrale Hilfsklasse für wiederkehrende Dateisystem-Operationen.
    /// Vermeidet Code-Duplikation bei Pfad-Prüfungen, Datei-Existenz-Checks
    /// und temporären Verzeichnissen.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Prüft, ob ein Pfad gültig und zugänglich ist.
        /// </summary>
        public static bool IsValidPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                var fullPath = Path.GetFullPath(path);
                return !string.IsNullOrEmpty(fullPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob eine Datei existiert und zugänglich ist.
        /// </summary>
        public static bool FileExists(string? path)
        {
            if (!IsValidPath(path)) return false;
            try
            {
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob ein Verzeichnis existiert und zugänglich ist.
        /// </summary>
        public static bool DirectoryExists(string? path)
        {
            if (!IsValidPath(path)) return false;
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Erstellt ein Verzeichnis, falls es nicht existiert.
        /// </summary>
        public static bool EnsureDirectory(string path)
        {
            if (!IsValidPath(path)) return false;
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kombiniert mehrere Pfadsegmente sicher.
        /// </summary>
        public static string Combine(params string[] paths)
        {
            string result = "";
            foreach (var p in paths)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    result = Path.Combine(result, p);
                }
            }
            return result;
        }

        /// <summary>
        /// Gibt den sicheren Dateinamen zurück (ohne Pfad-Traversal-Zeichen).
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            fileName = fileName.Replace("..", "_").Replace("/", "_").Replace("\\", "_");
            return fileName;
        }

        /// <summary>
        /// Erstellt einen temporären Verzeichnispfad mit eindeutigem Namen.
        /// </summary>
        public static string CreateTempDirectory(string prefix = "rosprite")
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        /// <summary>
        /// Löscht ein Verzeichnis rekursiv, falls es existiert.
        /// </summary>
        public static void SafeDeleteDirectory(string? path)
        {
            if (!DirectoryExists(path)) return;
            try
            {
                Directory.Delete(path, true);
            }
            catch { /* Bestehendes Verhalten nicht beeinträchtigen */ }
        }

        /// <summary>
        /// Löscht eine Datei, falls sie existiert.
        /// </summary>
        public static void SafeDeleteFile(string? path)
        {
            if (!FileExists(path)) return;
            try
            {
                File.Delete(path);
            }
            catch { /* Bestehendes Verhalten nicht beeinträchtigen */ }
        }
    }
}