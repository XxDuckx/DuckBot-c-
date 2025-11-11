using DuckBot.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace DuckBot.GUI.Views
{
    public partial class ScriptStepEditor : Window
    {
        private ScriptStep? _step;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public ScriptStepEditor()
        {
            InitializeComponent();
        }

        public void LoadStep(ScriptStep step)
        {
            _step = step;
            Title = $"Edit {step.Type} Parameters";
            EditorPanel.Children.Clear();

            foreach (var kv in step.Params)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };

                row.Children.Add(new TextBlock
                {
                    Text = kv.Key + ":",
                    Width = 120,
                    VerticalAlignment = VerticalAlignment.Center
                });

                row.Children.Add(new TextBox
                {
                    Text = kv.Value?.ToString() ?? "",
                    Width = 220,
                    Tag = kv.Key
                });

                EditorPanel.Children.Add(row);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_step == null)
            {
                DialogResult = false;
                return;
            }

            var map = new Dictionary<string, object>();

            foreach (var child in EditorPanel.Children.OfType<StackPanel>())
            {
                if (child.Children[1] is TextBox box && box.Tag is string key)
                    map[key] = box.Text;
            }

            _step.Params = map;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
