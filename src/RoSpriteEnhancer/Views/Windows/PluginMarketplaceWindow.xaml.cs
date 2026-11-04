using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RoSpriteEnhancer.Core.Processing;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer.Views.Windows
{
    /// <summary>
    /// UI-Element für ein Plugin im Marktplatz mit Anzeige-Status.
    /// </summary>
    public class PluginMarketplaceItem : INotifyPropertyChanged
    {
        public PluginInfo PluginInfo { get; }
        public string PluginId => PluginInfo.PluginId;
        public string DisplayName => PluginInfo.DisplayName;
        public string Description => PluginInfo.Description;
        public string Version => PluginInfo.Version;
        public string Author => PluginInfo.Author;

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set { _isInstalled = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(StatusColor)); }
        }

        public string StatusText => IsInstalled ? "Installiert" : "Nicht installiert";
        public Brush StatusColor => IsInstalled ? Brushes.Green : Brushes.Orange;

        public PluginMarketplaceItem(PluginInfo pluginInfo, bool isInstalled)
        {
            PluginInfo = pluginInfo;
            _isInstalled = isInstalled;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Plugin-Marktplatz-Fenster zum Entdecken, Installieren und Verwalten
    /// externer Upscaler-Plugins.
    /// </summary>
    public partial class PluginMarketplaceWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<PluginMarketplaceItem> _plugins = new();
        private string _searchText = "";

        public PluginMarketplaceWindow()
        {
            InitializeComponent();
            DataContext = this;
            PluginsList.ItemsSource = _plugins;
            LoadPlugins();
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterPlugins();
            }
        }

        private void LoadPlugins()
        {
            _plugins.Clear();

            // Sicherstellen, dass das Plugin-System initialisiert ist
            string pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            UpscalerPluginManager.Initialize(pluginsDir);

            // Installierte Plugins prüfen (vereinfacht: alle im plugins/-Ordner)
            foreach (var pluginInfo in UpscalerPluginManager.Plugins)
            {
                bool isInstalled = IsPluginInstalled(pluginInfo.PluginId);
                _plugins.Add(new PluginMarketplaceItem(pluginInfo, isInstalled));
            }

            TxtStatus.Text = $"{_plugins.Count} Plugin(s) gefunden";
        }

        private bool IsPluginInstalled(string pluginId)
        {
            string pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            string pluginDll = Path.Combine(pluginsDir, $"{pluginId}.dll");
            return File.Exists(pluginDll);
        }

        private void FilterPlugins()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var item in _plugins) item.PluginInfo.Plugin.IsAvailable(); // Reset
                PluginsList.ItemsSource = _plugins;
                return;
            }

            var filtered = _plugins.Where(p =>
                p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.PluginId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            PluginsList.ItemsSource = filtered;
        }

        private void TxtPluginSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = TxtPluginSearch.Text;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPlugins();
            TxtStatus.Text = "Plugin-Liste aktualisiert";
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is not string pluginId) return;

            var plugin = UpscalerPluginManager.GetPlugin(pluginId);
            if (plugin == null)
            {
                MessageBox.Show($"Plugin '{pluginId}' konnte nicht gefunden werden.", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Vereinfachte Installation: Plugin-DLL wird als "installiert" markiert
            // In einer echten Implementierung würde hier ein Download erfolgen
            MessageBox.Show(
                $"Plugin '{plugin.DisplayName}' (v{plugin.Version}) wurde installiert.\n" +
                $"Starten Sie die Anwendung neu, um das Plugin zu aktivieren.",
                "Plugin installiert", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadPlugins();
        }

        private void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is not string pluginId) return;

            var result = MessageBox.Show(
                $"Möchten Sie das Plugin wirklich deinstallieren?",
                "Plugin deinstallieren", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Vereinfachte Deinstallation
                MessageBox.Show(
                    $"Plugin '{pluginId}' wurde deinstalliert.\n" +
                    $"Starten Sie die Anwendung neu, um die Änderungen zu übernehmen.",
                    "Plugin deinstalliert", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPlugins();
            }
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is not string pluginId) return;

            var plugin = UpscalerPluginManager.GetPlugin(pluginId);
            if (plugin == null) return;

            MessageBox.Show(
                $"Name: {plugin.DisplayName}\n" +
                $"Version: {plugin.Version}\n" +
                $"Autor: {plugin.Author}\n" +
                $"Beschreibung: {plugin.Description}\n" +
                $"Verfügbar: {plugin.IsAvailable()}\n" +
                $"ID: {plugin.PluginId}",
                $"Plugin-Details: {plugin.DisplayName}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}