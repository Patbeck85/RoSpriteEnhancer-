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
                    MessageBox.Show(string.Format(LanguageService.GetString("msg.update_check_failed"), result.ErrorMessage),
                        LanguageService.GetString("msg.plugin_error"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (result.UpdateAvailable)
                {
                    var msg = string.Format(LanguageService.GetString("msg.update_available"),
                        result.CurrentVersion, result.LatestVersion, result.ReleaseNotes);
                    if (MessageBox.Show(msg, LanguageService.GetString("msg.update_available_title"), MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        AutoUpdateService.OpenDownloadUrl(result.DownloadUrl);
                    }
                }
                else
                {
                    MessageBox.Show(string.Format(LanguageService.GetString("msg.update_already_latest"), result.CurrentVersion),
                        LanguageService.GetString("msg.plugin_error"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LanguageService.GetString("msg.update_check_error"), ex.Message),
                    LanguageService.GetString("msg.plugin_error"),
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
