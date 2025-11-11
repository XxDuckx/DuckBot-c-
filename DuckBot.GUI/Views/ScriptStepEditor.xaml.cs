using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using DuckBot.Data.Models;

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
            ParamBox.Text = JsonSerializer.Serialize(step.Params, _json);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_step == null)
            {
                DialogResult = false;
                return;
            }

            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, object>>(ParamBox.Text, _json);
                if (map != null)
                {
                    _step.Params = map;
                    DialogResult = true;
                    Close();
                    return;
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Invalid JSON: {ex.Message}", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show("Unable to parse parameters.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
