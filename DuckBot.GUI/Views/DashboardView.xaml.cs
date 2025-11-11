using System.Windows;
using System.Windows.Controls;

namespace DuckBot.GUI.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly string _username;

        public DashboardView(string username)
        {
            InitializeComponent();
            _username = username;
            WelcomeText.Text = $"Welcome, {_username}";
            LoadInstances();
        }

        private void LoadInstances()
        {
            // Placeholder for LDPlayer detection
            InstanceList.Items.Clear();
            InstanceList.Items.Add("LDPlayer-1 (Port 5555)");
            InstanceList.Items.Add("LDPlayer-2 (Port 5557)");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInstances();
            MessageBox.Show("Instance list refreshed!", "DuckBot");
        }
    }
}
