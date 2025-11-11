using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views
{
    public partial class LogsView : UserControl
    {
        private readonly List<TextBlock> _entries = new();

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
            foreach (var (msg, lvl) in LogService.GetBuffered())
                AddLine(msg, lvl, false);
            LogService.OnLog += AddLine;
        }

        private void Detach()
        {
            LogService.OnLog -= AddLine;
        }

        private void AddLine(string msg, LogService.Level lvl)
        {
            AddLine(msg, lvl, true);
        }

        private void AddLine(string msg, LogService.Level lvl, bool fromEvent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var tb = new TextBlock
                {
                    Text = msg,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(4, 2, 4, 2),
                    Foreground = lvl switch
                    {
                        LogService.Level.Warn => (Brush)FindResource("WarningBrush"),
                        LogService.Level.Error => (Brush)FindResource("ErrorBrush"),
                        _ => (Brush)FindResource("TextBrush")
                    }
                };
                LogPanel.Children.Add(tb);
                _entries.Add(tb);
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