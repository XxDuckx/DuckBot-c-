using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DuckBot.Core.Scripts;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Templates;
using Microsoft.Win32;

namespace DuckBot.GUI.Views
{
    public partial class ScriptBuilderView : UserControl
    {
        private readonly ObservableCollection<ScriptStep> _steps = new();
        private readonly ObservableCollection<ScriptVariable> _variables = new();
        private readonly Dictionary<string, string> _scriptPaths = new(StringComparer.OrdinalIgnoreCase);
        private ScriptModel _current = new();
        private ScriptStep? _selectedStep;
        private ScriptStep? _draggedStep;
        private Point _dragStart;
        private string? _currentPath;

        public ScriptBuilderView()
        {
            InitializeComponent();
            StepSequence.ItemsSource = _steps;
            LoadGames();
        }

        private void LoadGames()
        {
            var games = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists("Games"))
            {
                foreach (var dir in Directory.GetDirectories("Games"))
                    games.Add(Path.GetFileName(dir));
            }
            string scriptsRoot = Path.Combine("data", "scripts");
            if (Directory.Exists(scriptsRoot))
            {
                foreach (var dir in Directory.GetDirectories(scriptsRoot))
                    games.Add(Path.GetFileName(dir));
            }
            if (!games.Any()) games.Add("West Game");
            GameSelect.ItemsSource = games;
            GameSelect.SelectedItem = games.First();
        }

        private void LoadActions()
        {
            ActionsLibrary.Items.Clear();
            foreach (var key in StepTemplates.Library.Keys.OrderBy(k => k))
                ActionsLibrary.Items.Add(key);
        }

        private void LoadScripts(string game)
        {
            ScriptSelect.Items.Clear();
            _scriptPaths.Clear();
            string dir = Path.Combine("data", "scripts", game);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                _scriptPaths[name] = file;
                ScriptSelect.Items.Add(name);
            }
            if (ScriptSelect.Items.Count > 0)
                ScriptSelect.SelectedIndex = 0;
            else
                ResetEditor(new ScriptModel { Game = game, Name = "New Script" });
        }

        private void ResetEditor(ScriptModel model)
        {
            _current = model;
            _steps.Clear();
            foreach (var step in model.Steps)
                _steps.Add(step.Clone());
            _variables.Clear();
            foreach (var variable in model.Variables)
                _variables.Add(new ScriptVariable
                {
                    Key = variable.Key,
                    Default = variable.Default,
                    Options = variable.Options,
                    Prompt = variable.Prompt,
                    Required = variable.Required
                });
            RebuildVariables();
            RefreshInspector();
            UpdateStatus($"Loaded script '{model.Name}'.");
        }

        private void GameSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameSelect.SelectedItem is string game)
            {
                StepTemplates.EnsureGameTemplates(game);
                LoadActions();
                LoadScripts(game);
            }
        }

        private void ScriptSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScriptSelect.SelectedItem is string name && _scriptPaths.TryGetValue(name, out var path))
            {
                _currentPath = path;
                var model = ScriptIO.Load(path);
                if (string.IsNullOrWhiteSpace(model.Game)) model.Game = GameSelect.SelectedItem as string ?? "";
                ResetEditor(model);
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var game = GameSelect.SelectedItem as string ?? "West Game";
            _currentPath = null;
            ResetEditor(new ScriptModel { Game = game, Name = "New Script" });
            ScriptSelect.SelectedItem = null;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureGameSelected(out var game)) return;
            if (string.IsNullOrWhiteSpace(_currentPath))
            {
                SaveAs_Click(sender, e);
                return;
            }
            WriteCurrentScript(game, _currentPath);
            UpdateStatus($"Saved '{_current.Name}'.");
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureGameSelected(out var game)) return;
            var dialog = new SaveFileDialog
            {
                Filter = "DuckBot Script (*.json)|*.json",
                InitialDirectory = Path.GetFullPath(Path.Combine("data", "scripts", game)),
                FileName = $"{_current.Name}.json"
            };
            if (dialog.ShowDialog() == true)
            {
                WriteCurrentScript(game, dialog.FileName);
                _currentPath = dialog.FileName;
                _current.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
                if (!_scriptPaths.ContainsKey(_current.Name))
                    ScriptSelect.Items.Add(_current.Name);
                _scriptPaths[_current.Name] = dialog.FileName;
                ScriptSelect.SelectedItem = _current.Name;
                UpdateStatus($"Saved as '{_current.Name}'.");
            }
        }

        private void WriteCurrentScript(string game, string path)
        {
            _current.Game = game;
            _current.Steps = _steps.Select(s => s.Clone()).ToList();
            _current.Variables = _variables.Select(v => new ScriptVariable
            {
                Key = v.Key,
                Default = v.Default,
                Options = v.Options,
                Prompt = v.Prompt,
                Required = v.Required
            }).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            ScriptIO.Save(_current, path);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureGameSelected(out var game)) return;
            var dialog = new SaveFileDialog
            {
                Filter = "JavaScript File (*.js)|*.js",
                FileName = $"{_current.Name}.js"
            };
            if (dialog.ShowDialog() == true)
            {
                string js = ScriptTranspiler.Transpile(BuildModel(), null);
                File.WriteAllText(dialog.FileName, js);
                UpdateStatus($"Exported JS to {Path.GetFileName(dialog.FileName)}.");
            }
        }

        private void TestRun_Click(object sender, RoutedEventArgs e)
        {
            string js = ScriptTranspiler.Transpile(BuildModel(), null);
            LogService.Info("[ScriptBuilder] Preview output:");
            foreach (var line in js.Split(Environment.NewLine).Take(20))
                LogService.Info(line);
            UpdateStatus("Script transpiled to logs (first 20 lines).");
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            var image = ScreenshotService.GeneratePlaceholder("Script Builder Capture");
            var cropper = new CropperWindow();
            cropper.Owner = Window.GetWindow(this);
            cropper.LoadImage(image, _current.Game, "capture");
            if (cropper.ShowDialog() == true)
            {
                UpdateStatus($"Saved crop {_current.Game}/{cropper.SavedFileName}");
            }
        }

        private void PickCoords_Click(object sender, RoutedEventArgs e)
        {
            var image = ScreenshotService.GeneratePlaceholder("Coordinate Picker");
            var picker = new CoordinatePickerWindow();
            picker.Owner = Window.GetWindow(this);
            picker.LoadImage(image);
            picker.ShowDialog();
            if (picker.SelectedPoint is Point pt)
            {
                UpdateStatus($"Selected coordinates {pt.X:0}, {pt.Y:0}");
                if (_selectedStep != null)
                {
                    _selectedStep["x"] = (int)pt.X;
                    _selectedStep["y"] = (int)pt.Y;
                    RefreshInspector();
                    StepSequence.Items.Refresh();
                }
            }
        }

        private bool EnsureGameSelected(out string game)
        {
            if (GameSelect.SelectedItem is string g)
            {
                game = g;
                return true;
            }
            MessageBox.Show("Select a game first.", "DuckBot");
            game = string.Empty;
            return false;
        }
        private void ActionsLibrary_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ActionsLibrary.SelectedItem is string type)
            {
                var step = CreateStep(type);
                _steps.Add(step);
                StepSequence.SelectedItem = step;
                UpdateStatus($"Added step {type}.");
            }
        }

        private ScriptStep CreateStep(string type)
        {
            var step = new ScriptStep { Type = type };
            if (StepTemplates.Library.TryGetValue(type, out var template))
                step.ApplyDefaults(template);
            return step;
        }

        private void StepSequence_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedStep = StepSequence.SelectedItem as ScriptStep;
            RefreshInspector();
        }

        private void RefreshInspector()
        {
            InspectorContent.Children.Clear();
            if (_selectedStep == null)
            {
                InspectorContent.Children.Add(new TextBlock { Text = "Select a step to edit.", Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush") });
                return;
            }

            var template = StepTemplates.Library.TryGetValue(_selectedStep.Type, out var map) ? map : _selectedStep.Params;
            foreach (var kv in template)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                row.Children.Add(new TextBlock { Text = kv.Key, Width = 120, VerticalAlignment = VerticalAlignment.Center });
                FrameworkElement editor;
                switch (kv.Value)
                {
                    case bool b:
                        var chk = new CheckBox { IsChecked = _selectedStep.GetValue(kv.Key, b) };
                        chk.Checked += (_, _) => SetStepParam(_selectedStep, kv.Key, true);
                        chk.Unchecked += (_, _) => SetStepParam(_selectedStep, kv.Key, false);
                        editor = chk;
                        break;
                    case int or long:
                    case float or double:
                        var num = new TextBox { Width = 140, Text = _selectedStep.GetValue<double>(kv.Key, Convert.ToDouble(kv.Value)).ToString("0.##") };
                        num.LostFocus += (_, _) =>
                        {
                            if (double.TryParse(num.Text, out var val))
                                SetStepParam(_selectedStep, kv.Key, val);
                        };
                        editor = num;
                        break;
                    default:
                        var box = new TextBox { Width = 200, Text = _selectedStep.GetValue<string>(kv.Key, kv.Value?.ToString() ?? string.Empty) };
                        box.LostFocus += (_, _) => SetStepParam(_selectedStep, kv.Key, box.Text);
                        editor = box;
                        break;
                }
                row.Children.Add(editor);
                InspectorContent.Children.Add(row);
            }

            var advanced = new Button { Content = "Advanced...", Width = 110, Margin = new Thickness(0, 12, 0, 0) };
            advanced.Click += (_, _) => EditAdvancedStep(_selectedStep);
            InspectorContent.Children.Add(advanced);
        }

        private void SetStepParam(ScriptStep step, string key, object value)
        {
            step[key] = value;
            StepSequence.Items.Refresh();
        }

        private void EditAdvancedStep(ScriptStep step)
        {
            var editor = new ScriptStepEditor();
            editor.Owner = Window.GetWindow(this);
            editor.LoadStep(step);
            if (editor.ShowDialog() == true)
            {
                StepSequence.Items.Refresh();
                RefreshInspector();
            }
        }

        private void RemoveStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepSequence.SelectedItem is ScriptStep step)
            {
                _steps.Remove(step);
                RefreshInspector();
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (StepSequence.SelectedItem is ScriptStep step)
            {
                int index = _steps.IndexOf(step);
                if (index > 0) _steps.Move(index, index - 1);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (StepSequence.SelectedItem is ScriptStep step)
            {
                int index = _steps.IndexOf(step);
                if (index >= 0 && index < _steps.Count - 1) _steps.Move(index, index + 1);
            }
        }

        private void AddVariable_Click(object sender, RoutedEventArgs e)
        {
            var variable = new ScriptVariable { Key = $"var{_variables.Count + 1}", Default = string.Empty };
            _variables.Add(variable);
            RebuildVariables();
        }

        private void RebuildVariables()
        {
            VariablesHost.Children.Clear();
            foreach (var variable in _variables.ToList())
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                var keyBox = new TextBox { Width = 120, Text = variable.Key };
                keyBox.LostFocus += (_, _) => variable.Key = keyBox.Text;
                var defaultBox = new TextBox { Width = 160, Text = variable.Default, Margin = new Thickness(8, 0, 8, 0) };
                defaultBox.LostFocus += (_, _) => variable.Default = defaultBox.Text;
                var remove = new Button { Content = "Remove", Width = 80 };
                remove.Click += (_, _) =>
                {
                    _variables.Remove(variable);
                    RebuildVariables();
                };
                row.Children.Add(keyBox);
                row.Children.Add(defaultBox);
                row.Children.Add(remove);
                VariablesHost.Children.Add(row);
            }
        }
        private void StepSequence_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _draggedStep = (e.OriginalSource as FrameworkElement)?.DataContext as ScriptStep ?? StepSequence.SelectedItem as ScriptStep;
        }

        private void StepSequence_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedStep != null)
            {
                var position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(StepSequence, _draggedStep, DragDropEffects.Move);
                }
            }
        }

        private void StepSequence_Drop(object sender, DragEventArgs e)
        {
            if (_draggedStep == null) return;
            var target = (e.OriginalSource as FrameworkElement)?.DataContext as ScriptStep;
            if (target == null || target == _draggedStep) return;
            int oldIndex = _steps.IndexOf(_draggedStep);
            int newIndex = _steps.IndexOf(target);
            if (oldIndex >= 0 && newIndex >= 0)
            {
                _steps.Move(oldIndex, newIndex);
                StepSequence.SelectedItem = _draggedStep;
            }
            _draggedStep = null;
        }

        private ScriptModel BuildModel()
        {
            return new ScriptModel
            {
                Name = _current.Name,
                Game = GameSelect.SelectedItem as string ?? _current.Game,
                Author = _current.Author,
                Steps = _steps.Select(s => s.Clone()).ToList(),
                Variables = _variables.Select(v => new ScriptVariable
                {
                    Key = v.Key,
                    Default = v.Default,
                    Options = v.Options,
                    Prompt = v.Prompt,
                    Required = v.Required
                }).ToList()
            };
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}