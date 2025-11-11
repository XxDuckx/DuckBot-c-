using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views.Settings
{
    public partial class BackupsSettingsView : UserControl
    {
        public BackupsSettingsView()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog() { Filter = "ZIP Files (*.zip)|*.zip" };
            if (dialog.ShowDialog() == true)
                BackupPath.Text = dialog.FileName;
        }

        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("data/backups");
            string path = Path.Combine("data", "backups", $"DuckBotBackup_{DateTime.Now:yyyyMMdd_HHmm}.zip");
            BackupService.CreateBackup(path);
            MessageBox.Show($"Backup created:\n{path}", "DuckBot");
        }

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e) => BackupService.OpenBackupFolder();

        private void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(BackupPath.Text))
            {
                BackupService.RestoreBackup(BackupPath.Text);
                MessageBox.Show("Backup restored successfully.", "DuckBot");
            }
            else
            {
                MessageBox.Show("Invalid backup file path.", "DuckBot");
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Save();
            MessageBox.Show("Backup settings saved.", "DuckBot");
        }
    }
}
