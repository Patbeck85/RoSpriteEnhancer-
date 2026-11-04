using System;
using System.Threading.Tasks;
using System.Windows;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer.Views.Windows
{
    /// <summary>
    /// Über-Dialog mit Versionsnummer, Entwickler-Info und Bibliotheksliste.
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            UpdateTooltips();
        }

        private void UpdateTooltips()
        {
            BtnCheckUpdate.ToolTip = LanguageService.GetString("tooltip.btn.update_check");
            BtnClose.ToolTip = LanguageService.GetString("tooltip.btn.close");
        }

        private async void BtnCheckUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                var result = await AutoUpdateService.CheckForUpdateAsync();

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    MessageBox.Show($"Update-Check fehlgeschlagen:\n{result.ErrorMessage}", "Update",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (result.UpdateAvailable)
                {
                    var msg = $"Update verfügbar!\n\nAktuelle Version: {result.CurrentVersion}\nNeue Version: {result.LatestVersion}\n\n{result.ReleaseNotes}\n\nMöchten Sie die Download-Seite öffnen?";
                    if (MessageBox.Show(msg, "Update verfügbar", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        AutoUpdateService.OpenDownloadUrl(result.DownloadUrl);
                    }
                }
                else
                {
                    MessageBox.Show($"Sie verwenden bereits die neueste Version ({result.CurrentVersion}).", "Update",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Update-Check:\n{ex.Message}", "Update",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
