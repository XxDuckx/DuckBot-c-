using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views
{
    public partial class LogsView : UserControl
    {
        public LogsView()
        {
            InitializeComponent();
            foreach (var (msg, lvl) in LogService.GetBuffered())
                AddLine(msg, lvl);

            LogService.OnLog += AddLine;
        }

        private void AddLine(string msg, LogService.Level lvl)
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
                        LogService.Level.Warn => Brushes.Yellow,
                        LogService.Level.Error => Brushes.IndianRed,
                        _ => Brushes.White
                    }
                };
                LogPanel.Children.Add(tb);
                ScrollBox.ScrollToEnd();
            });
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            LogPanel.Children.Clear();
        }
    }
}
