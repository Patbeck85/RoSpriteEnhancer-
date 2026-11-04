using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using RoSpriteEnhancer.Core.Utils;
using RoSpriteEnhancer.Core.Services;
using RoSpriteEnhancer.Views.Converters;
using RoSpriteEnhancer.Views.Windows;

namespace RoSpriteEnhancer
{
    public partial class App : Application
    {
        public static string? InitialStartupPath { get; private set; }

        public App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Standardsprache: English
            LanguageService.LoadLanguage("en");

            // RTL-Unterstützung: FlowDirection bei Sprachänderung anwenden
            LanguageService.LanguageChanged += OnLanguageChanged;

            // GLOBAL CRASH GUARD: Fängt unvorhergesehene Fehler ab -> KEIN Hard-Crash!
            AppDomain.CurrentDomain.UnhandledException += OnGlobalUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // FlowDirection auf alle offenen Fenster anwenden
            foreach (Window window in Application.Current.Windows)
            {
                RtlSupport.ApplyFlowDirection(window);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
            {
                InitialStartupPath = e.Args[0];
            }

            // Erster Start Wizard — wird angezeigt wenn keine Einstellungen existieren
            var stateFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer", "windowstate.json");
            if (!File.Exists(stateFile))
            {
                var wizard = new FirstRunWizard();
                if (wizard.ShowDialog() == true)
                {
                    // Wizard abgeschlossen — MainWindow wird normal geladen
                }
                else
                {
                    // Benutzer hat abgebrochen — Anwendung beenden
                    Current.Shutdown();
                    return;
                }
            }

            // Silent Auto-Update-Check im Hintergrund
            _ = CheckForUpdateSilentlyAsync();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception);
            MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten:\n{e.Exception.Message}\n\nLog wurde unter .workspace_temp/logs/crash.log gespeichert.", "RoSpriteEnhancer Schutzschild", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true; // Verhindert Anwendungs-Absturz!
        }

        private void OnGlobalUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex) LogCrash(ex);
        }

        private static void LogCrash(Exception ex)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".workspace_temp", "logs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "crash.log");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(logFile, entry);
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"[App] LogCrash Fehler: {logEx.Message}");
            }
        }

        /// <summary>
        /// Prüft im Hintergrund auf Updates und installiert sie automatisch.
        /// Zeigt keine Benutzeroberfläche an.
        /// </summary>
        private async Task CheckForUpdateSilentlyAsync()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10)); // Kurz warten, damit UI laden kann

                var result = await AutoUpdateService.CheckForUpdateAsync();
                if (result.UpdateAvailable && !string.IsNullOrEmpty(result.DownloadUrl))
                {
                    var downloadedPath = await AutoUpdateService.DownloadUpdateAsync(result.DownloadUrl);
                    if (downloadedPath != null)
                    {
                        AutoUpdateService.InstallUpdate(downloadedPath);
                    }
                }
            }
            catch { /* Silent - kein Benutzer-Input bei Fehlern */ }
        }
    }
}
