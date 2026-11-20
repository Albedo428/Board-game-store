using System.Windows;

namespace WpfApp1
{
    /// <summary>Окно справки (О программе / Как работает программа).</summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow(string title, string text)
        {
            InitializeComponent();
            Title = title;
            TitleText.Text = title.ToUpper();
            BodyText.Text = text;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
