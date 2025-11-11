using System.Windows;
using System.Windows.Controls;

namespace DuckBot.GUI.Views
{
    public partial class LoginView : UserControl
    {
        private readonly MainWindow _main;

        public LoginView(MainWindow main)
        {
            InitializeComponent();
            _main = main;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string user = UsernameBox.Text;
            string pass = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Enter username and password", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Fake login for now
           
        }
    }
}
