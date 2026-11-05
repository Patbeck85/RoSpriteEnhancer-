#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RoSpriteEnhancer.Core;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly Dictionary<string, (bool Enabled, CategoryOverride Ov)> _categoryOverrideStates = new();
        private string? _currentCategoryPickerId = null;

        public ProcessingOptions Options { get; private set; } = new();

        /// <summary>
        /// Wird ausgelöst, wenn sich eine Einstellung in der UI ändert (Live-Vorschau).
        /// </summary>
        public event EventHandler? OptionsChanged;

        public SettingsWindow(ProcessingOptions currentOptions)
        {
            InitializeComponent();
            if (WindowStateService.TryLoadWindowState("SettingsWindow", out double l, out double t, out double w, out double h, out string ws))
            {
                Left = l; Top = t; Width = w; Height = h;
                WindowState = (WindowState)Enum.Parse(typeof(WindowState), ws);
            }
            this.Closing += (_, _) =>
            {
                WindowStateService.SaveWindowState("SettingsWindow", Left, Top, Width, Height, WindowState.ToString());
                WindowStateService.SaveLastFolders(TxtInputPath?.Text ?? "", TxtOutputPath?.Text ?? "");
            };

            // Letzte Ordner wiederherstellen
            var (recentInputs, recentOutputs) = WindowStateService.LoadLastFoldersList();
            if (recentInputs.Count > 0 && Directory.Exists(recentInputs[0]))
                TxtInputPath.Text = recentInputs[0];
            if (recentOutputs.Count > 0 && Directory.Exists(recentOutputs[0]))
                TxtOutputPath.Text = recentOutputs[0];

            // Recent-Folder-ComboBoxes befüllen
            CmbRecentInputs.ItemsSource = recentInputs;
            CmbRecentInputs.SelectedIndex = -1;
            CmbRecentOutputs.ItemsSource = recentOutputs;
            CmbRecentOutputs.SelectedIndex = -1;

            // Sprachauswahl initialisieren
            InitLanguageCombo();
            // Aktuelle Optionen als Startwerte übernehmen
            Options = currentOptions;

            RefreshPresetList();
            InitCategoryOverrides();
            ApplyOptionsToUi(currentOptions);
            RegisterKeyboardShortcuts();
            UpdateTooltips();
            LoadGitHubToken();
        }

        private void UpdateTooltips()
        {
            BtnDeletePreset.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.deletePreset");
            BtnExportPreset.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.exportPreset");
            BtnImportPreset.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.importPreset");
            BtnCloudUploadPreset.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.cloudUploadPreset");
            BtnCloudDownloadPreset.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.cloudDownloadPreset");
            CmbRecentInputs.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.recentInputFolders");
            BtnClearRecentInputs.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.clearRecentInputs");
            CmbRecentOutputs.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.recentOutputFolders");
            BtnClearRecentOutputs.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.clearRecentOutputs");
            BtnSavePreset.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.save_preset");
            BtnBrowseInput.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.browse_input");
            BtnBrowseOutput.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.browse_folder");
            BtnBrowseEsrgan.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.ai_tool_browse");
            BtnBrowseWaifu2x.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.ai_tool_browse");
            BtnBrowseRealCugan.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.ai_tool_browse");
            BtnBrowseRealESRGAN.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.ai_tool_browse");
            BtnBrowseGFPGAN.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.ai_tool_browse");
            BtnOk.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.ok");
            BtnCancel.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.cancel");
            BtnApply.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.btn.apply");

            // OutputFormat ComboBox lokalisieren
            CmbOutputFormatPng.Content = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("cmb.formatPng");
            CmbOutputFormatBmpMagenta.Content = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("cmb.formatBmpMagenta");
            CmbOutputFormatBmpPalette.Content = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("cmb.formatBmpPalette");
            CmbOutputFormatTga.Content = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("cmb.formatTga");
            LoadShortcuts();
        }

        private void ChkDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            ToggleDarkMode(true);
        }

        private void ChkDarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleDarkMode(false);
        }

        private void ToggleDarkMode(bool isDark)
        {
            var app = Application.Current as App;
            if (app == null) return;
            var merged = app.Resources.MergedDictionaries;
            var darkDict = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("DarkTheme"));
            if (isDark)
            {
                if (darkDict == null)
                    merged.Add(new ResourceDictionary { Source = new Uri("Views/Windows/DarkTheme.xaml", UriKind.Relative) });
            }
            else
            {
                if (darkDict != null)
                    merged.Remove(darkDict);
            }
        }

        private void LoadGitHubToken()
        {
            var stateFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer", "ghtoken.txt");
            if (File.Exists(stateFile))
            {
                var token = File.ReadAllText(stateFile).Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    TxtGitHubToken.Text = token;
                    PresetSharingService.GitHubToken = token;
                }
            }
        }

        private void SaveGitHubToken()
        {
            var stateDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer");
            Directory.CreateDirectory(stateDir);
            var stateFile = Path.Combine(stateDir, "ghtoken.txt");
            File.WriteAllText(stateFile, TxtGitHubToken.Text?.Trim() ?? "");
            PresetSharingService.GitHubToken = TxtGitHubToken.Text?.Trim();
        }

        /// <summary>
        /// ViewModel für ein bearbeitbares Tastaturkürzel in der UI.
        /// </summary>
        private class ShortcutViewModel
        {
            public string CommandName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string DisplayText { get; set; } = "";
            public string Key { get; set; } = "";
            public string Modifiers { get; set; } = "None";
        }

        private readonly ObservableCollection<ShortcutViewModel> _shortcutViewModels = new();

        private void LoadShortcuts()
        {
            _shortcutViewModels.Clear();

            var shortcutDefs = new Dictionary<string, string>
            {
                { "OpenInput", "Eingabe-Ordner wählen" },
                { "StartProcess", "Batch-Verarbeitung starten" },
                { "About", "Über-Dialog öffnen" }
            };

            foreach (var (cmd, display) in shortcutDefs)
            {
                var sc = KeyboardShortcutManager.GetShortcut(cmd);
                _shortcutViewModels.Add(new ShortcutViewModel
                {
                    CommandName = cmd,
                    DisplayName = display,
                    DisplayText = sc.DisplayText,
                    Key = sc.Key,
                    Modifiers = sc.Modifiers
                });
            }

            LstShortcuts.ItemsSource = _shortcutViewModels;
            SetupShortcutComboBoxes();
        }

        private void SetupShortcutComboBoxes()
        {
            // Diese Methode wird aufgerufen, nachdem die ListBox geladen wurde,
            // um die ComboBoxes in jedem Item mit den richtigen Werten zu befüllen.
            // Da DataTemplate die ComboBoxes erzeugt, befüllen wir sie bei SelectionChanged.
        }

        private void FillKeyComboBox(ComboBox cmb, string currentKey)
        {
            cmb.Items.Clear();
            var keys = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            foreach (var k in keys)
                cmb.Items.Add(k);
            cmb.SelectedItem = currentKey;
        }

        private void FillModifierComboBox(ComboBox cmb, string currentModifiers)
        {
            cmb.Items.Clear();
            var modifiers = new[] { "None", "Control", "Shift", "Alt", "Control+Shift", "Control+Alt", "Shift+Alt", "Control+Shift+Alt" };
            foreach (var m in modifiers)
                cmb.Items.Add(m);
            cmb.SelectedItem = currentModifiers;
        }

        private void CmbShortcutKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb || cmb.Tag is not string cmd) return;
            var selected = cmb.SelectedItem as string;
            if (selected == null) return;

            var sc = KeyboardShortcutManager.GetShortcut(cmd);
            sc.Key = selected;
            KeyboardShortcutManager.SetShortcut(cmd, selected, sc.Modifiers);

            // Anzeige aktualisieren
            foreach (var vm in _shortcutViewModels)
            {
                if (vm.CommandName == cmd)
                {
                    vm.Key = selected;
                    vm.DisplayText = sc.DisplayText;
                    break;
                }
            }
            LstShortcuts.Items.Refresh();
        }

        private void CmbShortcutModifiers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb || cmb.Tag is not string cmd) return;
            var selected = cmb.SelectedItem as string;
            if (selected == null) return;

            var sc = KeyboardShortcutManager.GetShortcut(cmd);
            sc.Modifiers = selected;
            KeyboardShortcutManager.SetShortcut(cmd, sc.Key, selected);

            // Anzeige aktualisieren
            foreach (var vm in _shortcutViewModels)
            {
                if (vm.CommandName == cmd)
                {
                    vm.Modifiers = selected;
                    vm.DisplayText = sc.DisplayText;
                    break;
                }
            }
            LstShortcuts.Items.Refresh();
        }

        private void CmbShortcutKey_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox cmb || cmb.Tag is not string cmd) return;
            FillKeyComboBox(cmb, KeyboardShortcutManager.GetShortcut(cmd).Key);
        }

        private void CmbShortcutModifiers_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox cmb || cmb.Tag is not string cmd) return;
            FillModifierComboBox(cmb, KeyboardShortcutManager.GetShortcut(cmd).Modifiers);
        }

        #region Tastaturkürzel

        private void RegisterKeyboardShortcuts()
        {
            // Strg+S: Übernehmen
            var applyCmd = new System.Windows.Input.RoutedCommand();
            applyCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.Control));
            this.CommandBindings.Add(new System.Windows.Input.CommandBinding(applyCmd, (s, e) => BtnApply_Click(s, e)));

            // Esc: Abbrechen
            var cancelCmd = new System.Windows.Input.RoutedCommand();
            cancelCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Escape));
            this.CommandBindings.Add(new System.Windows.Input.CommandBinding(cancelCmd, (s, e) => BtnCancel_Click(s, e)));

            // F1: Über-Dialog
            var aboutCmd = new System.Windows.Input.RoutedCommand();
            aboutCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(System.Windows.Input.Key.F1));
            this.CommandBindings.Add(new System.Windows.Input.CommandBinding(aboutCmd, (s, e) => ShowAboutDialog(s, e)));
        }

        private void ShowAboutDialog(object sender, System.Windows.RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }

        #region Sprachumschaltung

        private void InitLanguageCombo()
        {
            CmbLanguage.Items.Clear();
            foreach (var langCode in LanguageService.GetAvailableLanguages())
            {
                var oldLang = LanguageService.CurrentLanguageCode;
                LanguageService.LoadLanguage(langCode);
                var displayName = LanguageService.GetString("language");
                LanguageService.LoadLanguage(oldLang);

                CmbLanguage.Items.Add(new { Code = langCode, Name = displayName });
            }
            CmbLanguage.DisplayMemberPath = "Name";
            CmbLanguage.SelectedValuePath = "Code";

            var current = LanguageService.CurrentLanguageCode;
            foreach (var item in CmbLanguage.Items)
            {
                var code = item.GetType().GetProperty("Code")?.GetValue(item) as string;
                if (code == current)
                {
                    CmbLanguage.SelectedItem = item;
                    break;
                }
            }

            // Event-Handler für Sprachwechsel registrieren
            LanguageService.LanguageChanged += OnLanguageChanged;
            UpdateUiTexts();
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateUiTexts();
        }

        private void UpdateUiTexts()
        {
            // Tab-Header aktualisieren (indexbasiert, sprachunabhängig)
            var tabs = FindVisualChildren<TabItem>(this).ToList();
            var tabKeys = new[] { "tab.general", "tab.scaling", "tab.shading", "tab.categories", "tab.animation" };
            for (int i = 0; i < Math.Min(tabs.Count, tabKeys.Length); i++)
            {
                if (tabs[i].Header is string)
                {
                    tabs[i].Header = GetTabIcon(i) + " " + LanguageService.GetString(tabKeys[i]);
                }
            }
        }

        private static string GetTabIcon(int index) => index switch
        {
            0 => "⚙",
            1 => "🔍",
            2 => "✨",
            3 => "🏷",
            4 => "🎬",
            _ => ""
        };

        private void CmbLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbLanguage.SelectedItem == null) return;

            var code = CmbLanguage.SelectedItem.GetType().GetProperty("Code")?.GetValue(CmbLanguage.SelectedItem) as string;
            if (string.IsNullOrEmpty(code)) return;

            LanguageService.LoadLanguage(code);
            UpdateTooltips();
        }

        #endregion

        #region Visual Tree Helpers

        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        #endregion

        #endregion

        #region UI -> ProcessingOptions Mapping

        public ProcessingOptions BuildOptions()
        {
            bool ssaaEnabled = ChkSsaa?.IsChecked == true;

            var ssaaFilter = CmbSsaaFilter?.SelectedIndex switch
            {
                1 => SuperSampler.DownscaleFilter.Bicubic,
                2 => SuperSampler.DownscaleFilter.Bilinear,
                _ => SuperSampler.DownscaleFilter.Lanczos3
            };
            var ssaaSize = CmbSsaaSize?.SelectedIndex == 1
                ? SuperSampler.OutputSize.Original
                : SuperSampler.OutputSize.TwoX;

            string selectedWaifuModel = CmbWaifu2xModel?.SelectedItem?.ToString() ?? "models-cunet";

            int waifuNoise = CmbWaifu2xNoise?.SelectedIndex switch
            {
                0 => -1,
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 3,
                _ => 0
            };

            string selectedCuganModel = CmbRealCuganModel?.SelectedIndex switch
            {
                1 => "models-pro",
                2 => "models-nose",
                _ => "models-se"
            };

            int cuganDenoise = CmbRealCuganDenoise?.SelectedIndex switch
            {
                0 => -1,
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 3,
                _ => -1
            };

            return new ProcessingOptions
            {
                InputFolder = TxtInputPath?.Text ?? "",
                OutputFolder = TxtOutputPath?.Text ?? "",
                ScalePasses = CmbScale?.SelectedIndex == 1 ? 2 : 1,
                ScaleAlgorithm = CmbScale?.SelectedIndex switch
                {
                    1 => ScaleAlgorithm.Scale2x,
                    2 => ScaleAlgorithm.XbrzX2,
                    3 => ScaleAlgorithm.XbrzX4,
                    4 => ScaleAlgorithm.EsrganExternal,
                    5 => ScaleAlgorithm.Waifu2xAnime,
                    6 => ScaleAlgorithm.Waifu4xAnime,
                    7 => ScaleAlgorithm.RealCugan2x,
                    8 => ScaleAlgorithm.RealCugan4x,
                    9 => ScaleAlgorithm.AiFirst_HighResShading,
                    _ => ScaleAlgorithm.Scale2x
                },
                Esrgan = new EsrganOptions
                {
                    ExecutablePath = TxtEsrganPath?.Text?.Trim() ?? "",
                    ModelName = (TxtEsrganModel?.Text?.Trim() is string em && !string.IsNullOrWhiteSpace(em)) ? em : "realesrgan-x4plus-anime",
                    ScaleFactor = 4
                },
                Waifu2x = new Waifu2xOptions
                {
                    ExecutablePath = TxtWaifu2xPath?.Text?.Trim() ?? "",
                    ModelPath = selectedWaifuModel,
                    NoiseLevel = waifuNoise,
                    ScaleFactor = CmbScale?.SelectedIndex == 6 ? 4 : 2
                },
                RealCugan = new RealCuganOptions
                {
                    ExecutablePath = TxtRealCuganPath?.Text?.Trim() ?? "",
                    ModelPath = selectedCuganModel,
                    DenoiseLevel = cuganDenoise,
                    ScaleFactor = CmbScale?.SelectedIndex == 8 ? 4 : 2
                },
                AiFirst = new AiFirstOptions
                {
                    PrimaryModel = CmbAiModel?.SelectedIndex switch
                    {
                        1 => AiFirstModel.Waifu2x,
                        2 => AiFirstModel.RealCugan,
                        3 => AiFirstModel.Xbrz,
                        _ => AiFirstModel.Auto
                    },
                    FallbackModel = CmbAiFallbackModel?.SelectedIndex switch
                    {
                        1 => AiFirstModel.Waifu2x,
                        2 => AiFirstModel.RealCugan,
                        _ => AiFirstModel.Xbrz
                    },
                    ProgressiveMode = ChkProgressiveMode?.IsChecked == true,
                    TargetWidth = int.TryParse(TxtTargetWidth?.Text?.Trim(), out var tw) ? tw : 0,
                    TargetHeight = int.TryParse(TxtTargetHeight?.Text?.Trim(), out var th) ? th : 0,
                    Waifu2xModel = CmbWaifuModelVariant?.SelectedIndex switch
                    {
                        1 => Waifu2xModelVariant.Photo,
                        2 => Waifu2xModelVariant.General,
                        _ => Waifu2xModelVariant.Anime
                    },
                    RealCuganDenoise = CmbRealCuganDenoiseLevel?.SelectedIndex switch
                    {
                        1 => 1,
                        2 => 2,
                        3 => 3,
                        _ => 0
                    },
                    EnableAiLogging = ChkAiLogging?.IsChecked == true,
                    EnableSuperSampler = ChkAiSuperSampler?.IsChecked == true
                },
                IncludeBmp = ChkFilterBmp?.IsChecked == true,
                IncludePng = ChkFilterPng?.IsChecked == true,
                IncludeTga = ChkFilterTga?.IsChecked == true,
                IncludeSpr = ChkFilterSpr?.IsChecked == true,
                ScaleActAnchors = ChkScaleActAnchors?.IsChecked == true,
                Subfolders = ChkSubfolders?.IsChecked == true,
                HandleMagentaKey = ChkMagenta?.IsChecked == true,
                AlphaThreshold = (int)(SldrAlphaThreshold?.Value ?? 128),
                OutputFormat = CmbOutputFormat?.SelectedIndex switch
                {
                    1 => OutputFormat.Bmp_MagentaKey,
                    2 => OutputFormat.Bmp_Indexed8_Palette,
                    3 => OutputFormat.Tga_Uncompressed,
                    _ => OutputFormat.Png
                },
                Enhancer = new EnhancerOptions
                {
                    Outline = ChkOutline?.IsChecked == true,
                    OutlineStrength = (float)(SldrOutlineStrength?.Value ?? 0.5),
                    OutlineMode = CmbOutlineMode?.SelectedIndex == 1 ? OutlineMode.InternalOnly : OutlineMode.Full,
                    Shadow = ChkShadow?.IsChecked == true,
                    ShadowStrength = (float)(SldrShadowStrength?.Value ?? 1.0),
                    Highlight = ChkHighlight?.IsChecked == true,
                    HighlightStrength = (float)(SldrHighlightStrength?.Value ?? 1.0),
                    Sharpen = ChkSharpen?.IsChecked == true,
                    SharpenStrength = (float)(SldrSharpenStrength?.Value ?? 0.3),
                    Volumetric3D = (FindName("ChkVolumetric3D") is CheckBox cb3d && cb3d.IsChecked == true),
                    Volumetric3DStrength = (float)(SldrVolumetric3DStrength?.Value ?? 0.75f),
                    HueShift = (float)(SldrHueShift?.Value ?? 0.0),
                    SaturationScale = (float)(SldrSaturation?.Value ?? 1.0),
                    LightnessScale = (float)(SldrLightness?.Value ?? 1.0),
                    ExclusionColor = ChkExclusionColor?.IsChecked == true ? ParseExclusionColor(TxtExclusionColor?.Text) : null
                },
                IncrementalMode = ChkIncrementalMode?.IsChecked == true,
                SuperSampler = new SuperSamplerOptions
                {
                    Enabled = ssaaEnabled,
                    Filter = ssaaFilter,
                    TargetSize = ssaaSize
                },
                CategoryOverrides = BuildCategoryOverrides()
            };
        }

        #endregion

        #region ProcessingOptions -> UI Mapping

        private void ApplyOptionsToUi(ProcessingOptions opts)
        {
            // Skalierung
            CmbScale.SelectedIndex = (opts.ScaleAlgorithm, opts.ScalePasses) switch
            {
                (ScaleAlgorithm.Scale2x, 2) => 1,
                (ScaleAlgorithm.Scale2x, _) => 0,
                (ScaleAlgorithm.XbrzX2, _) => 2,
                (ScaleAlgorithm.XbrzX4, _) => 3,
                (ScaleAlgorithm.EsrganExternal, _) => 4,
                (ScaleAlgorithm.Waifu2xAnime, _) => 5,
                (ScaleAlgorithm.Waifu4xAnime, _) => 6,
                (ScaleAlgorithm.RealCugan2x, _) => 7,
                (ScaleAlgorithm.RealCugan4x, _) => 8,
                (ScaleAlgorithm.AiFirst_HighResShading, _) => 9,
                _ => 0
            };

            // Waifu2x
            TxtWaifu2xPath.Text = opts.Waifu2x.ExecutablePath;
            CmbWaifu2xNoise.SelectedIndex = opts.Waifu2x.NoiseLevel switch
            {
                -1 => 0,
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 4,
                _ => 0
            };

            // RealCUGAN
            TxtRealCuganPath.Text = opts.RealCugan.ExecutablePath;
            CmbRealCuganModel.SelectedIndex = opts.RealCugan.ModelPath switch
            {
                "models-pro" => 1,
                "models-nose" => 2,
                _ => 0
            };
            CmbRealCuganDenoise.SelectedIndex = opts.RealCugan.DenoiseLevel switch
            {
                -1 => 0,
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 4,
                _ => 0
            };

            // ESRGAN
            TxtEsrganPath.Text = opts.Esrgan.ExecutablePath;
            TxtEsrganModel.Text = opts.Esrgan.ModelName;

            // AI-First
            ChkProgressiveMode.IsChecked = opts.AiFirst.ProgressiveMode;
            CmbAiModel.SelectedIndex = opts.AiFirst.PrimaryModel switch
            {
                AiFirstModel.Waifu2x => 1,
                AiFirstModel.RealCugan => 2,
                AiFirstModel.Xbrz => 3,
                _ => 0
            };
            CmbAiFallbackModel.SelectedIndex = opts.AiFirst.FallbackModel switch
            {
                AiFirstModel.Waifu2x => 1,
                AiFirstModel.RealCugan => 2,
                _ => 0
            };
            CmbWaifuModelVariant.SelectedIndex = opts.AiFirst.Waifu2xModel switch
            {
                Waifu2xModelVariant.Photo => 1,
                Waifu2xModelVariant.General => 2,
                _ => 0
            };
            CmbRealCuganDenoiseLevel.SelectedIndex = opts.AiFirst.RealCuganDenoise switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                _ => 0
            };
            TxtTargetWidth.Text = opts.AiFirst.TargetWidth.ToString();
            TxtTargetHeight.Text = opts.AiFirst.TargetHeight.ToString();
            ChkAiLogging.IsChecked = opts.AiFirst.EnableAiLogging;
            ChkAiSuperSampler.IsChecked = opts.AiFirst.EnableSuperSampler;

            // Pixel-Shading
            ChkOutline.IsChecked = opts.Enhancer.Outline;
            SldrOutlineStrength.Value = opts.Enhancer.OutlineStrength;
            CmbOutlineMode.SelectedIndex = opts.Enhancer.OutlineMode == OutlineMode.InternalOnly ? 1 : 0;
            ChkShadow.IsChecked = opts.Enhancer.Shadow;
            SldrShadowStrength.Value = opts.Enhancer.ShadowStrength;
            ChkHighlight.IsChecked = opts.Enhancer.Highlight;
            SldrHighlightStrength.Value = opts.Enhancer.HighlightStrength;
            ChkSharpen.IsChecked = opts.Enhancer.Sharpen;
            SldrSharpenStrength.Value = opts.Enhancer.SharpenStrength;
            ChkVolumetric3D.IsChecked = opts.Enhancer.Volumetric3D;
            SldrVolumetric3DStrength.Value = opts.Enhancer.Volumetric3DStrength;
            SldrHueShift.Value = opts.Enhancer.HueShift;
            SldrSaturation.Value = opts.Enhancer.SaturationScale;
            SldrLightness.Value = opts.Enhancer.LightnessScale;

            // Ausschluss-Farbe
            ChkExclusionColor.IsChecked = opts.Enhancer.ExclusionColor.HasValue;
            TxtExclusionColor.Text = opts.Enhancer.ExclusionColor.HasValue
                ? $"0x{opts.Enhancer.ExclusionColor.Value:X8}"
                : "FF00FFFF";

            // Inkrementell
            ChkIncrementalMode.IsChecked = opts.IncrementalMode;

            // Dateitypen
            ChkFilterBmp.IsChecked = opts.IncludeBmp;
            ChkFilterPng.IsChecked = opts.IncludePng;
            ChkFilterTga.IsChecked = opts.IncludeTga;
            ChkFilterSpr.IsChecked = opts.IncludeSpr;
            ChkSubfolders.IsChecked = opts.Subfolders;

            // Ausgabeformat
            CmbOutputFormat.SelectedIndex = opts.OutputFormat switch
            {
                OutputFormat.Bmp_MagentaKey => 1,
                OutputFormat.Bmp_Indexed8_Palette => 2,
                OutputFormat.Tga_Uncompressed => 3,
                _ => 0
            };

            // Magenta
            ChkMagenta.IsChecked = opts.HandleMagentaKey;
            SldrAlphaThreshold.Value = opts.AlphaThreshold;

            // ACT-Anker
            ChkScaleActAnchors.IsChecked = opts.ScaleActAnchors;

            // SSAA
            ChkSsaa.IsChecked = opts.SuperSampler.Enabled;
            SsaaOptions.Visibility = opts.SuperSampler.Enabled ? Visibility.Visible : Visibility.Collapsed;
            CmbSsaaFilter.SelectedIndex = opts.SuperSampler.Filter switch
            {
                SuperSampler.DownscaleFilter.Bicubic => 1,
                SuperSampler.DownscaleFilter.Bilinear => 2,
                _ => 0
            };
            CmbSsaaSize.SelectedIndex = opts.SuperSampler.TargetSize == SuperSampler.OutputSize.Original ? 1 : 0;

            // Kategorie-Overrides
            ChkCategoryOverridesEnabled.IsChecked = opts.CategoryOverrides != null && opts.CategoryOverrides.Count > 0;
        }

        #endregion

        #region Presets

        private void RefreshPresetList(string? selectAfter = null)
        {
            var names = PresetManager.ListNames();
            CmbPresets.ItemsSource = names;
            if (selectAfter != null && names.Contains(selectAfter))
                CmbPresets.SelectedItem = selectAfter;
        }

        private void CmbPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (CmbPresets.SelectedItem is not string name) return;

            var preset = PresetManager.Load(name);
            if (preset == null) return;

            ApplyPreset(preset);
            TxtPresetName.Text = name;
        }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtPresetName.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name)) return;

            var preset = CaptureCurrentPreset(name);
            PresetManager.Save(preset);
            RefreshPresetList(name);
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPresets.SelectedItem is not string name) return;
            PresetManager.Delete(name);
            RefreshPresetList();
        }

        private void BtnExportPreset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPresets.SelectedItem is not string name) return;
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Preset exportieren",
                Filter = "RoSpriteEnhancer Preset (*.json)|*.json",
                FileName = name + ".json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dialog.ShowDialog() == true)
            {
                if (PresetManager.Export(name, dialog.FileName))
                    MessageBox.Show($"Preset '{name}' exportiert.", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show($"Fehler beim Exportieren von '{name}'.", "Export fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportPreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Preset importieren",
                Filter = "RoSpriteEnhancer Preset (*.json)|*.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dialog.ShowDialog() == true)
            {
                var importedName = PresetManager.Import(dialog.FileName);
                if (importedName != null)
                {
                    RefreshPresetList(importedName);
                    MessageBox.Show($"Preset '{importedName}' importiert.", "Import erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Fehler beim Importieren des Presets.", "Import fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnCloudUploadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPresets.SelectedItem is not string name) return;

            var preset = PresetManager.Load(name);
            if (preset == null) return;

            var json = System.Text.Json.JsonSerializer.Serialize(preset, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            var result = await PresetSharingService.UploadPresetAsync(name, json, $"Preset '{name}' geteilt über RoSpriteEnhancer");

            if (result.Success)
            {
                MessageBox.Show($"Preset '{name}' erfolgreich in die Cloud hochgeladen!\\nID: {result.PresetId}\\nDownload-URL: {result.DownloadUrl}", "Cloud-Upload erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Cloud-Upload fehlgeschlagen:\\n{result.ErrorMessage}", "Upload-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCloudDownloadPreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Preset aus Cloud herunterladen", "Preset-ID eingeben:");
            dialog.Owner = this;
            if (dialog.ShowDialog() != true) return;
            var presetId = dialog.Result;

            if (string.IsNullOrWhiteSpace(presetId)) return;

            var (presetJson, error) = await PresetSharingService.DownloadPresetAsync(presetId);

            if (error != null)
            {
                MessageBox.Show($"Download fehlgeschlagen:\\n{error}", "Download-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (presetJson == null)
            {
                MessageBox.Show("Preset nicht gefunden oder leer.", "Download-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var presetsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer", "Presets");

            if (PresetSharingService.SaveDownloadedPreset(presetJson, $"Cloud_{presetId}", presetsDir))
            {
                RefreshPresetList();
                MessageBox.Show($"Preset '{presetId}' erfolgreich heruntergeladen und lokal gespeichert.", "Download erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Fehler beim Speichern des heruntergeladenen Presets.", "Speicher-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private EnhancementPreset CaptureCurrentPreset(string name)
        {
            var opts = BuildOptions();
            return new EnhancementPreset
            {
                Name = name,
                ScaleAlgorithm = opts.ScaleAlgorithm,
                ScalePasses = opts.ScalePasses,
                Outline = opts.Enhancer.Outline,
                OutlineStrength = opts.Enhancer.OutlineStrength,
                Shadow = opts.Enhancer.Shadow,
                ShadowStrength = opts.Enhancer.ShadowStrength,
                Highlight = opts.Enhancer.Highlight,
                HighlightStrength = opts.Enhancer.HighlightStrength,
                Sharpen = opts.Enhancer.Sharpen,
                SharpenStrength = opts.Enhancer.SharpenStrength,
                Volumetric3D = opts.Enhancer.Volumetric3D,
                Volumetric3DStrength = opts.Enhancer.Volumetric3DStrength,
                AiFirst = opts.AiFirst,
                HandleMagentaKey = opts.HandleMagentaKey,
                AlphaThreshold = opts.AlphaThreshold,
                OutputFormat = opts.OutputFormat,
                IncludeBmp = opts.IncludeBmp,
                IncludePng = opts.IncludePng,
                IncludeTga = opts.IncludeTga,
                IncludeSpr = opts.IncludeSpr,
                Subfolders = opts.Subfolders,
                ScaleActAnchors = opts.ScaleActAnchors,
                SuperSamplerEnabled = opts.SuperSampler.Enabled,
                SuperSamplerFilter = opts.SuperSampler.Filter,
                SuperSamplerTargetSize = opts.SuperSampler.TargetSize
            };
        }

        private void ApplyPreset(EnhancementPreset p)
        {
            CmbScale.SelectedIndex = (p.ScaleAlgorithm, p.ScalePasses) switch
            {
                (ScaleAlgorithm.Scale2x, 2) => 1,
                (ScaleAlgorithm.Scale2x, _) => 0,
                (ScaleAlgorithm.XbrzX2, _) => 2,
                (ScaleAlgorithm.XbrzX4, _) => 3,
                (ScaleAlgorithm.EsrganExternal, _) => 4,
                (ScaleAlgorithm.Waifu2xAnime, _) => 5,
                (ScaleAlgorithm.Waifu4xAnime, _) => 6,
                (ScaleAlgorithm.RealCugan2x, _) => 7,
                (ScaleAlgorithm.RealCugan4x, _) => 8,
                (ScaleAlgorithm.AiFirst_HighResShading, _) => 9,
                _ => 0
            };

            ChkOutline.IsChecked = p.Outline;
            SldrOutlineStrength.Value = p.OutlineStrength;
            ChkShadow.IsChecked = p.Shadow;
            SldrShadowStrength.Value = p.ShadowStrength;
            ChkHighlight.IsChecked = p.Highlight;
            SldrHighlightStrength.Value = p.HighlightStrength;
            ChkSharpen.IsChecked = p.Sharpen;
            SldrSharpenStrength.Value = p.SharpenStrength;
            ChkVolumetric3D.IsChecked = p.Volumetric3D;
            SldrVolumetric3DStrength.Value = p.Volumetric3DStrength;

            // AI-First
            if (p.AiFirst != null)
            {
                ChkProgressiveMode.IsChecked = p.AiFirst.ProgressiveMode;
                CmbAiModel.SelectedIndex = p.AiFirst.PrimaryModel switch
                {
                    AiFirstModel.Waifu2x => 1,
                    AiFirstModel.RealCugan => 2,
                    AiFirstModel.Xbrz => 3,
                    _ => 0
                };
                CmbAiFallbackModel.SelectedIndex = p.AiFirst.FallbackModel switch
                {
                    AiFirstModel.Waifu2x => 1,
                    AiFirstModel.RealCugan => 2,
                    _ => 0
                };
                CmbWaifuModelVariant.SelectedIndex = p.AiFirst.Waifu2xModel switch
                {
                    Waifu2xModelVariant.Photo => 1,
                    Waifu2xModelVariant.General => 2,
                    _ => 0
                };
                CmbRealCuganDenoiseLevel.SelectedIndex = p.AiFirst.RealCuganDenoise switch
                {
                    1 => 1,
                    2 => 2,
                    3 => 3,
                    _ => 0
                };
                TxtTargetWidth.Text = p.AiFirst.TargetWidth.ToString();
                TxtTargetHeight.Text = p.AiFirst.TargetHeight.ToString();
                ChkAiLogging.IsChecked = p.AiFirst.EnableAiLogging;
                ChkAiSuperSampler.IsChecked = p.AiFirst.EnableSuperSampler;
            }

            ChkMagenta.IsChecked = p.HandleMagentaKey;
            SldrAlphaThreshold.Value = p.AlphaThreshold;

            CmbOutputFormat.SelectedIndex = p.OutputFormat switch
            {
                OutputFormat.Bmp_MagentaKey => 1,
                OutputFormat.Bmp_Indexed8_Palette => 2,
                OutputFormat.Tga_Uncompressed => 3,
                _ => 0
            };

            ChkFilterBmp.IsChecked = p.IncludeBmp;
            ChkFilterPng.IsChecked = p.IncludePng;
            ChkFilterTga.IsChecked = p.IncludeTga;
            ChkFilterSpr.IsChecked = p.IncludeSpr;
            ChkSubfolders.IsChecked = p.Subfolders;
            ChkScaleActAnchors.IsChecked = p.ScaleActAnchors;

            ChkSsaa.IsChecked = p.SuperSamplerEnabled;
            SsaaOptions.Visibility = p.SuperSamplerEnabled ? Visibility.Visible : Visibility.Collapsed;
            CmbSsaaFilter.SelectedIndex = p.SuperSamplerFilter switch
            {
                SuperSampler.DownscaleFilter.Bicubic => 1,
                SuperSampler.DownscaleFilter.Bilinear => 2,
                _ => 0
            };
            CmbSsaaSize.SelectedIndex = p.SuperSamplerTargetSize == SuperSampler.OutputSize.Original ? 1 : 0;

            Option_Changed(this, new RoutedEventArgs());
        }

        #endregion

        #region Skalierung & KI Event-Handler

        private async void CmbScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            bool isWaifu = CmbScale.SelectedIndex == 5 || CmbScale.SelectedIndex == 6;
            bool isRealCugan = CmbScale.SelectedIndex == 7 || CmbScale.SelectedIndex == 8;
            bool isAiFirst = CmbScale.SelectedIndex == 9;
            bool isRealESRGAN = CmbScale.SelectedIndex == 10;
            bool isGFPGAN = CmbScale.SelectedIndex == 11;

            // ── Automatischer Download wenn Tool noch nicht installiert ──
            if (isWaifu || isAiFirst)
            {
                var tool = AiToolManager.GetTool("waifu2x")!;
                if (!AiToolManager.IsToolInstalled(tool))
                {
                    TxtScaleHint.Text = $"⬇️ Lade {tool.DisplayName}...";
                    var progress = new Progress<double>(p =>
                    {
                        TxtScaleHint.Text = $"⬇️ {tool.DisplayName}: {p:F0}%";
                    });
                    await AiToolManager.EnsureInstalledAsync(tool, progress);
                    TxtWaifu2xPath.Text = AiToolManager.FindExecutable("waifu2x", "waifu2x-ncnn-vulkan.exe");
                }
            }
            else if (isRealCugan)
            {
                var tool = AiToolManager.GetTool("realcugan")!;
                if (!AiToolManager.IsToolInstalled(tool))
                {
                    TxtScaleHint.Text = $"⬇️ Lade {tool.DisplayName}...";
                    var progress = new Progress<double>(p =>
                    {
                        TxtScaleHint.Text = $"⬇️ {tool.DisplayName}: {p:F0}%";
                    });
                    await AiToolManager.EnsureInstalledAsync(tool, progress);
                    TxtRealCuganPath.Text = AiToolManager.FindExecutable("realcugan", "realcugan-ncnn-vulkan.exe");
                }
            }
            else if (isRealESRGAN)
            {
                var tool = AiToolManager.GetTool("realesrgan")!;
                if (!AiToolManager.IsToolInstalled(tool))
                {
                    TxtScaleHint.Text = $"⬇️ Lade {tool.DisplayName}...";
                    var progress = new Progress<double>(p =>
                    {
                        TxtScaleHint.Text = $"⬇️ {tool.DisplayName}: {p:F0}%";
                    });
                    await AiToolManager.EnsureInstalledAsync(tool, progress);
                    TxtRealESRGANPath.Text = AiToolManager.FindExecutable("realesrgan", "realesrgan-ncnn-vulkan.exe");
                }
            }
            else if (isGFPGAN)
            {
                var tool = AiToolManager.GetTool("gfpgan")!;
                if (!AiToolManager.IsToolInstalled(tool))
                {
                    TxtScaleHint.Text = $"⬇️ Lade {tool.DisplayName}...";
                    var progress = new Progress<double>(p =>
                    {
                        TxtScaleHint.Text = $"⬇️ {tool.DisplayName}: {p:F0}%";
                    });
                    await AiToolManager.EnsureInstalledAsync(tool, progress);
                    TxtGFGANPath.Text = AiToolManager.FindExecutable("gfpgan", "gfpgan-ncnn-vulkan.exe");
                }
            }

            bool waifuMissing = (isWaifu || isAiFirst) &&
                (string.IsNullOrWhiteSpace(TxtWaifu2xPath?.Text) || !File.Exists(TxtWaifu2xPath?.Text));
            bool cuganMissing = isRealCugan &&
                (string.IsNullOrWhiteSpace(TxtRealCuganPath?.Text) || !File.Exists(TxtRealCuganPath?.Text));
            bool realesrganMissing = isRealESRGAN &&
                (string.IsNullOrWhiteSpace(TxtRealESRGANPath?.Text) || !File.Exists(TxtRealESRGANPath?.Text));
            bool gfpganMissing = isGFPGAN &&
                (string.IsNullOrWhiteSpace(TxtGFGANPath?.Text) || !File.Exists(TxtGFGANPath?.Text));

            if (TxtScaleHint != null)
            {
                if (waifuMissing)
                {
                    TxtScaleHint.Text = "⚠ Waifu2x-Exe wird geladen...";
                    TxtScaleHint.Foreground = System.Windows.Media.Brushes.Gold;
                }
                else if (cuganMissing)
                {
                    TxtScaleHint.Text = "⚠ RealCUGAN-Exe wird geladen...";
                    TxtScaleHint.Foreground = System.Windows.Media.Brushes.Gold;
                }
                else if (realesrganMissing)
                {
                    TxtScaleHint.Text = "⚠ RealESRGAN-Exe wird geladen...";
                    TxtScaleHint.Foreground = System.Windows.Media.Brushes.Gold;
                }
                else if (gfpganMissing)
                {
                    TxtScaleHint.Text = "⚠ GFPGAN-Exe wird geladen...";
                    TxtScaleHint.Foreground = System.Windows.Media.Brushes.Gold;
                }
                else
                {
                    TxtScaleHint.Text = CmbScale.SelectedIndex switch
                    {
                        0 => "Scale2x — schnell, ideal für alte Hardware",
                        1 => "Scale4x — 4x RAM, sehr langsam",
                        2 => "xBRZ 2x — glattere Diagonalen als Scale2x",
                        3 => "xBRZ 4x — höchste Qualität, 4x langsamer",
                        4 => "ESRGAN — externes KI-Tool, weicherer Look",
                        5 => "Waifu2x — 2x Anime- & Sprite-Look (Aktiv)",
                        6 => "Waifu4x — 4x Ultra-HD Anime Look (Aktiv)",
                        7 => "RealCUGAN — 2x Profi-Anime-Remastering (Aktiv)",
                        8 => "RealCUGAN — 4x Profi-Anime-Remastering (Aktiv)",
                        9 => "AI-First + High-Res Shading — BESTE 3D-Optik (AI 4x → High-Res Volumetric/Shading)",
                        10 => "RealESRGAN — Beste Anime-Qualität mit x4plus-Modell (Aktiv)",
                        11 => "GFPGAN — Gesicht & Haar-Restaurierung für Details (Aktiv)",
                        _ => ""
                    };
                    TxtScaleHint.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55));
                }
            }

            PanelEsrganSettings.Visibility = CmbScale.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
            PanelWaifu2xSettings.Visibility = (isWaifu || isAiFirst) ? Visibility.Visible : Visibility.Collapsed;
            PanelRealCuganSettings.Visibility = isRealCugan ? Visibility.Visible : Visibility.Collapsed;
            PanelAiFirstSettings.Visibility = isAiFirst ? Visibility.Visible : Visibility.Collapsed;
            PanelEsrganSettings.Visibility = isRealESRGAN ? Visibility.Visible : Visibility.Collapsed;
            PanelGFPGANSettings.Visibility = isGFPGAN ? Visibility.Visible : Visibility.Collapsed;

            Option_Changed(sender, e);
        }



        private void RefreshWaifu2xModels()
        {
            string exePath = TxtWaifu2xPath?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) return;

            string exeDir = Path.GetDirectoryName(exePath) ?? "";
            var foundModels = new List<string>();

            string modelsSub = Path.Combine(exeDir, "models");
            string searchBase = Directory.Exists(modelsSub) ? modelsSub : exeDir;

            if (Directory.Exists(searchBase))
            {
                foreach (var dir in Directory.GetDirectories(searchBase, "models-*"))
                    foundModels.Add(Path.GetFileName(dir));
            }

            if (foundModels.Count > 0)
            {
                CmbWaifu2xModel.ItemsSource = foundModels;
                CmbWaifu2xModel.SelectedIndex = 0;
            }
        }

        private void CmbOutputFormat_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || TxtFormatHint == null) return;
            TxtFormatHint.Text = CmbOutputFormat.SelectedIndex switch
            {
                0 => RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.formatPng"),
                1 => RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.formatBmpMagenta"),
                2 => RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.formatBmpPalette"),
                3 => RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.formatTga"),
                _ => ""
            };
            Option_Changed(sender, e);
        }

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            // Änderungen werden beim OK-Button übernommen, aber Event für Live-Vorschau feuern
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ChkSsaa_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = ChkSsaa?.IsChecked == true;
            if (SsaaOptions != null) SsaaOptions.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            Option_Changed(sender, e);
        }

        #endregion

        #region Ordner & Dateitypen

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            Option_Changed(sender, e);
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            string? folder = BrowseFolder("Eingabe-Ordner wählen (extrahierte Sprites)");
            if (folder == null) return;
            TxtInputPath.Text = folder;
            // Recent-Liste aktualisieren
            var (inputs, outputs) = WindowStateService.LoadLastFoldersList();
            if (!inputs.Contains(folder))
            {
                inputs.Insert(0, folder);
                if (inputs.Count > 5) inputs.RemoveRange(5, inputs.Count - 5);
            }
            CmbRecentInputs.ItemsSource = null;
            CmbRecentInputs.ItemsSource = inputs;
        }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            string? folder = BrowseFolder("Ausgabe-Ordner wählen");
            if (folder != null)
            {
                TxtOutputPath.Text = folder;
                // Recent-Liste aktualisieren
                var (inputs, outputs) = WindowStateService.LoadLastFoldersList();
                if (!outputs.Contains(folder))
                {
                    outputs.Insert(0, folder);
                    if (outputs.Count > 5) outputs.RemoveRange(5, outputs.Count - 5);
                }
                CmbRecentOutputs.ItemsSource = null;
                CmbRecentOutputs.ItemsSource = outputs;
            }
        }

        private void CmbRecentInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRecentInputs.SelectedItem is string folder && Directory.Exists(folder))
                TxtInputPath.Text = folder;
        }

        private void CmbRecentOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRecentOutputs.SelectedItem is string folder && Directory.Exists(folder))
                TxtOutputPath.Text = folder;
        }

        private void BtnClearRecentInputs_Click(object sender, RoutedEventArgs e)
        {
            CmbRecentInputs.ItemsSource = new List<string>();
            CmbRecentInputs.SelectedIndex = -1;
        }

        private void BtnClearRecentOutputs_Click(object sender, RoutedEventArgs e)
        {
            CmbRecentOutputs.ItemsSource = new List<string>();
            CmbRecentOutputs.SelectedIndex = -1;
        }

        private static string? BrowseFolder(string description)
        {
            var dialog = new OpenFolderDialog { Title = description };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        private void BtnBrowseEsrgan_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "ESRGAN-Tool auswählen",
                Filter = "Ausführbare Dateien (*.exe)|*.exe"
            };
            if (dialog.ShowDialog() == true) TxtEsrganPath.Text = dialog.FileName;
        }

        private void BtnBrowseWaifu2x_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Waifu2x Tool auswählen (waifu2x-ncnn-vulkan.exe)",
                Filter = "Ausführbare Dateien (*.exe)|*.exe"
            };
            if (dialog.ShowDialog() == true)
            {
                TxtWaifu2xPath.Text = dialog.FileName;
                RefreshWaifu2xModels();
            }
        }

        private void BtnBrowseRealCugan_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "RealCUGAN Tool auswählen (realcugan-ncnn-vulkan.exe)",
                Filter = "Ausführbare Dateien (*.exe)|*.exe"
            };
            if (dialog.ShowDialog() == true) TxtRealCuganPath.Text = dialog.FileName;
        }

        private void BtnBrowseRealESRGAN_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "RealESRGAN Tool auswählen (realesrgan-ncnn-vulkan.exe)",
                Filter = "Ausführbare Dateien (*.exe)|*.exe"
            };
            if (dialog.ShowDialog() == true) TxtRealESRGANPath.Text = dialog.FileName;
        }

        private void BtnBrowseGFPGAN_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "GFPGAN Tool auswählen (gfpgan-ncnn-vulkan.exe)",
                Filter = "Ausführbare Dateien (*.exe)|*.exe"
            };
            if (dialog.ShowDialog() == true) TxtGFGANPath.Text = dialog.FileName;
        }

        #endregion

        #region Kategorie-Overrides

        private void InitCategoryOverrides()
        {
            foreach (var cat in SpriteFilter.AllCategories)
                _categoryOverrideStates[cat.Id] = (false, new CategoryOverride());

            CmbCategoryPicker.ItemsSource = SpriteFilter.AllCategories;
            CmbCategoryPicker.SelectedIndex = 0;

            if (SpriteFilter.AllCategories.Length > 0)
            {
                _currentCategoryPickerId = SpriteFilter.AllCategories[0].Id;
                LoadCategoryPanelFromState(_currentCategoryPickerId);
            }
        }

        private void SaveCurrentCategoryPanelToState()
        {
            if (_currentCategoryPickerId == null) return;
            var ov = new CategoryOverride
            {
                Outline = ChkCatOutline.IsChecked == true,
                OutlineStrength = (float)SldrCatOutline.Value,
                Shadow = ChkCatShadow.IsChecked == true,
                ShadowStrength = (float)SldrCatShadow.Value,
                Highlight = ChkCatHighlight.IsChecked == true,
                HighlightStrength = (float)SldrCatHighlight.Value,
                Sharpen = ChkCatSharpen.IsChecked == true,
                SharpenStrength = (float)SldrCatSharpen.Value,
                Volumetric3D = ChkCatVolumetric3D.IsChecked == true,
                Volumetric3DStrength = (float)SldrCatVolumetric3D.Value,
            };
            bool enabled = ChkCategoryOverrideActive.IsChecked == true;
            _categoryOverrideStates[_currentCategoryPickerId] = (enabled, ov);
        }

        private void LoadCategoryPanelFromState(string categoryId)
        {
            if (!_categoryOverrideStates.TryGetValue(categoryId, out var state))
                state = (false, new CategoryOverride());

            ChkCategoryOverrideActive.IsChecked = state.Enabled;
            PanelCategorySliders.IsEnabled = state.Enabled;

            ChkCatOutline.IsChecked = state.Ov.Outline;
            SldrCatOutline.Value = state.Ov.OutlineStrength;
            ChkCatShadow.IsChecked = state.Ov.Shadow;
            SldrCatShadow.Value = state.Ov.ShadowStrength;
            ChkCatHighlight.IsChecked = state.Ov.Highlight;
            SldrCatHighlight.Value = state.Ov.HighlightStrength;
            ChkCatSharpen.IsChecked = state.Ov.Sharpen;
            SldrCatSharpen.Value = state.Ov.SharpenStrength;
            ChkCatVolumetric3D.IsChecked = state.Ov.Volumetric3D;
            SldrCatVolumetric3D.Value = state.Ov.Volumetric3DStrength;
        }

        private void CmbCategoryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            SaveCurrentCategoryPanelToState();

            if (CmbCategoryPicker.SelectedItem is not SpriteCategory cat) return;
            _currentCategoryPickerId = cat.Id;
            LoadCategoryPanelFromState(cat.Id);
        }

        private void ChkCategoryOverrideActive_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            PanelCategorySliders.IsEnabled = ChkCategoryOverrideActive.IsChecked == true;
            SaveCurrentCategoryPanelToState();
        }

        private void CategorySetting_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (sender is Slider s)
            {
                if (s.Name == "SldrCatOutline") TxtCatOutlineVal.Text = s.Value.ToString("F1");
                if (s.Name == "SldrCatShadow") TxtCatShadowVal.Text = s.Value.ToString("F1");
                if (s.Name == "SldrCatHighlight") TxtCatHighlightVal.Text = s.Value.ToString("F1");
                if (s.Name == "SldrCatSharpen") TxtCatSharpenVal.Text = s.Value.ToString("F1");
                if (s.Name == "SldrCatVolumetric3D") TxtCatVolumetricVal.Text = s.Value.ToString("F1");
            }
            SaveCurrentCategoryPanelToState();
        }

        private Dictionary<string, CategoryOverride>? BuildCategoryOverrides()
        {
            if (ChkCategoryOverridesEnabled?.IsChecked != true) return null;
            SaveCurrentCategoryPanelToState();

            var result = new Dictionary<string, CategoryOverride>();
            foreach (var kv in _categoryOverrideStates)
                if (kv.Value.Enabled)
                    result[kv.Key] = kv.Value.Ov;

            return result.Count > 0 ? result : null;
        }

        #endregion

        #region Hilfsmittel

        private static int? ParseExclusionColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            hex = hex.Trim().TrimStart('#');
            if (hex.Length != 8) return null;
            if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out int value))
                return null;
            return value;
        }

        #endregion

        #region Window-Buttons

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Options = BuildOptions();
            SaveGitHubToken();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            Options = BuildOptions();
            SaveGitHubToken();
            // Änderungen übernehmen, Fenster bleibt offen
        }

        #endregion
    }
}