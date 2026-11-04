using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RoSpriteEnhancer.Views.Windows
{
    /// <summary>
    /// Ein einzelner Schritt in einem Tutorial.
    /// </summary>
    public class TutorialStep : INotifyPropertyChanged
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ImagePath { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Ein komplettes Tutorial mit mehreren Schritten.
    /// </summary>
    public class TutorialItem : INotifyPropertyChanged
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<TutorialStep> Steps { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Hilfe & Tutorials Fenster - zeigt Schritt-fuer-Schritt Anleitungen
    /// für neue Benutzer der RoSpriteEnhancer Anwendung.
    /// </summary>
    public partial class HelpWindow : Window
    {
        private readonly ObservableCollection<TutorialItem> _tutorials = new();

        public HelpWindow()
        {
            InitializeComponent();
            TutorialList.ItemsSource = _tutorials;
            LoadTutorials();
        }

        private void LoadTutorials()
        {
            _tutorials.Clear();

            // Tutorial 1: Schnellstart
            var tutorial1 = new TutorialItem
            {
                Title = "Schnellstart: Erster Sprite verbessern",
                Description = "Lernen Sie die Grundlagen: Sprite laden, Einstellungen waehlen und verarbeiten.",
                Steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Title = "Schritt 1: Sprite laden",
                        Description = "Klicken Sie auf 'Ordner waehlen' oder ziehen Sie eine BMP/PNG/TGA Datei per Drag & Drop in das MainWindow."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 2: Skalierungs-Algorithmus waehlen",
                        Description = "Waehlen Sie im Hauptfenster einen Skalierungs-Algorithmus aus. Empfohlen: Scale2x für schnelle Vorschau, RealESRGAN4x für beste KI-Qualitaet."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 3: Verarbeitung starten",
                        Description = "Klicken Sie auf 'BATCH STARTEN' um den Verarbeitungsprozess zu beginnen. Die verarbeiteten Dateien werden im Ausgabe-Ordner gespeichert."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 4: Ergebnis begutachten",
                        Description = "Nutzen Sie die Side-by-Side Vorschau mit dem Blend-Slider um Original und Ergebnis zu vergleichen."
                    }
                }
            };
            _tutorials.Add(tutorial1);

            // Tutorial 2: AI-Upscaling
            var tutorial2 = new TutorialItem
            {
                Title = "KI-Upscaling mit Waifu2x / RealESRGAN",
                Description = "Nutzen Sie KI-basierte Upscaler für hochwertige Anime-Sprite-Verbes-serung.",
                Steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Title = "Schritt 1: KI-Tool installieren",
                        Description = "Gehen Sie zu den Einstellungen und klicken Sie auf 'Download Tool' neben Waifu2x oder RealESRGAN um das KI-Modell herunterzuladen."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 2: Pfad konfigurieren",
                        Description = "Tragen Sie den Pfad zur heruntergeladenen .exe Datei in das entsprechende Textfeld ein."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 3: Algorithmus waehlen",
                        Description = "Waehlen Sie in den Skalierungs-Einstellungen 'Waifu2xAnime' oder 'RealESRGAN4x' als Algorithmus aus."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 4: Verarbeiten",
                        Description = "Starten Sie die Batch-Verarbeitung. Die KI-Tools verarbeiten Ihre Sprites mit hoher Qualitaet."
                    }
                }
            };
            _tutorials.Add(tutorial2);

            // Tutorial 3: Presets
            var tutorial3 = new TutorialItem
            {
                Title = "Presets speichern und wiederverwenden",
                Description = "Speichern Sie Ihre bevorzugten Einstellungen als Preset und laden Sie sie spater wieder.",
                Steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Title = "Schritt 1: Einstellungen konfigurieren",
                        Description = "Offnen Sie die Einstellungen und konfigurieren Sie alle gewunschten Optionen (Skalierung, Schattierung, Kontur usw.)."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 2: Preset speichern",
                        Description = "Klicken Sie auf 'Preset speichern', geben Sie einen Namen ein und waehlen Sie eine Kategorie (z.B. Monster, Items, Jobs)."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 3: Preset laden",
                        Description = "Waehlen Sie ein gespeichertes Preset aus der Liste um alle Einstellungen mit einem Klick wiederherzustellen."
                    },
                    new TutorialStep
                    {
                        Title = "Schritt 4: Preset teilen",
                        Description = "Nutzen Sie die Cloud-Upload Funktion um Presets ueber GitHub Gists mit anderen Benutzern zu teilen."
                    }
                }
            };
            _tutorials.Add(tutorial3);

            // Erstes Tutorial auswaehlen
            if (_tutorials.Count > 0)
            {
                TutorialList.SelectedIndex = 0;
            }
        }

        private void TutorialList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TutorialList.SelectedItem is TutorialItem tutorial)
            {
                TutorialTitle.Text = tutorial.Title;
                TutorialDescription.Text = tutorial.Description;
                StepsList.ItemsSource = tutorial.Steps;
            }
        }
    }
}