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

        public BotEditorWindow(BotProfile bot)
        {
            InitializeComponent();
            Bot = bot;
            DataContext = this;

            // Ensure at least one script row for UX
            if (!Bot.Scripts.Any())
                Bot.Scripts.Add(new ScriptSetting { Name = "Building Upgrading" });
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            BotStore.Save(Bot);
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
    }
}
