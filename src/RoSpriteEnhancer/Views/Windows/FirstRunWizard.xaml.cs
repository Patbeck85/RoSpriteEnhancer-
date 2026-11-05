using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer.Views.Windows
{
    /// <summary>
    /// Erster Start Wizard — führt neue Benutzer durch die Erstkonfiguration.
    /// Wird nur angezeigt, wenn keine Einstellungen gespeichert wurden.
    /// </summary>
    public partial class FirstRunWizard : Window
    {
        private readonly App _app;

        public FirstRunWizard()
        {
            InitializeComponent();
            _app = Application.Current as App ?? throw new InvalidOperationException("App nicht verfügbar");
            InitLanguageCombo();
        }

        private void InitLanguageCombo()
        {
            // Verfügbare Sprachen laden
            var localesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Locales");
            if (Directory.Exists(localesDir))
            {
                foreach (var file in Directory.GetFiles(localesDir, "*.json"))
                {
                    var code = Path.GetFileNameWithoutExtension(file);
                    if (code == "en" || code == "de")
                    {
                        var name = code == "en" ? "English" : "Deutsch";
                        CmbLanguage.Items.Add(new { Code = code, Name = name });
                    }
                }
            }
            CmbLanguage.DisplayMemberPath = "Name";
            CmbLanguage.SelectedValuePath = "Code";
            CmbLanguage.SelectedValue = "en"; // English als Default
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = LanguageService.GetString("msg.dialog_input_folder") };
            if (dialog.ShowDialog() == true) TxtInputPath.Text = dialog.FolderName;
        }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = LanguageService.GetString("msg.dialog_output_folder") };
            if (dialog.ShowDialog() == true) TxtOutputPath.Text = dialog.FolderName;
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            // Sprache speichern
            var selectedLang = CmbLanguage.SelectedValue as string ?? "en";
            LanguageService.LoadLanguage(selectedLang);

            // Ordner speichern (in WindowStateService)
            WindowStateService.SaveLastFolders(TxtInputPath.Text ?? "", TxtOutputPath.Text ?? "");

            // AI-Tools herunterladen (wenn ausgewählt)
            if (ChkDownloadWaifu2x?.IsChecked == true)
            {
                var tool = AiToolManager.GetTool("waifu2x");
                if (tool != null) _ = AiToolManager.EnsureInstalledAsync(tool);
            }
            if (ChkDownloadRealCugan?.IsChecked == true)
            {
                var tool = AiToolManager.GetTool("realcugan");
                if (tool != null) _ = AiToolManager.EnsureInstalledAsync(tool);
            }
            if (ChkDownloadRealESRGAN?.IsChecked == true)
            {
                var tool = AiToolManager.GetTool("realesrgan");
                if (tool != null) _ = AiToolManager.EnsureInstalledAsync(tool);
            }
            if (ChkDownloadGFPGAN?.IsChecked == true)
            {
                var tool = AiToolManager.GetTool("gfpgan");
                if (tool != null) _ = AiToolManager.EnsureInstalledAsync(tool);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}