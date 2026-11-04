using System.ComponentModel;

namespace RoSpriteEnhancer
{
    public class FileItem : INotifyPropertyChanged
    {
        private bool _done;

        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";

        public bool Done
        {
            get => _done;
            set { _done = value; OnPropertyChanged(nameof(Done)); OnPropertyChanged(nameof(DoneVisibility)); }
        }

        public System.Windows.Visibility DoneVisibility =>
            _done ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
