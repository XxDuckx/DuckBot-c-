using System.Windows;
using System.Windows.Controls;
using DuckBot.GUI.Views.Settings;

namespace DuckBot.GUI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            ShowGeneral();
        }

        private void ShowGeneral() => ContentArea.Content = new GeneralSettingsView();
        private void General_Click(object s, RoutedEventArgs e) => ShowGeneral();
        private void Advanced_Click(object s, RoutedEventArgs e) => ContentArea.Content = new AdvancedSettingsView();
        private void Solvers_Click(object s, RoutedEventArgs e) => ContentArea.Content = new SolversSettingsView();
        private void Repository_Click(object s, RoutedEventArgs e) => ContentArea.Content = new RepositorySettingsView();
        private void Backups_Click(object s, RoutedEventArgs e) => ContentArea.Content = new BackupsSettingsView();
    }
}
