using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Data.Models;
using DuckBot.Data.Storage;

namespace DuckBot.GUI.Views
{
    public partial class BotEditorWindow : Window
    {
        public BotProfile Bot { get; }
        public ObservableCollection<MailAccount> MailAccounts { get; } = new(AccountStore.LoadAll());

        public BotEditorWindow(BotProfile bot)
        {
            InitializeComponent();
            Bot = bot;
            DataContext = this;

            if (!Bot.Scripts.Any())
                Bot.Scripts.Add(new ScriptSetting { Name = "Building Upgrading" });
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            BotStore.Save(Bot);
            AccountStore.SaveAll(MailAccounts);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddScript_Click(object sender, RoutedEventArgs e)
        {
            Bot.Scripts.Add(new ScriptSetting { Name = "New Script" });
        }

        private void RemoveScript_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptsList.SelectedItem is ScriptSetting s)
                Bot.Scripts.Remove(s);
        }

        private void EditVars_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ScriptSetting s)
                MessageBox.Show($"(Stub) Vars editor for '{s.Name}'", "DuckBot");
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            var account = new MailAccount { Label = "New Account", Enabled = true };
            MailAccounts.Add(account);
            AccountLibraryGrid.SelectedItem = account;
        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountLibraryGrid.SelectedItem is MailAccount account)
            {
                MailAccounts.Remove(account);
            }
        }

        private void SaveAccounts_Click(object sender, RoutedEventArgs e)
        {
            AccountStore.SaveAll(MailAccounts);
            MessageBox.Show("Accounts saved.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AssignAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountLibraryGrid.SelectedItem is not MailAccount account) return;
            if (Bot.Accounts.Any(a => string.Equals(a.Email, account.Email, System.StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This account is already assigned to the bot.", "DuckBot");
                return;
            }

            Bot.Accounts.Add(new AccountProfile
            {
                Username = account.Label,
                Email = account.Email,
                Pin = account.Password,
                Active = account.Enabled
            });
        }

        private void RemoveBotAccount_Click(object sender, RoutedEventArgs e)
        {
            if (BotAccountsList.SelectedItem is AccountProfile profile)
            {
                Bot.Accounts.Remove(profile);
            }
        }
    }
}