using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Infrastructure;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using DuckBot.GUI.ViewModels;

namespace DuckBot.GUI.Views
{
    public partial class BotEditorWindow : Window
    {
        public BotProfile Bot { get; }
        public ObservableCollection<MailAccount> MailAccounts { get; } = new(AccountStore.LoadAll());
        public ObservableCollection<InstanceBindingRow> InstanceBindingsView { get; } = new();

        private readonly IEmulatorService _emulatorService;
        private readonly IAppLogger _logger;

        public BotEditorWindow(BotProfile bot)
        {
            InitializeComponent();
            Bot = bot;

            AppServices.ConfigureDefaults();
            _emulatorService = AppServices.EmulatorService;
            _logger = AppServices.Logger;

            DataContext = this;

            if (!Bot.Scripts.Any())
            {
                Bot.Scripts.Add(new ScriptSetting { Name = "Building Upgrading" });
            }

            Loaded += BotEditorWindow_Loaded;
        }

        private async void BotEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshInstanceBindingsAsync();
        }

        private async Task RefreshInstanceBindingsAsync()
        {
            try
            {
                if (InstanceStatus != null)
                {
                    InstanceStatus.Text = "Detecting LDPlayer instances...";
                }

                var instances = await _emulatorService.GetInstancesAsync(true);

                foreach (var row in InstanceBindingsView)
                {
                    row.PropertyChanged -= InstanceBinding_PropertyChanged;
                }

                InstanceBindingsView.Clear();
                foreach (var inst in instances.OrderBy(i => i.InstallDisplayName).ThenBy(i => i.Name))
                {
                    var existing = Bot.InstanceBindings.FirstOrDefault(b => string.Equals(b.InstanceName, inst.Name, StringComparison.OrdinalIgnoreCase));
                    var row = new InstanceBindingRow(inst.Name, inst.InstallDisplayName, inst.IsRunning)
                    {
                        AccountId = existing?.AccountId
                    };
                    row.PropertyChanged += InstanceBinding_PropertyChanged;
                    InstanceBindingsView.Add(row);
                }

                if (InstanceStatus != null)
                {
                    InstanceStatus.Text = InstanceBindingsView.Count > 0
                        ? $"Detected {InstanceBindingsView.Count} instance(s)."
                        : "No LDPlayer instances detected.";
                }

                SyncBotBindings();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to refresh emulator instances: {ex.Message}");
                if (InstanceStatus != null)
                {
                    InstanceStatus.Text = $"Failed to refresh: {ex.Message}";
                }
            }
        }

        private void InstanceBinding_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceBindingRow.AccountId))
            {
                SyncBotBindings();
            }
        }

        private void SyncBotBindings()
        {
            Bot.InstanceBindings = InstanceBindingsView
                .Where(row => !string.IsNullOrWhiteSpace(row.AccountId))
                .Select(row => new InstanceBinding
                {
                    InstanceName = row.InstanceName,
                    AccountId = row.AccountId
                })
                .ToList();
        }

        private async void RefreshInstances_Click(object sender, RoutedEventArgs e)
        {
            await RefreshInstanceBindingsAsync();
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
            {
                Bot.Scripts.Remove(s);
            }
        }

        private void EditVars_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ScriptSetting s)
            {
                MessageBox.Show($"(Stub) Vars editor for '{s.Name}'", "DuckBot");
            }
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
            if (AccountLibraryGrid.SelectedItem is not MailAccount account)
            {
                return;
            }

            if (Bot.Accounts.Any(a => string.Equals(a.Email, account.Email, StringComparison.OrdinalIgnoreCase)))
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

        public sealed class InstanceBindingRow : INotifyPropertyChanged
        {
            private string? _accountId;

            public InstanceBindingRow(string instanceName, string emulator, bool isRunning)
            {
                InstanceName = instanceName;
                Emulator = emulator;
                IsRunning = isRunning;
            }

            public string InstanceName { get; }
            public string Emulator { get; }
            public bool IsRunning { get; }

            public string? AccountId
            {
                get => _accountId;
                set
                {
                    if (_accountId != value)
                    {
                        _accountId = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private BotEntry? _bot;

        public BotEditorWindow()
        {
            InitializeComponent();
        }

        public void LoadBot(BotEntry bot)
        {
            _bot = bot;
            DataContext = new BotEditorViewModel(bot);
            Owner = Application.Current?.MainWindow;
            ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Simple VM for the editor to provide helpers (keeps View clean)
    public class BotEditorViewModel
    {
        public BotEntry Bot { get; }
        public string ScriptJsonPreview => JsonSerializer.Serialize(Bot.Script, new JsonSerializerOptions { WriteIndented = true });

        public BotEditorViewModel(BotEntry bot) => Bot = bot;
    }
}