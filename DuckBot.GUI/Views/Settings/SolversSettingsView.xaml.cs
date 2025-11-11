using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views.Settings
{
    public partial class SolversSettingsView : UserControl
    {
        public SolversSettingsView()
        {
            InitializeComponent();
            LoadValues();
        }

        private void LoadValues()
        {
            var s = SettingsManager.Current.Solvers;
            AdCloser.IsChecked = s.GameAdCloser;
            LdCrashCloser.IsChecked = s.LdStoreCrashCloser;
            CaptchaSolver.IsChecked = s.CaptchaSolver;
            GameLoading.IsChecked = s.GameLoadingSolver;
            MessageSolver.IsChecked = s.MessageSolver;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.Solvers;
            s.GameAdCloser = AdCloser.IsChecked == true;
            s.LdStoreCrashCloser = LdCrashCloser.IsChecked == true;
            s.CaptchaSolver = CaptchaSolver.IsChecked == true;
            s.GameLoadingSolver = GameLoading.IsChecked == true;
            s.MessageSolver = MessageSolver.IsChecked == true;
            SettingsManager.Save();
            SolverService.ApplySettings(s);
            MessageBox.Show("Solver settings saved.", "DuckBot");
        }
    }
}
