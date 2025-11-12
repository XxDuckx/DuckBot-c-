using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DuckBot.GUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DuckBot.GUI.Views
{
    public partial class MyBotsView : UserControl
    {
        private readonly BotViewModel _vm;

        public MyBotsView()
        {
            InitializeComponent();

            // resolve the viewmodel from host
            var host = ((App)Application.Current).Host;
            _vm = host.Services.GetRequiredService<BotViewModel>();
            DataContext = _vm;

            // rebuild variables panel when selection changes
            BotGrid.SelectionChanged += (_, _) => RebuildVariables();
        }

        private void RebuildVariables()
        {
            VariablesHost.Children.Clear();
            if (BotGrid.SelectedItem is BotEntry selected)
            {
                foreach (var v in selected.Script.Variables ?? new System.Collections.Generic.List<DuckBot.Data.Models.ScriptVariable>())
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                    var keyBox = new TextBox { Width = 120, Text = v.Key ?? string.Empty };
                    keyBox.LostFocus += (_, _) => v.Key = keyBox.Text;
                    var defaultBox = new TextBox { Width = 160, Text = v.Default ?? string.Empty, Margin = new Thickness(8, 0, 8, 0) };
                    defaultBox.LostFocus += (_, _) => v.Default = defaultBox.Text;
                    var remove = new Button { Content = "Remove", Width = 80 };
                    remove.Click += (_, _) =>
                    {
                        selected.Script.Variables.Remove(v);
                        RebuildVariables();
                    };
                    row.Children.Add(keyBox);
                    row.Children.Add(defaultBox);
                    row.Children.Add(remove);
                    VariablesHost.Children.Add(row);
                }

                var add = new Button { Content = "Add variable", Margin = new Thickness(0, 8, 0, 0) };
                add.Click += (_, _) =>
                {
                    selected.Script.Variables.Add(new DuckBot.Data.Models.ScriptVariable { Key = $"var{selected.Script.Variables.Count + 1}", Default = "" });
                    RebuildVariables();
                };
                VariablesHost.Children.Add(add);
            }
        }
    }
}