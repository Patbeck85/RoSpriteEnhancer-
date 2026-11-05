using System.Windows;

namespace RoSpriteEnhancer.Views.Windows
{
    /// <summary>
    /// Einfacher Text-Eingabedialog für Preset-IDs etc.
    /// </summary>
    public partial class InputDialog : Window
    {
        public string? Result { get; private set; }

        public InputDialog(string title, string prompt, string initialValue = "")
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 12
            };
            stack.Children.Add(textBlock);

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = initialValue,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 12,
                MinHeight = 28
            };
            textBox.SelectAll();
            stack.Children.Add(textBox);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = LanguageService.GetString("msg.ok"),
                Width = 70,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            btnOk.Click += (_, _) =>
            {
                Result = textBox.Text;
                DialogResult = true;
            };
            buttonPanel.Children.Add(btnOk);

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = LanguageService.GetString("msg.cancel"),
                Width = 70,
                IsCancel = true
            };
            buttonPanel.Children.Add(btnCancel);
            stack.Children.Add(buttonPanel);

            Content = stack;

            textBox.Focus();
        }
    }
}