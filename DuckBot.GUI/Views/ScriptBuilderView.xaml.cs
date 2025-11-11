using System.Windows;
using System.Windows.Controls;
using DuckBot.Data.Models;
using DuckBot.Core.Scripts;

namespace DuckBot.GUI.Views
{
    public partial class ScriptBuilderView : UserControl
    {
        private ScriptModel _current = new();

        public ScriptBuilderView()
        {
            InitializeComponent();
            LoadActions();
        }

        private void LoadActions()
        {
            ActionsLibrary.Items.Clear();
            foreach (var key in DuckBot.Data.Templates.StepTemplates.Library.Keys)
                ActionsLibrary.Items.Add(key);
        }

        // TODO: handle drag-drop to StepSequence
        // TODO: implement Inspector dynamic controls
        // TODO: handle Save, Load, Export, and Test Run buttons
        // TODO: integrate CropperWindow & CoordinatePickerWindow
    }
}
