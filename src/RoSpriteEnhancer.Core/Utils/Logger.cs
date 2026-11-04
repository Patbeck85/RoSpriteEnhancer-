using System;
using System.IO;
using System.Text;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Strukturierter Logger für RoSpriteEnhancer.
    /// Schreibt Log-Einträge in eine tägliche Log-Datei unter
    /// %APPDATA%\RoSpriteEnhancer\Logs\rosprite_yyyy-MM-dd.log
    ///
    /// Log-Level:
    ///   Debug   - Detaillierte Diagnoseinformationen (nur im Debug-Build)
    ///   Info    - Allgemeine Informationsmeldungen
    ///   Warn    - Warnungen, die nicht zum Abbruch führen
    ///   Error   - Fehler, die zu Funktionsausfällen führen
    ///   Fatal   - Kritische Fehler, die zum Absturz führen können
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();
        private static string? _logDirectory;
        private static string _currentLogFile = "";
        private static string _currentDate = "";

        static Logger()
        {
            try
            {
                _logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RoSpriteEnhancer", "Logs");
                Directory.CreateDirectory(_logDirectory);
            }
            catch
            {
                // Falls AppData nicht beschreibbar, Logs in temporären Ordner
                _logDirectory = Path.Combine(Path.GetTempPath(), "RoSpriteEnhancerLogs");
                try { Directory.CreateDirectory(_logDirectory!); } catch { }
            }
        }

        /// <summary>
        /// Gibt zurück, ob Logging aktiviert ist.
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Schreibt eine Debug-Meldung (nur wenn DEBUG-Kompilationssymbol gesetzt).
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Debug(string message, string? category = null)
        {
            if (!IsEnabled) return;
            Write(LogLevel.Debug, message, category);
        }

        /// <summary>
        /// Schreibt eine Informationsmeldung.
        /// </summary>
        public static void Info(string message, string? category = null)
        {
            if (!IsEnabled) return;
            Write(LogLevel.Info, message, category);
        }

        /// <summary>
        /// Schreibt eine Warnmeldung.
        /// </summary>
        public static void Warn(string message, string? category = null)
        {
            if (!IsEnabled) return;
            Write(LogLevel.Warn, message, category);
        }

        /// <summary>
        /// Schreibt eine Fehlermeldung.
        /// </summary>
        public static void Error(string message, Exception? exception = null, string? category = null)
        {
            if (!IsEnabled) return;
            string fullMessage = exception != null
                ? $"{message} | Exception: {exception.GetType().Name}: {exception.Message}"
                : message;
            Write(LogLevel.Error, fullMessage, category);
        }

        /// <summary>
        /// Schreibt eine kritische Fehlermeldung.
        /// </summary>
        public static void Fatal(string message, Exception? exception = null, string? category = null)
        {
            if (!IsEnabled) return;
            string fullMessage = exception != null
                ? $"{message} | Exception: {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                : message;
            Write(LogLevel.Fatal, fullMessage, category);
        }

        private static void Write(LogLevel level, string message, string? category)
        {
            lock (_lock)
            {
                try
                {
                    RotateLogFileIfNeeded();

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string levelStr = level.ToString().ToUpperInvariant();
                    string catStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
                    string line = $"[{timestamp}] {levelStr} {catStr}{message}";

                    File.AppendAllText(_currentLogFile, line + Environment.NewLine, Encoding.UTF8);

                    // Im Debug-Modus auch an Debug-Ausgabe senden
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(line);
#endif
                }
                catch
                {
                    // Logging darf niemals den Anwendungsfluss unterbrechen
                }
            }
        }

        private static void RotateLogFileIfNeeded()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_currentDate == today && !string.IsNullOrEmpty(_currentLogFile))
                return;

            _currentDate = today;
            _currentLogFile = Path.Combine(_logDirectory!, $"rosprite_{today}.log");
        }

        /// <summary>
        /// Löscht alle Log-Dateien älter als die angegebene Anzahl Tage.
        /// </summary>
        public static void CleanupOldLogs(int maxDays = 30)
        {
            try
            {
                if (_logDirectory == null || !Directory.Exists(_logDirectory))
                    return;

                foreach (var file in Directory.GetFiles(_logDirectory, "rosprite_*.log"))
                {
                    var info = new FileInfo(file);
                    if ((DateTime.Now - info.CreationTime).TotalDays > maxDays)
                        info.Delete();
                }
            }
            catch
            {
                // Cleanup-Fehler ignorieren
            }
        }
    }

    /// <summary>
    /// Log-Level für strukturiertes Logging.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }
}