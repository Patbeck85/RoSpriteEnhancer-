#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using RoSpriteEnhancer.Core.Processing;
using RoSpriteEnhancer.Core.IO;
using RoSpriteEnhancer.Core.Models;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer.Views.Windows
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<FileItem> _files = new();
        private readonly BatchProcessor _processor = new();
        private CancellationTokenSource? _cts;
        private bool _processingActive = false;
        private ProcessingOptions _currentOptions = new();

        private string? _lastPreviewFilePath;
        private string? _openGrfPath;
        private string? _grfExtractFolder;
        private string? _grfEnhancedFolder;
        private readonly List<SpriteCategory> _categories = SpriteFilter.AllCategories.ToList();

        // --- Undo/Redo für Vorschau-Einstellungen ---
        private readonly UndoRedoManager<ProcessingOptions> _previewUndoRedo = new(maxHistory: 50);

        private System.Diagnostics.Stopwatch _grfStopwatch = new();
        private DispatcherTimer _grfElapsedTimer = new();
        private double _grfLastSpeed = 0;

        private DispatcherTimer _previewDebounce = new() { Interval = TimeSpan.FromMilliseconds(200) };

        private double _zoomLevel = 1.0;
        private const double ZoomMin = 0.25;
        private const double ZoomMax = 8.0;
        private const double ZoomStep = 0.25;

        private SprFile? _currentSpr;
        private DispatcherTimer _animTimer = new();
        private int _currentFrame = 0;
        private bool _isAnimPlaying = false;

        private bool _gridViewActive = false;
        private bool _contactSheetMode = false;
        private readonly Dictionary<string, (bool Enabled, CategoryOverride Ov)> _categoryOverrideStates = new();

                public MainWindow()
                {
                    InitializeComponent();
                    if (WindowStateService.TryLoadWindowState("MainWindow", out double l, out double t, out double w, out double h, out string ws))
                    {
                        Left = l; Top = t; Width = w; Height = h;
                        WindowState = (WindowState)Enum.Parse(typeof(WindowState), ws);
                    }
            RegisterKeyboardShortcuts();
            AutoDetectTools();
            LstFiles.ItemsSource = _files;
            SldrPreview.Value = 0.5;
            _animTimer.Tick += AnimTimer_Tick;
            UpdateAnimFps();

            // Undo/Redo initialisieren mit aktuellen Optionen
            _previewUndoRedo.SetInitial(_currentOptions);

            _previewDebounce.Tick += (_, _) =>
            {
                _previewDebounce.Stop();
                if (!string.IsNullOrEmpty(_lastPreviewFilePath) && File.Exists(_lastPreviewFilePath))
                    LoadPreview(_lastPreviewFilePath);
                else if (LstFiles.SelectedItem is FileItem item)
                    LoadPreview(item.FilePath);
            };
            _grfElapsedTimer.Interval = TimeSpan.FromMilliseconds(500);
            _grfElapsedTimer.Tick += GrfElapsedTimer_Tick;
            SizeChanged += (_, _) => UpdatePreviewClip();
            SldrOnionSkin.ToolTip = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("tooltip.onionSkinStrength");
            Closing += (_, _) =>
            {
                _animTimer.Stop();
                _grfElapsedTimer.Stop();
                _previewDebounce.Stop();
                StopAnimation();
                DisposeSpr();
                _cts?.Cancel();
                _cts?.Dispose();
                WindowStateService.SaveWindowState("MainWindow", Left, Top, Width, Height, WindowState.ToString());
            };
        }

        #region Tool Detection

        private static string? FindToolExecutable(string toolSubfolder, string exeName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            string[] searchBases = new[]
            {
                baseDir, currentDir,
                Path.Combine(baseDir, ".."), Path.Combine(baseDir, "..", ".."),
                Path.Combine(baseDir, "..", "..", ".."), Path.Combine(currentDir, "..")
            };
            foreach (var dir in searchBases)
            {
                try
                {
                    string candidate = Path.GetFullPath(Path.Combine(dir, "tools", toolSubfolder, exeName));
                    if (File.Exists(candidate)) return candidate;
                    string directCandidate = Path.GetFullPath(Path.Combine(dir, exeName));
                    if (File.Exists(directCandidate)) return directCandidate;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainWindow] AutoDetectTools Einzelner Pfad-Fehler: {ex.Message}");
                }
            }
            return null;
        }

        private void AutoDetectTools()
        {
            try
            {
                string? waifuExe = FindToolExecutable("waifu2x", "waifu2x-ncnn-vulkan.exe");
                if (!string.IsNullOrEmpty(waifuExe) && TxtWaifu2xPath != null && string.IsNullOrEmpty(TxtWaifu2xPath.Text))
                {
                    TxtWaifu2xPath.Text = waifuExe;
                }
                string? cuganExe = FindToolExecutable("realcugan", "realcugan-ncnn-vulkan.exe");
                if (!string.IsNullOrEmpty(cuganExe) && TxtRealCuganPath != null && string.IsNullOrEmpty(TxtRealCuganPath.Text))
                    TxtRealCuganPath.Text = cuganExe;
                string? realesrganExe = RealESRGANRunner.AutoDetectExecutable();
                if (!string.IsNullOrEmpty(realesrganExe) && TxtRealESRGANPath != null && string.IsNullOrEmpty(TxtRealESRGANPath.Text))
                    TxtRealESRGANPath.Text = realesrganExe;
                string? gfpganExe = GFPGANRunner.AutoDetectExecutable();
                if (!string.IsNullOrEmpty(gfpganExe) && TxtGFGANPath != null && string.IsNullOrEmpty(TxtGFGANPath.Text))
                    TxtGFGANPath.Text = gfpganExe;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindow] AutoDetectTools Fehler: {ex.Message}");
            }
        }



        #endregion

        #region GRF Workflow

        private void ShowGrfProgress(string opLabel, System.Windows.Media.Color color1, System.Windows.Media.Color color2)
                {
                    TxtGrfOpLabel.Text = opLabel;
                    PrgGrf.Value = 0;
            TxtGrfCurrentFile.Text = "Starte...";
            TxtGrfCounter.Text = "0 / 0";
            TxtGrfSpeed.Text = "—";
            TxtGrfEta.Text = "—";
            TxtGrfElapsed.Text = "00:00";
            TxtGrfStatus.Text = "";
            _grfLastSpeed = 0;
            _grfStopwatch.Restart();
            _grfElapsedTimer.Start();
            GrfProgressPanel.Visibility = Visibility.Visible;
        }

        private void UpdateGrfProgress(int current, int total, string file)
        {
            double pct = total > 0 ? (double)current / total * 100 : 0;
            PrgGrf.Value = pct;
            TxtGrfCurrentFile.Text = file;
            TxtGrfCounter.Text = $"{current:N0} / {total:N0}";
            double elapsed = _grfStopwatch.Elapsed.TotalSeconds;
            if (elapsed > 0.5 && current > 0)
            {
                double rawSpeed = current / elapsed;
                _grfLastSpeed = _grfLastSpeed == 0 ? rawSpeed : _grfLastSpeed * 0.7 + rawSpeed * 0.3;
                TxtGrfSpeed.Text = _grfLastSpeed >= 1 ? $"{_grfLastSpeed:F1} Dateien/s" : $"{_grfLastSpeed * 60:F0} Dateien/min";
                int remaining = total - current;
                if (_grfLastSpeed > 0)
                {
                    double etaSec = remaining / _grfLastSpeed;
                    TxtGrfEta.Text = etaSec < 60 ? $"~{(int)etaSec}s" : etaSec < 3600 ? $"~{(int)(etaSec / 60)}m {(int)(etaSec % 60)}s" : $"~{(int)(etaSec / 3600)}h {(int)(etaSec % 3600 / 60)}m";
                }
            }
        }

        private void GrfElapsedTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = _grfStopwatch.Elapsed;
            TxtGrfElapsed.Text = elapsed.TotalHours >= 1 ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}" : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        private void FinishGrfProgress(string statusMessage)
        {
            _grfElapsedTimer.Stop();
            _grfStopwatch.Stop();
            PrgGrf.Value = 100;
            TxtGrfSpeed.Text = "—";
            TxtGrfEta.Text = "fertig";
            TxtGrfStatus.Text = statusMessage;
            TxtGrfCurrentFile.Text = "✅ Abgeschlossen";
            var elapsed = _grfStopwatch.Elapsed;
            TxtGrfElapsed.Text = elapsed.TotalHours >= 1 ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}" : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        private void HideGrfProgress()
        {
            _grfElapsedTimer.Stop();
            _grfStopwatch.Stop();
            GrfProgressPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnGrfCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            TxtGrfStatus.Text = "Abbrechen wird angefordert...";
            TxtGrfCurrentFile.Text = "⏸ Wird abgebrochen...";
        }

        private void BtnGrfOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "GRF-Datei öffnen", Filter = "GRF-Archive (*.grf)|*.grf|Alle Dateien (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            OpenGrfFile(dialog.FileName);
        }

        private void OpenGrfFile(string path)
        {
            try
            {
                _openGrfPath = path;
                using var grf = GrfArchive.Open(_openGrfPath);
                var counts = grf.GetSpriteCounts();
                int totalSprites = counts.Values.Sum();
                TxtGrfPath.Text = Path.GetFileName(_openGrfPath);
                TxtGrfPath.Foreground = System.Windows.Media.Brushes.LightBlue;
                TxtGrfInfo.Text = $"{grf.FileCount:N0} Dateien · {totalSprites:N0} Sprites";
                GrfInfoBadge.Visibility = Visibility.Visible;
                BuildCategoryCheckboxes(counts);
                GrfFilterPanel.Visibility = Visibility.Visible;
                BtnGrfExtract.IsEnabled = true;
                BtnGrfPack.IsEnabled = false;
                SetGrfStep(1);
                TxtStatus.Text = $"GRF geöffnet: {grf.FileCount:N0} Dateien, {totalSprites:N0} Sprites";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"GRF konnte nicht geöffnet werden:\n{ex.Message}", "GRF Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                _openGrfPath = null;
            }
        }

        private void BuildCategoryCheckboxes(Dictionary<string, int> counts)
        {
            CategoryPanel.Children.Clear();
            foreach (var cat in _categories)
            {
                counts.TryGetValue(cat.Id, out int count);
                var cb = new CheckBox
                {
                    Content = $"{cat.Icon} {cat.DisplayName}  ({count:N0})",
                    IsChecked = cat.IsSelected,
                    Foreground = count > 0 ? System.Windows.Media.Brushes.LightGray : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x55, 0x66)),
                    IsEnabled = count > 0, Margin = new Thickness(0, 0, 14, 4), FontSize = 11, Tag = cat.Id
                };
                cb.Checked += CatCheckbox_Changed;
                cb.Unchecked += CatCheckbox_Changed;
                CategoryPanel.Children.Add(cb);
            }
            UpdateFilterSummary();
        }

        private void CatCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is string id)
            {
                var cat = _categories.FirstOrDefault(c => c.Id == id);
                if (cat != null) cat.IsSelected = cb.IsChecked == true;
            }
            UpdateFilterSummary();
        }

        private void BtnCatAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cat in _categories) cat.IsSelected = true;
            foreach (CheckBox cb in CategoryPanel.Children.OfType<CheckBox>()) if (cb.IsEnabled) cb.IsChecked = true;
            UpdateFilterSummary();
        }

        private void BtnCatNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cat in _categories) cat.IsSelected = false;
            foreach (CheckBox cb in CategoryPanel.Children.OfType<CheckBox>()) cb.IsChecked = false;
            UpdateFilterSummary();
        }

        private void UpdateFilterSummary()
        {
            var selected = _categories.Where(c => c.IsSelected).ToList();
            if (TxtFilterSummary == null) return;
            TxtFilterSummary.Text = selected.Count == _categories.Count ? "Alle Kategorien" : selected.Count == 0 ? "Keine ausgewählt" : string.Join(", ", selected.Select(c => c.DisplayName));
        }

        private async void BtnGrfExtract_Click(object sender, RoutedEventArgs e)
        {
            if (_openGrfPath == null) return;
            var selectedIds = _categories.Where(c => c.IsSelected).Select(c => c.Id).ToList();
            if (selectedIds.Count == 0)
            {
                MessageBox.Show("Bitte mindestens eine Kategorie auswählen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string suggestedFolder = Path.Combine(Path.GetDirectoryName(_openGrfPath) ?? "", Path.GetFileNameWithoutExtension(_openGrfPath) + "_sprites");
            string? folder = BrowseFolderWithSuggestion("Ordner für extrahierte Sprites wählen", suggestedFolder);
            if (folder == null) return;
            _grfExtractFolder = folder;
            _grfEnhancedFolder = folder + "_enhanced";
            TxtInputPath.Text = _grfExtractFolder;
            TxtOutputPath.Text = _grfEnhancedFolder;
            bool allCategories = selectedIds.Count == _categories.Count;
            string catLabel = allCategories ? "alle Kategorien" : string.Join(", ", _categories.Where(c => c.IsSelected).Select(c => c.DisplayName));
            SetProcessingState(true);
            SetGrfStep(2);
            ShowGrfProgress("📤 EXTRAHIEREN", System.Windows.Media.Color.FromRgb(0x00, 0x88, 0xFF), System.Windows.Media.Color.FromRgb(0x00, 0xDD, 0xFF));
            TxtGrfStatus.Text = $"Kategorien: {catLabel}";
            _cts?.Cancel(); _cts?.Dispose();
            _cts = new CancellationTokenSource();
            try
            {
                int extracted = 0;
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(_grfExtractFolder);
                    using var grf = GrfArchive.Open(_openGrfPath);
                    var progress = new Progress<(int current, int total, string file)>(p => Dispatcher.InvokeAsync(() => UpdateGrfProgress(p.current, p.total, p.file)));
                    string customP = TxtCustomGrfPath?.Text?.Trim() ?? "";
                    extracted = grf.ExtractSprites(_grfExtractFolder, allCategories ? null : selectedIds, progress, _cts.Token);
                }, _cts.Token);
                FinishGrfProgress($"{extracted:N0} Sprites · Ausgabe: {Path.GetFileName(_grfExtractFolder)}");
                TxtGrfCounter.Text = $"{extracted:N0} / {extracted:N0}";
                TxtStatus.Text = "Extraktion abgeschlossen";
                LoadFileList(_grfExtractFolder);
                SetGrfStep(3);
                BtnGrfPack.IsEnabled = true;
                MessageBox.Show($"{extracted:N0} Sprites extrahiert:\n{_grfExtractFolder}\n\n▶ Klicke 'BATCH STARTEN' um alle Sprites zu verbessern\n📦 Dann 'Zurück in GRF packen'", "Extraktion abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                _grfElapsedTimer.Stop();
                TxtGrfCurrentFile.Text = "⏹ Abgebrochen";
                TxtGrfStatus.Text = "Extraktion abgebrochen";
                TxtGrfEta.Text = "—";
                TxtStatus.Text = "Abgebrochen";
            }
            catch (Exception ex)
            {
                HideGrfProgress();
                MessageBox.Show($"Fehler bei der Extraktion:\n{ex.Message}", "GRF Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { SetProcessingState(false); }
        }

        private async void BtnGrfPack_Click(object sender, RoutedEventArgs e)
        {
            if (_openGrfPath == null) return;
            string packSource = !string.IsNullOrEmpty(_grfEnhancedFolder) && Directory.Exists(_grfEnhancedFolder) ? _grfEnhancedFolder : (!string.IsNullOrEmpty(TxtOutputPath.Text) && Directory.Exists(TxtOutputPath.Text) ? TxtOutputPath.Text : "");
            if (string.IsNullOrEmpty(packSource))
            {
                MessageBox.Show("Kein Ausgabe-Ordner mit verbesserten Sprites gefunden.\nBitte zuerst den Batch-Enhancer ausführen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string defaultGrfOut = Path.Combine(Path.GetDirectoryName(_openGrfPath) ?? "", Path.GetFileNameWithoutExtension(_openGrfPath) + "_enhanced.grf");
            var dialog = new SaveFileDialog { Title = "Neue GRF-Datei speichern", Filter = "GRF-Archive (*.grf)|*.grf", FileName = Path.GetFileName(defaultGrfOut), InitialDirectory = Path.GetDirectoryName(_openGrfPath) ?? "" };
            if (dialog.ShowDialog() != true) return;
            string outputGrf = dialog.FileName;
            var modeResult = MessageBox.Show("Wie soll die neue GRF erstellt werden?\n\n[Ja]   = MERGE: Originale GRF als Basis, verbesserte Sprites ersetzen originale\n[Nein] = NEU: Nur die verbesserten Sprites einpacken", "Pack-Modus wählen", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (modeResult == MessageBoxResult.Cancel) return;
            bool mergeMode = modeResult == MessageBoxResult.Yes;
            SetProcessingState(true);
            SetGrfStep(4);
            if (mergeMode)
                ShowGrfProgress("📦 MERGE", System.Windows.Media.Color.FromRgb(0x00, 0xAA, 0x44), System.Windows.Media.Color.FromRgb(0x44, 0xFF, 0x88));
            else
                ShowGrfProgress("📦 PACKEN", System.Windows.Media.Color.FromRgb(0xFF, 0x88, 0x00), System.Windows.Media.Color.FromRgb(0xFF, 0xCC, 0x00));
            TxtGrfStatus.Text = mergeMode ? $"Basis: {Path.GetFileName(_openGrfPath)} + Patch: {Path.GetFileName(packSource)}" : $"Quelle: {Path.GetFileName(packSource)}";
            _cts?.Cancel(); _cts?.Dispose();
            _cts = new CancellationTokenSource();
            try
            {
                await Task.Run(() =>
                {
                    var progress = new Progress<(int current, int total, string file)>(p => Dispatcher.InvokeAsync(() => UpdateGrfProgress(p.current, p.total, p.file)));
                    if (mergeMode)
                        GrfArchive.MergeWithFolder(_openGrfPath, packSource, outputGrf, progress, _cts.Token);
                    else
                        GrfArchive.PackFolder(packSource, outputGrf, progress, _cts.Token);
                }, _cts.Token);
                var fi = new FileInfo(outputGrf);
                FinishGrfProgress($"GRF erstellt: {fi.Name} · {fi.Length / 1024 / 1024:F1} MB");
                TxtStatus.Text = "GRF-Erstellung abgeschlossen";
                string hint = mergeMode ? "Trage die neue GRF in deiner DATA.INI vor der originalen data.grf ein." : "Die neue GRF enthält nur die verbesserten Sprites.";
                MessageBox.Show($"GRF erfolgreich erstellt:\n{outputGrf}\n({fi.Length / 1024 / 1024:F1} MB)\n\n{hint}", "GRF erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
                SetGrfStep(0);
            }
            catch (OperationCanceledException)
            {
                _grfElapsedTimer.Stop();
                TxtGrfCurrentFile.Text = "⏹ Abgebrochen";
                TxtGrfStatus.Text = "Vorgang abgebrochen";
                TxtGrfEta.Text = "—";
                TxtStatus.Text = "Abgebrochen";
            }
            catch (Exception ex)
            {
                HideGrfProgress();
                MessageBox.Show($"Fehler beim Packen:\n{ex.Message}", "GRF Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { SetProcessingState(false); }
        }

        private void SetGrfStep(int activeStep)
        {
            var steps = new[] { TxtGrfStep1, TxtGrfStep2, TxtGrfStep3, TxtGrfStep4 };
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] == null) continue;
                steps[i].Foreground = i < activeStep ? System.Windows.Media.Brushes.LimeGreen : activeStep == i + 1 ? System.Windows.Media.Brushes.Gold : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x4A, 0x6B));
            }
        }

        private static string? BrowseFolderWithSuggestion(string description, string suggestion)
        {
            var dialog = new OpenFolderDialog { Title = description, InitialDirectory = Path.GetDirectoryName(suggestion) ?? "" };
            if (dialog.ShowDialog() == true) return dialog.FolderName;
            var res = MessageBox.Show($"Keinen Ordner gewählt.\nStandardpfad verwenden?\n\n{suggestion}", "Pfad bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes) { Directory.CreateDirectory(suggestion); return suggestion; }
            return null;
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            string? folder = BrowseFolder("Eingabe-Ordner wählen (extrahierte Sprites)");
            if (folder == null) return;
            TxtInputPath.Text = folder;
            LoadFileList(folder);
        }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            string? folder = BrowseFolder("Ausgabe-Ordner wählen");
            if (folder != null) TxtOutputPath.Text = folder;
        }

        private void BtnCreateSpriteAtlas_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Sprite-Bilder für Atlas auswählen",
                Filter = "Bilder (*.bmp;*.png;*.tga)|*.bmp;*.png;*.tga|Alle Dateien (*.*)|*.*",
                Multiselect = true,
                InitialDirectory = string.IsNullOrEmpty(TxtInputPath.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : TxtInputPath.Text
            };

            if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0) return;

            var folderDialog = new OpenFolderDialog { Title = "Ausgabe-Ordner für Atlas wählen" };
            if (!string.IsNullOrEmpty(TxtOutputPath.Text))
                folderDialog.InitialDirectory = TxtOutputPath.Text;
            else
                folderDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (folderDialog.ShowDialog() != true) return;

            string outputDir = folderDialog.FolderName;
            string baseName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileNames[0]) + "_atlas";

            try
            {
                TxtStatus.Text = "Sprite Atlas wird erstellt...";
                var atlases = SpriteAtlasGenerator.CreateAtlas(dialog.FileNames.ToList(), outputDir, baseName);

                if (atlases.Count == 0)
                {
                    MessageBox.Show("Keine gültigen Bilder gefunden.", "Sprite Atlas", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int totalSprites = atlases.Sum(a => a.SpriteCount);
                string msg = "✅ Sprite Atlas erstellt:" + Environment.NewLine + Environment.NewLine;
                msg += "• " + atlases.Count + " Atlas-Datei(en)" + Environment.NewLine;
                msg += "• " + totalSprites + " Sprites verarbeitet" + Environment.NewLine;
                msg += "• Ausgabeordner: " + outputDir + Environment.NewLine + Environment.NewLine;
                msg += "Metadatendateien (*.atlas.json) enthalten die Koordinaten jedes Sprites.";

                MessageBox.Show(msg, "Sprite Atlas", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatus.Text = "Sprite Atlas erstellt: " + totalSprites + " Sprites in " + atlases.Count + " Datei(en)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Erstellen des Sprite Atlas:" + Environment.NewLine + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Fehler beim Erstellen des Sprite Atlas";
            }
        }

        private static string? BrowseFolder(string description)
        {
            var dialog = new OpenFolderDialog { Title = description };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        #endregion

        #region Drag & Drop

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) { e.Effects = DragDropEffects.Copy; DropIndicator.Visibility = Visibility.Visible; }
            else { e.Effects = DragDropEffects.None; }
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e) { DropIndicator.Visibility = Visibility.Collapsed; }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            DropIndicator.Visibility = Visibility.Collapsed;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropped == null || dropped.Length == 0) return;
            string first = dropped[0];
            if (Directory.Exists(first)) { TxtInputPath.Text = first; LoadFileList(first); TxtStatus.Text = $"Ordner geladen: {Path.GetFileName(first)}"; }
            else if (File.Exists(first))
            {
                string ext = Path.GetExtension(first).ToLowerInvariant();
                if (ext == ".grf") { TxtStatus.Text = $"GRF wird geöffnet: {Path.GetFileName(first)}"; OpenGrfFile(first); }
                else if (ext is ".bmp" or ".png" or ".tga" or ".spr") { _lastPreviewFilePath = first; LoadPreview(first); TxtStatus.Text = $"Datei geladen: {Path.GetFileName(first)}"; }
            }
            e.Handled = true;
        }

        #endregion

        #region File List & Preview

        private void LoadFileList(string folder)
        {
            _files.Clear();
            var opts = _currentOptions;
            var files = _processor.GetFiles(opts);
            foreach (var f in files)
                _files.Add(new FileItem { FilePath = f, FileName = Path.GetRelativePath(folder, f) });
            TxtFileCount.Text = $"{_files.Count} Dateien";
            TxtStatus.Text = $"{_files.Count} Sprites gefunden";
            if (_gridViewActive) BuildGridView();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || string.IsNullOrEmpty(TxtInputPath.Text)) return;
            LoadFileList(TxtInputPath.Text);
        }

        private void BtnViewList_Click(object sender, RoutedEventArgs e) { _contactSheetMode = false; SetView(false); }
        private void BtnViewGrid_Click(object sender, RoutedEventArgs e) { _contactSheetMode = false; SetView(true); }
        private void BtnViewContact_Click(object sender, RoutedEventArgs e) { _contactSheetMode = true; SetView(true); }

        private void SetView(bool gridMode)
        {
            _gridViewActive = gridMode;
            PreviewScroll.Visibility = gridMode ? Visibility.Collapsed : Visibility.Visible;
            GridViewScroll.Visibility = gridMode ? Visibility.Visible : Visibility.Collapsed;
            if (gridMode) BuildGridView();
        }

        private void BuildGridView()
        {
            ThumbPanel.Children.Clear();
            var opts = _contactSheetMode ? _currentOptions : null;
            int tileWidth = _contactSheetMode ? 172 : 100;
            foreach (var item in _files)
            {
                var border = new Border { Width = tileWidth, Height = 110, Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0D, 0x1B, 0x2A)), BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x4A, 0x6B)), BorderThickness = new Thickness(1), Margin = new Thickness(4), Cursor = System.Windows.Input.Cursors.Hand };
                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                string filePath = item.FilePath;
                if (_contactSheetMode)
                {
                    var imgRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                    var imgBefore = new System.Windows.Controls.Image { Width = 78, Height = 78, Stretch = Stretch.Uniform, Margin = new Thickness(2, 4, 1, 2) };
                    RenderOptions.SetBitmapScalingMode(imgBefore, BitmapScalingMode.NearestNeighbor);
                    var imgAfter = new System.Windows.Controls.Image { Width = 78, Height = 78, Stretch = Stretch.Uniform, Margin = new Thickness(1, 4, 2, 2) };
                    RenderOptions.SetBitmapScalingMode(imgAfter, BitmapScalingMode.NearestNeighbor);
                    imgRow.Children.Add(imgBefore); imgRow.Children.Add(imgAfter);
                    stack.Children.Add(imgRow);
                    Task.Run(() =>
                    {
                        try
                        {
                            Bitmap? origBmp = LoadFirstFrameOrBitmap(filePath);
                            if (origBmp == null) return;
                            BitmapSource beforeSrc = BitmapToImageSource(origBmp, 78);
                            Bitmap? afterBmp = null;
                            try
                            {
                                string relPath = Path.GetRelativePath(opts!.InputFolder, filePath).Replace("/", "\\");
                                                            var enhancerOverride = BatchProcessor.ResolveEnhancerOptions(relPath, opts);
                                                            var categoryId = ResolveCategoryIdInMain(relPath);
                                                            afterBmp = BatchProcessor.ProcessBitmapPreview(origBmp, opts, categoryId, enhancerOverride);
                                                        }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MainWindow] Batch preview inner Fehler: {ex.Message}");
                            }
                            BitmapSource? afterSrc = afterBmp != null ? BitmapToImageSource(afterBmp, 78) : null;
                            origBmp.Dispose(); afterBmp?.Dispose();
                            Dispatcher.InvokeAsync(() => { imgBefore.Source = beforeSrc; if (afterSrc != null) imgAfter.Source = afterSrc; });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MainWindow] Batch preview outer Fehler: {ex.Message}");
                        }
                    });
                }
                else
                {
                    var img = new System.Windows.Controls.Image { Width = 80, Height = 80, Stretch = Stretch.Uniform, Margin = new Thickness(0, 4, 0, 4) };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                    Task.Run(() =>
                    {
                        try
                        {
                            Bitmap? bmp = LoadFirstFrameOrBitmap(filePath);
                            if (bmp == null) return;
                            BitmapSource thumb = BitmapToImageSource(bmp, 80);
                            bmp.Dispose();
                            Dispatcher.InvokeAsync(() => img.Source = thumb);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MainWindow] Thumbnail Fehler: {ex.Message}");
                        }
                    });
                    stack.Children.Add(img);
                }
                var label = new TextBlock { Text = Path.GetFileName(item.FilePath), FontSize = 9, Foreground = new SolidColorBrush(Colors.Gray), TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = tileWidth - 8, Margin = new Thickness(2, 0, 2, 4) };
                stack.Children.Add(label);
                border.Child = stack;
                border.MouseLeftButtonUp += (_, _) =>
                {
                    SetView(false);
                    _lastPreviewFilePath = filePath;
                    LoadPreview(filePath);
                    var found = _files.FirstOrDefault(f => f.FilePath == filePath);
                    if (found != null) LstFiles.SelectedItem = found;
                };
                ThumbPanel.Children.Add(border);
            }
        }

        private static Bitmap? LoadFirstFrameOrBitmap(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".spr")
            {
                using var spr = SprFile.Load(filePath);
                if (spr.Frames.Count == 0) return null;
                var first = spr.Frames.OrderBy(f => f.Index).First();
                if (first.Width == 0 || first.Height == 0) return null;
                return new Bitmap(first.Image);
            }
            return new Bitmap(filePath);
        }

        private void LstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstFiles.SelectedItem is FileItem item) { _lastPreviewFilePath = item.FilePath; LoadPreview(item.FilePath); }
        }

        private void BtnRefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastPreviewFilePath)) LoadPreview(_lastPreviewFilePath);
            else if (LstFiles.SelectedItem is FileItem item) LoadPreview(item.FilePath);
        }

        private async void LoadPreview(string filePath)
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
                    _lastPreviewFilePath = filePath;
                    StopAnimation();
                    NoFileHint.Visibility = Visibility.Collapsed;
                    TxtPreviewFile.Text = Path.GetFileName(filePath);
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext == ".spr") { LoadSprPreview(filePath); return; }
                    SprBadge.Visibility = Visibility.Collapsed;
                    AnimBar.Visibility = Visibility.Collapsed;
                    TxtStatus.Text = "Vorschau wird berechnet...";
                    // Alte Image-Sources freigeben bevor neue geladen werden (Speicher-Leak vermeiden)
                    ImgBefore.Source = null;
                    ImgAfter.Source = null;
                    Bitmap? before = null;
                    Bitmap? after = null;
                    try
                    {
                        var opts = _currentOptions;
                        (before, after) = await Task.Run(() =>
                        {
                            Bitmap? b = LoadBitmapSafe(filePath);
                            if (b == null) return ((Bitmap?)null, (Bitmap?)null);
                            Bitmap? a = _processor.ProcessSingle(filePath, opts);
                            return ((Bitmap?)b, a);
                        });
                        if (!string.Equals(_lastPreviewFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            before?.Dispose();
                            after?.Dispose();
                            return;
                        }
                        if (before == null)
                        {
                            after?.Dispose();
                            TxtStatus.Text = "Datei konnte nicht geladen werden.";
                            return;
                        }
                        int dispW = after?.Width ?? before.Width;
                        int dispH = after?.Height ?? before.Height;
                        Bitmap beforeDisplay = ScaleForDisplay(before, dispW, dispH);
                        const int maxDisplaySize = 512;
                        ImgBefore.Source = BitmapToImageSource(beforeDisplay, maxDisplaySize);
                        ImgAfter.Source = BitmapToImageSource(after, maxDisplaySize);
                        // Dimensionen vor dem Dispose merken
                        int finalW = beforeDisplay.Width;
                        int finalH = beforeDisplay.Height;
                        int finalAw = after?.Width ?? 0;
                                                int finalAh = after?.Height ?? 0;
                        // FIX: before-Disposal je nachdem ob ScaleForDisplay src direkt zurückgab
                        if (ReferenceEquals(beforeDisplay, before))
                                                {
                                                    before.Dispose(); // beforeDisplay == before, beide auf einmal entsorgen
                                                    before = null;
                                                }
                        else
                                                {
                                                    beforeDisplay.Dispose();
                                                    before.Dispose();
                                                    before = null;
                                                }
                        // FIX: after-Bitmap nach BitmapToImageSource entsorgen (intern wird kopiert + Frozen)
                        after?.Dispose();
                                                after = null;
                        TxtPixelInfo.Text = $"{finalW}×{finalH} → {finalAw}×{finalAh}";
                        TxtStatus.Text = "Vorschau bereit";
                        FitPreviewToContainer();
                        await Dispatcher.InvokeAsync(UpdatePreviewClip, DispatcherPriority.Render);
                    }
                    catch (Exception ex)
                    {
                        before?.Dispose();
                        after?.Dispose();
                        TxtStatus.Text = $"Fehler: {ex.Message}";
                    }
                }

        private static Bitmap ScaleForDisplay(Bitmap src, int dstW, int dstH)
        {
            if (src.Width == dstW && src.Height == dstH) return src;
            var dst = new Bitmap(dstW, dstH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(dst);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(src, 0, 0, dstW, dstH);
            return dst;
        }

        private void LoadSprPreview(string filePath)
        {
            try
            {
                TxtStatus.Text = "SPR wird geladen...";
                DisposeSpr();
                _currentSpr = SprFile.Load(filePath);
                _currentFrame = 0;
                SprBadge.Visibility = Visibility.Visible;
                TxtSprInfo.Text = $"SPR · {_currentSpr.TotalFrameCount} Frames";
                AnimBar.Visibility = _currentSpr.TotalFrameCount > 1 ? Visibility.Visible : Visibility.Collapsed;
                SldrFrame.Maximum = Math.Max(0, _currentSpr.TotalFrameCount - 1);
                SldrFrame.Value = 0;
                TxtFrameCounter.Text = $"Frame 1 / {_currentSpr.TotalFrameCount}";
                ShowSprFrame(0);
                TxtStatus.Text = $"SPR: {_currentSpr.TotalFrameCount} Frames geladen (Vorschau bereit)";
            }
            catch (Exception ex) { TxtStatus.Text = $"SPR Fehler: {ex.Message}"; }
        }

        private void ShowSprFrame(int frameIdx) { _frameRenderInProgress = true; _ = RenderSprFrameAsync(frameIdx); }

        private async Task ShowSprFrameInternal(int frameIdx)
        {
            if (_currentSpr == null || frameIdx < 0 || frameIdx >= _currentSpr.Frames.Count) return;
            var frame = _currentSpr.Frames[frameIdx];
            var opts = _currentOptions;
            ImgBefore.Source = BitmapToImageSource(frame.Image, 512);

            if (_onionSkinEnabled)
            {
                // Onion Skin: Mehrere Frames überblenden
                Bitmap? onionBitmap = null;
                try
                {
                    onionBitmap = CreateOnionSkinBitmap(frameIdx, opts);
                    if (onionBitmap != null)
                    {
                        ImgAfter.Source = BitmapToImageSource(onionBitmap, 512);
                        TxtPixelInfo.Text = $"{frame.Width}×{frame.Height} → Onion Skin ({_currentSpr.TotalFrameCount} Frames)";
                    }
                    else
                    {
                        ImgAfter.Source = BitmapToImageSource(frame.Image, 512);
                    }
                }
                finally
                {
                    onionBitmap?.Dispose();
                }
            }
            else
            {
                Bitmap? enhanced = null;
                try
                {
                    enhanced = await Task.Run(() =>
                    {
                        string relPath = !string.IsNullOrEmpty(opts.InputFolder) && !string.IsNullOrEmpty(_currentSpr.FilePath) ? Path.GetRelativePath(opts.InputFolder, _currentSpr.FilePath).Replace("/", "\\") : "";
                        var enhancerOverride = BatchProcessor.ResolveEnhancerOptions(relPath, opts);
                        var categoryId = ResolveCategoryIdInMain(relPath);
                        return BatchProcessor.ProcessBitmapPreview(frame.Image, opts, categoryId, enhancerOverride);
                    });
                    Bitmap beforeDisplay = ScaleForDisplay(frame.Image, enhanced.Width, enhanced.Height);
                    ImgBefore.Source = BitmapToImageSource(beforeDisplay, 512);
                    if (!ReferenceEquals(beforeDisplay, frame.Image)) beforeDisplay.Dispose();
                    ImgAfter.Source = BitmapToImageSource(enhanced, 512);
                    TxtPixelInfo.Text = $"{frame.Width}×{frame.Height} → {enhanced.Width}×{enhanced.Height}";
                }
                finally { enhanced?.Dispose(); }
            }
            TxtFrameCounter.Text = $"Frame {frameIdx + 1} / {_currentSpr.TotalFrameCount}";
            await Dispatcher.InvokeAsync(UpdatePreviewClip, DispatcherPriority.Render);
        }

        private static Bitmap EnsureArgb(Bitmap src)
        {
            if (src.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb) return new Bitmap(src);
            var dst = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(dst);
            g.Clear(System.Drawing.Color.Transparent);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            return dst;
        }

        private void BtnAnimPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSpr == null || _currentSpr.TotalFrameCount < 2) return;
            if (_isAnimPlaying) return;
            _isAnimPlaying = true;
            BtnAnimPlay.IsEnabled = false;
            BtnAnimStop.IsEnabled = true;
            UpdateAnimFps();
            _animTimer.Start();
        }

        private void BtnAnimStop_Click(object sender, RoutedEventArgs e) => StopAnimation();

        private void StopAnimation()
        {
            _animTimer.Stop();
            _isAnimPlaying = false;
            BtnAnimPlay.IsEnabled = _currentSpr != null && _currentSpr.TotalFrameCount > 1;
            BtnAnimStop.IsEnabled = false;
        }

        private bool _frameRenderInProgress = false;

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentSpr == null) { StopAnimation(); return; }
            if (_frameRenderInProgress) return;
            _frameRenderInProgress = true;
            _currentFrame = (_currentFrame + 1) % _currentSpr.TotalFrameCount;
            SldrFrame.Value = _currentFrame;
            _ = RenderSprFrameAsync(_currentFrame);
        }

        private async Task RenderSprFrameAsync(int frameIdx)
        {
            try { await ShowSprFrameInternal(frameIdx); }
            finally { _frameRenderInProgress = false; }
        }

        private void SldrFrame_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int frame = (int)e.NewValue;
            if (frame != _currentFrame) { _currentFrame = frame; ShowSprFrame(_currentFrame); }
        }

        private void CmbAnimFps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateAnimFps();
        }

        private void UpdateAnimFps()
        {
            int fps = int.TryParse((CmbAnimFps?.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int f) ? f : 15;
            _animTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
        }

        #region Onion Skin

        private bool _onionSkinEnabled = false;
        private float _onionSkinStrength = 0.3f;

        private void ChkOnionSkin_Checked(object sender, RoutedEventArgs e)
        {
            _onionSkinEnabled = true;
            SldrOnionSkin.IsEnabled = true;
            if (_currentSpr != null) ShowSprFrame(_currentFrame);
        }

        private void ChkOnionSkin_Unchecked(object sender, RoutedEventArgs e)
        {
            _onionSkinEnabled = false;
            SldrOnionSkin.IsEnabled = false;
            if (_currentSpr != null) ShowSprFrame(_currentFrame);
        }

        private void SldrOnionSkin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _onionSkinStrength = (float)e.NewValue;
            if (_currentSpr != null && _onionSkinEnabled) ShowSprFrame(_currentFrame);
        }

        /// <summary>
        /// Erstellt ein Onion-Skin Bitmap: Der aktuelle Frame ist voll sichtbar,
        /// benachbarte Frames werden mit abnehmender Opacity darüber gezeichnet.
        /// </summary>
        private Bitmap? CreateOnionSkinBitmap(int frameIdx, ProcessingOptions opts)
        {
            if (_currentSpr == null || frameIdx < 0 || frameIdx >= _currentSpr.Frames.Count) return null;

            int count = _currentSpr.TotalFrameCount;
            int overlayCount = 3;

            var currentFrame = _currentSpr.Frames[frameIdx];
            Bitmap result = new Bitmap(currentFrame.Width, currentFrame.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(result))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                g.DrawImage(currentFrame.Image, 0, 0, currentFrame.Width, currentFrame.Height);

                for (int offset = 1; offset <= overlayCount; offset++)
                {
                    float opacity = _onionSkinStrength / offset;

                    int prevIdx = (frameIdx - offset + count) % count;
                    var prevFrame = _currentSpr.Frames[prevIdx];
                    DrawFrameWithOpacity(g, prevFrame.Image, opacity);

                    int nextIdx = (frameIdx + offset) % count;
                    var nextFrame = _currentSpr.Frames[nextIdx];
                    DrawFrameWithOpacity(g, nextFrame.Image, opacity);
                }
            }

            return result;
        }

        private static void DrawFrameWithOpacity(Graphics g, Bitmap frame, float opacity)
        {
            if (frame == null || opacity <= 0) return;

            using var imgAttr = new ImageAttributes();
            var matrix = new ColorMatrix
            {
                Matrix00 = 1, Matrix11 = 1, Matrix22 = 1, Matrix33 = opacity, Matrix44 = 1
            };
            imgAttr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            g.DrawImage(frame,
                new Rectangle(0, 0, frame.Width, frame.Height),
                0, 0, frame.Width, frame.Height,
                GraphicsUnit.Pixel, imgAttr);
        }

        #endregion

        private void DisposeSpr() { _currentSpr?.Dispose(); _currentSpr = null; }

        #endregion

        #region Zoom & Pan

        private System.Windows.Point _panStartPoint;
        private bool _isPanning;
        private bool _isSideBySide;

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomLevel + ZoomStep);
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomLevel - ZoomStep);
        private void BtnZoomReset_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);

        private void PreviewContainer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0) SetZoom(_zoomLevel + ZoomStep);
            else SetZoom(_zoomLevel - ZoomStep);
            e.Handled = true;
        }

        private void PreviewContainer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_zoomLevel > 1.0)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(PreviewContainer);
                PreviewContainer.CaptureMouse();
                PreviewContainer.Cursor = System.Windows.Input.Cursors.Hand;
                e.Handled = true;
            }
        }

        private void PreviewContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isPanning && PreviewContainer.IsMouseCaptured)
            {
                var currentPos = e.GetPosition(PreviewContainer);
                var delta = currentPos - _panStartPoint;
                PanTransform.X += delta.X;
                PanTransform.Y += delta.Y;
                _panStartPoint = currentPos;
                e.Handled = true;
            }
        }

        private void PreviewContainer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                PreviewContainer.ReleaseMouseCapture();
                PreviewContainer.Cursor = null;
                e.Handled = true;
            }
        }

        private void BtnSideBySide_Click(object sender, RoutedEventArgs e)
        {
            _isSideBySide = BtnSideBySide.IsChecked == true;
            if (_isSideBySide)
            {
                // Seitenweiser Vergleich: 50/50 Split
                SldrPreview.Value = 0.5;
                SldrPreview.IsEnabled = false;
                TxtStatus.Text = "Vergleichs-Modus: Seitenweise (Original ↔ Verarbeitet)";
            }
            else
            {
                SldrPreview.IsEnabled = true;
                TxtStatus.Text = "Blend-Modus aktiv";
            }
            UpdatePreviewClip();
        }

        private void SetZoom(double level)
        {
            _zoomLevel = Math.Clamp(level, ZoomMin, ZoomMax);
            ZoomTransform.ScaleX = _zoomLevel;
            ZoomTransform.ScaleY = _zoomLevel;
            TxtZoomLevel.Text = $"{(int)(_zoomLevel * 100)}%";
            double baseW = 600, baseH = 500;
            CheckerBg.Width = Math.Max(baseW, baseW * _zoomLevel);
            CheckerBg.Height = Math.Max(baseH, baseH * _zoomLevel);
            ZoomTarget.Width = CheckerBg.Width;
            ZoomTarget.Height = CheckerBg.Height;
            ZoomTransform.CenterX = ZoomTarget.Width / 2;
            ZoomTransform.CenterY = ZoomTarget.Height / 2;
            // PanTransform zurücksetzen wenn Zoom auf 1.0 gesetzt wird
            if (_zoomLevel == 1.0)
            {
                PanTransform.X = 0;
                PanTransform.Y = 0;
            }
            UpdatePreviewClip();
        }

        private void FitPreviewToContainer()
        {
            var src = (ImgAfter.Source ?? ImgBefore.Source) as BitmapSource;
            if (src == null) return;
            double availW = PreviewScroll.ActualWidth > 0 ? PreviewScroll.ActualWidth : 600;
            double availH = PreviewScroll.ActualHeight > 0 ? PreviewScroll.ActualHeight : 500;
            double imgW = src.PixelWidth;
            double imgH = src.PixelHeight;
            if (imgW <= availW && imgH <= availH) SetZoom(1.0);
            else { double fitZoom = Math.Min(availW / imgW, availH / imgH) * 0.9; SetZoom(Math.Max(ZoomMin, Math.Round(fitZoom / ZoomStep) * ZoomStep)); }
        }

        private void SldrPreview_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreviewClip();

        private void UpdatePreviewClip()
        {
            if (ImgAfter == null) return;
            double val = SldrPreview?.Value ?? 0.5;
            // Blend-Effekt: Opacity-Übergang zwischen Vorher und Nachher
            ImgBefore.Opacity = 1.0 - val;
            ImgAfter.Opacity = val;
            // DividerLine als visuelle Positionshilfe behalten
            if (DividerLine != null && ImgAfter.ActualWidth > 0)
            {
                double zw = ZoomTarget.ActualWidth > 0 ? ZoomTarget.ActualWidth : ZoomTarget.Width;
                double zh = ZoomTarget.ActualHeight > 0 ? ZoomTarget.ActualHeight : ZoomTarget.Height;
                double imgLeft = (zw - ImgAfter.ActualWidth) / 2;
                double lineX = imgLeft + ImgAfter.ActualWidth * val;
                DividerLine.X1 = lineX; DividerLine.X2 = lineX;
                DividerLine.Y1 = 0; DividerLine.Y2 = zh;
            }
        }

        #endregion

        #region Batch Processing

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (_processingActive) { _cts?.Cancel(); return; }
            if (string.IsNullOrEmpty(TxtInputPath.Text))
            {
                MessageBox.Show("Bitte zuerst einen Eingabe-Ordner wählen.", "RO Sprite Enhancer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(TxtOutputPath.Text)) TxtOutputPath.Text = TxtInputPath.Text + "_enhanced";
            if (_files.Count == 0)
            {
                MessageBox.Show("Keine Dateien gefunden. Bitte Filter prüfen.", "RO Sprite Enhancer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var res = MessageBox.Show($"{_files.Count} Sprites werden verarbeitet.\nAusgabe: {TxtOutputPath.Text}\n\nJetzt starten?", "Batch-Verarbeitung", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            SetProcessingState(true);
            var opts = _currentOptions;
            var files = _files.Select(f => f.FilePath).ToList();
            var fileMap = _files.ToDictionary(f => f.FilePath);
            _cts?.Cancel(); _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var progress = new Progress<(int current, int total, string file, double throughput)>(p =>
            {
                PrgBatch.Value = (double)p.current / p.total * 100;
                TxtProgress.Text = $"Verarbeite: {p.file}";
                TxtProgressDetail.Text = $"{p.current} / {p.total}";
                TxtThroughput.Text = $"⚡ {p.throughput:F1} Sprite/s";
                string fullPath = files[p.current - 1];
                if (fileMap.TryGetValue(fullPath, out var fi)) fi.Done = true;
            });
            try
            {
                // TASK-1806: Qualitätsberichte vor dem Batch leeren
                _processor.QualityReports.Clear();

                var result = await _processor.ProcessAsync(opts, files, progress, _cts.Token);
                string summary = $"✅ Fertig in {result.Duration.TotalSeconds:F1}s — {result.Processed} verarbeitet, {result.Skipped} übersprungen";
                if (result.SkippedUnchanged > 0) summary += $", {result.SkippedUnchanged} unverändert (inkrementell)";
                if (result.Errors > 0) summary += $", {result.Errors} Fehler";
                TxtProgress.Text = summary;
                TxtProgressDetail.Text = "";
                PrgBatch.Value = 100;
                TxtStatus.Text = "Batch abgeschlossen";

                // TASK-1806: Qualitäts-Check Ergebnisse anzeigen
                var qualityReports = _processor.QualityReports;
                if (qualityReports.Count > 0)
                {
                    var problematic = qualityReports.Where(r => r.Issues.Count > 0).ToList();
                    if (problematic.Count > 0)
                    {
                        var msg = "⚠️ Qualitäts-Check: " + problematic.Count + " von " + qualityReports.Count + " Dateien haben Hinweise:\n\n";
                        foreach (var r in problematic.Take(10))
                        {
                            msg += "• " + r.FileName + ": " + string.Join(" | ", r.Issues) + "\n";
                        }
                        if (problematic.Count > 10) msg += "\n... und " + (problematic.Count - 10) + " weitere";
                        msg += "\n\nDurchschnittliche Qualität: " + qualityReports.Average(r => r.OverallScore).ToString("P0");
                        MessageBox.Show(msg, "Qualitäts-Check", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        TxtStatus.Text = "Batch abgeschlossen – Alle " + qualityReports.Count + " Dateien im grünen Bereich (Qualität OK)";
                    }
                }

                MessageBox.Show(summary, "Fertig", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                TxtProgress.Text = "Verarbeitung abgebrochen.";
                TxtStatus.Text = "Abgebrochen";
            }
            finally { SetProcessingState(false); }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

        private void SetProcessingState(bool active)
        {
            _processingActive = active;
            BtnProcess.Content = active ? "⬛ LÄUFT..." : "▶ BATCH STARTEN";
            BtnStop.IsEnabled = active;
            BtnProcess.IsEnabled = true;
        }

        #endregion



        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            // Settings-only handler - no-op in MainWindow
        }

        #region Settings Window

        private SettingsWindow? _settingsWindow;

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded) { _settingsWindow.Activate(); return; }
            _settingsWindow = new SettingsWindow(_currentOptions);
            _settingsWindow.OptionsChanged += OnSettingsOptionsChanged;
            bool? result = _settingsWindow.ShowDialog();
            _settingsWindow.OptionsChanged -= OnSettingsOptionsChanged;
            if (result == true)
            {
                // Aktuellen Zustand vor Änderung speichern (für Undo)
                _previewUndoRedo.Push(_currentOptions);
                _currentOptions = _settingsWindow.Options;
                TxtStatus.Text = "Einstellungen übernommen";
            }
        }

        private void OnSettingsOptionsChanged(object? sender, EventArgs e)
        {
            // Debounced Live-Vorschau bei Einstellungs-Änderungen im Settings-Fenster
            _previewDebounce.Stop();
            _previewDebounce.Tick -= OnSettingsPreviewDebounceTick;
            _previewDebounce.Tick += OnSettingsPreviewDebounceTick;
            _previewDebounce.Start();
        }

        private void OnSettingsPreviewDebounceTick(object? sender, EventArgs e)
        {
            _previewDebounce.Stop();
            _previewDebounce.Tick -= OnSettingsPreviewDebounceTick;
            if (_settingsWindow == null || string.IsNullOrEmpty(_lastPreviewFilePath) || !File.Exists(_lastPreviewFilePath)) return;
            var opts = _settingsWindow.BuildOptions();
            _ = RefreshPreviewWithOptions(_lastPreviewFilePath, opts);
        }

        private void OnUndoPreview(object sender, System.Windows.RoutedEventArgs e)
        {
            var previous = _previewUndoRedo.Undo();
            if (previous != null)
            {
                _currentOptions = previous;
                TxtStatus.Text = "Vorschau: Zurückgesetzt (Undo)";
                if (!string.IsNullOrEmpty(_lastPreviewFilePath) && File.Exists(_lastPreviewFilePath))
                    _ = RefreshPreviewWithOptions(_lastPreviewFilePath, _currentOptions);
            }
        }

        private void OnRedoPreview(object sender, System.Windows.RoutedEventArgs e)
        {
            var next = _previewUndoRedo.Redo();
            if (next != null)
            {
                _currentOptions = next;
                TxtStatus.Text = "Vorschau: Wiederhergestellt (Redo)";
                if (!string.IsNullOrEmpty(_lastPreviewFilePath) && File.Exists(_lastPreviewFilePath))
                    _ = RefreshPreviewWithOptions(_lastPreviewFilePath, _currentOptions);
            }
        }

        private async Task RefreshPreviewWithOptions(string filePath, ProcessingOptions opts)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            StopAnimation();
            ImgBefore.Source = null;
            ImgAfter.Source = null;
            Bitmap? before = null;
            Bitmap? after = null;
            try
            {
                (before, after) = await Task.Run(() =>
                {
                    Bitmap? b = LoadBitmapSafe(filePath);
                    if (b == null) return ((Bitmap?)null, (Bitmap?)null);
                    Bitmap? a = _processor.ProcessSingle(filePath, opts);
                    return ((Bitmap?)b, a);
                });
                if (!string.Equals(_lastPreviewFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    before?.Dispose();
                    after?.Dispose();
                    return;
                }
                if (before == null)
                {
                    after?.Dispose();
                    TxtStatus.Text = "Datei konnte nicht geladen werden.";
                    return;
                }
                int dispW = after?.Width ?? before.Width;
                int dispH = after?.Height ?? before.Height;
                Bitmap beforeDisplay = ScaleForDisplay(before, dispW, dispH);
                const int maxDisplaySize = 512;
                ImgBefore.Source = BitmapToImageSource(beforeDisplay, maxDisplaySize);
                ImgAfter.Source = BitmapToImageSource(after, maxDisplaySize);
                int finalW = beforeDisplay.Width;
                int finalH = beforeDisplay.Height;
                int finalAw = after?.Width ?? 0;
                int finalAh = after?.Height ?? 0;
                if (ReferenceEquals(beforeDisplay, before))
                {
                    before.Dispose();
                    before = null;
                }
                else
                {
                    beforeDisplay.Dispose();
                    before.Dispose();
                }
                after?.Dispose();
                UpdatePreviewClip();
                TxtStatus.Text = "Live-Vorschau mit aktuellen Einstellungen";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindow] RefreshPreviewWithOptions Fehler: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private static Bitmap? LoadBitmapSafe(string path)
        {
            try { return new Bitmap(path); }
            catch { return null; }
        }

        private static BitmapSource BitmapToImageSource(Bitmap? bmp, int maxSize)
        {
            if (bmp == null) return BitmapSource.Create(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, new byte[4], 4);
            Bitmap display = bmp;
            bool needsDispose = maxSize > 0 && (bmp.Width > maxSize || bmp.Height > maxSize);
            if (needsDispose)
            {
                float scale = Math.Min((float)maxSize / bmp.Width, (float)maxSize / bmp.Height);
                display = new Bitmap((int)(bmp.Width * scale), (int)(bmp.Height * scale));
                try
                {
                    using var g = Graphics.FromImage(display);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(bmp, 0, 0, display.Width, display.Height);
                }
                catch { display.Dispose(); throw; }
            }
            try
            {
                BitmapData data = display.LockBits(new Rectangle(0, 0, display.Width, display.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    var bitmapSource = BitmapSource.Create(display.Width, display.Height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, data.Scan0, data.Height * data.Stride, data.Stride);
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
                finally { display.UnlockBits(data); }
            }
            finally { if (needsDispose) display.Dispose(); }
                    }

                    /// <summary>
                    /// TASK-1508: Bestimmt die Sprite-Kategorie für einen relativen Pfad (für MainWindow Preview).
                    /// </summary>
                    private static string? ResolveCategoryIdInMain(string relativePath)
                    {
                        if (string.IsNullOrEmpty(relativePath)) return null;
                        foreach (var cat in SpriteFilter.AllCategories)
                        {
                            if (SpriteFilter.MatchesCategory(relativePath, cat))
                                return cat.Id;
                        }
                        return null;
                    }

                    #endregion

                    #region Tastaturkürzel

                    private void RegisterKeyboardShortcuts()
                    {
                        // Strg+O: Eingabe-Ordner wählen
                        var openShortcut = KeyboardShortcutManager.GetShortcut("OpenInput");
                        var openCmd = new System.Windows.Input.RoutedCommand();
                        openCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(ParseKey(openShortcut.Key), ParseModifiers(openShortcut.Modifiers)));
                        this.CommandBindings.Add(new System.Windows.Input.CommandBinding(openCmd, (s, e) => BtnBrowseInput_Click(s, e)));

                        // Strg+S: Batch-Verarbeitung starten
                        var startShortcut = KeyboardShortcutManager.GetShortcut("StartProcess");
                        var startCmd = new System.Windows.Input.RoutedCommand();
                        startCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(ParseKey(startShortcut.Key), ParseModifiers(startShortcut.Modifiers)));
                        this.CommandBindings.Add(new System.Windows.Input.CommandBinding(startCmd, (s, e) => BtnProcess_Click(s, e)));

                        // F1: Über-Dialog
                        var aboutShortcut = KeyboardShortcutManager.GetShortcut("About");
                        var aboutCmd = new System.Windows.Input.RoutedCommand();
                        aboutCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(ParseKey(aboutShortcut.Key), ParseModifiers(aboutShortcut.Modifiers)));
                        this.CommandBindings.Add(new System.Windows.Input.CommandBinding(aboutCmd, (s, e) => ShowAboutDialog(s, e)));

                        // Strg+Z: Undo Vorschau-Einstellungen
                        var undoCmd = new System.Windows.Input.RoutedCommand();
                        undoCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Z, System.Windows.Input.ModifierKeys.Control));
                        this.CommandBindings.Add(new System.Windows.Input.CommandBinding(undoCmd, (s, e) => OnUndoPreview(s, e)));

                        // Strg+Y: Redo Vorschau-Einstellungen
                        var redoCmd = new System.Windows.Input.RoutedCommand();
                        redoCmd.InputGestures.Add(new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Y, System.Windows.Input.ModifierKeys.Control));
                        this.CommandBindings.Add(new System.Windows.Input.CommandBinding(redoCmd, (s, e) => OnRedoPreview(s, e)));
                    }

                    private static System.Windows.Input.Key ParseKey(string keyName)
                    {
                        return keyName switch
                        {
                            "O" => System.Windows.Input.Key.O,
                            "S" => System.Windows.Input.Key.S,
                            "F1" => System.Windows.Input.Key.F1,
                            "F2" => System.Windows.Input.Key.F2,
                            "F3" => System.Windows.Input.Key.F3,
                            "F4" => System.Windows.Input.Key.F4,
                            "F5" => System.Windows.Input.Key.F5,
                            "F6" => System.Windows.Input.Key.F6,
                            "F7" => System.Windows.Input.Key.F7,
                            "F8" => System.Windows.Input.Key.F8,
                            "F9" => System.Windows.Input.Key.F9,
                            "F10" => System.Windows.Input.Key.F10,
                            "F11" => System.Windows.Input.Key.F11,
                            "F12" => System.Windows.Input.Key.F12,
                            "A" => System.Windows.Input.Key.A,
                            "B" => System.Windows.Input.Key.B,
                            "C" => System.Windows.Input.Key.C,
                            "D" => System.Windows.Input.Key.D,
                            "E" => System.Windows.Input.Key.E,
                            "F" => System.Windows.Input.Key.F,
                            "G" => System.Windows.Input.Key.G,
                            "H" => System.Windows.Input.Key.H,
                            "I" => System.Windows.Input.Key.I,
                            "J" => System.Windows.Input.Key.J,
                            "K" => System.Windows.Input.Key.K,
                            "L" => System.Windows.Input.Key.L,
                            "M" => System.Windows.Input.Key.M,
                            "N" => System.Windows.Input.Key.N,
                            "P" => System.Windows.Input.Key.P,
                            "Q" => System.Windows.Input.Key.Q,
                            "R" => System.Windows.Input.Key.R,
                            "T" => System.Windows.Input.Key.T,
                            "U" => System.Windows.Input.Key.U,
                            "V" => System.Windows.Input.Key.V,
                            "W" => System.Windows.Input.Key.W,
                            "X" => System.Windows.Input.Key.X,
                            "Y" => System.Windows.Input.Key.Y,
                            "Z" => System.Windows.Input.Key.Z,
                            _ => System.Windows.Input.Key.O,
                        };
                    }

                    private static System.Windows.Input.ModifierKeys ParseModifiers(string modifiers)
                    {
                        var result = System.Windows.Input.ModifierKeys.None;
                        if (modifiers.Contains("Control")) result |= System.Windows.Input.ModifierKeys.Control;
                        if (modifiers.Contains("Shift")) result |= System.Windows.Input.ModifierKeys.Shift;
                        if (modifiers.Contains("Alt")) result |= System.Windows.Input.ModifierKeys.Alt;
                        return result;
                    }

                    private void ShowAboutDialog(object sender, System.Windows.RoutedEventArgs e)
                    {
                        var about = new AboutWindow
                        {
                            Owner = this
                        };
                        about.ShowDialog();
                    }

                    #endregion
                }
            }
