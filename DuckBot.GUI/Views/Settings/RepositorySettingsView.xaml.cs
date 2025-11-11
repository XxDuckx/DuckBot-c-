using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views.Settings
{
    public partial class RepositorySettingsView : UserControl
    {
        public RepositorySettingsView()
        {
            InitializeComponent();
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            TemplateList.Items.Clear();
            foreach (var t in RepositoryService.GetTemplates())
                TemplateList.Items.Add($"{t.Name} ({t.Game}) - by {t.Author}");
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => LoadTemplates();

        private void ContributeBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Template contribution coming soon.", "DuckBot");
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/XxDuckx/DuckBot",
                UseShellExecute = true
            });
        }
    }
}
