using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DuckBot.Core.Infrastructure;
using DuckBot.Core.Logging;

namespace DuckBot.GUI.Views
{
    public partial class LogsView : UserControl
    {
        private readonly List<TextBlock> _entries = new();
        private IAppLogger Logger => AppServices.Logger;

        public LogsView()
        {
            InitializeComponent();
            Loaded += (_, _) => Attach();
            Unloaded += (_, _) => Detach();
        }

        private void Attach()
        {
            Detach();
            LogPanel.Children.Clear();
            _entries.Clear();
            foreach (var entry in Logger.RecentEntries.OrderBy(e => e.Timestamp))
                AddLine(entry, false);
            Logger.EntryWritten += OnEntryWritten;
        }

        private void Detach()
        {
            Logger.EntryWritten -= OnEntryWritten;
        }

        private void OnEntryWritten(object? sender, LogEntry entry)
            => AddLine(entry, true);

        private void AddLine(LogEntry entry, bool fromEvent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var textBlock = new TextBlock
                {
                    Text = entry.ToString(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(4, 2, 4, 2),
                    Foreground = entry.Level switch
                    {
                        LogLevel.Warning => (Brush)FindResource("WarningBrush"),
                        LogLevel.Error => (Brush)FindResource("ErrorBrush"),
                        _ => (Brush)FindResource("TextBrush")
                    }
                };
                LogPanel.Children.Add(textBlock);
                _entries.Add(textBlock);
                if (_entries.Count > 5000)
                {
                    LogPanel.Children.RemoveAt(0);
                    _entries.RemoveAt(0);
                }

                if (AutoScrollToggle.IsChecked == true && fromEvent)
                    ScrollBox.ScrollToEnd();
            });
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            LogPanel.Children.Clear();
            _entries.Clear();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_entries.Count == 0)
            {
                MessageBox.Show("No log entries to copy.", "DuckBot");
                return;
            }
            var builder = new StringBuilder();
            foreach (var entry in _entries)
                builder.AppendLine(entry.Text);
            Clipboard.SetText(builder.ToString());
        }
    }
}