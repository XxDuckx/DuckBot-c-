using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Scripts;
using DuckBot.Data.Templates;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DuckBot.GUI.Views
{
    public partial class ScriptBuilderView : UserControl
    {
        private sealed class ActionTemplateItem
        {
            public required string Type { get; init; }
            public required string DisplayName { get; init; }
            public required string Category { get; init; }
            public required string Description { get; init; }
            public required Dictionary<string, object> Defaults { get; init; }
            public required Dictionary<string, string> ParameterHints { get; init; }
            public override string ToString() => DisplayName;
        }

        private readonly ObservableCollection<ScriptStep> _steps = new();
        private readonly ObservableCollection<ScriptVariable> _variables = new();
        private readonly ObservableCollection<ActionTemplateItem> _actionTemplates = new();
        private readonly Dictionary<string, string> _scriptPaths = new(StringComparer.OrdinalIgnoreCase);

        private ScriptModel _current = new();
        private ScriptStep? _selectedStep;
        private ScriptStep? _draggedStep;
        private Point _dragStart;
        private string? _currentPath;

        public ScriptBuilderView()
        {
            InitializeComponent();

            // Lists
            StepSequence.ItemsSource = _steps;
            ActionsLibrary.ItemsSource = _actionTemplates;

            // Initial data
            LoadGames();
            LoadActions(null);

            // Optional: if your XAML has these events declared by name, they’re here:
            StepSequence.PreviewMouseLeftButtonDown += StepSequence_PreviewMouseLeftButtonDown;
            StepSequence.PreviewMouseMove += StepSequence_PreviewMouseMove;
            StepSequence.Drop += StepSequence_Drop;
            StepSequence.SelectionChanged += StepSequence_SelectionChanged;
            ActionsLibrary.MouseDoubleClick += ActionsLibrary_DoubleClick;
        }

        // -----------------------------
        // Games / Actions / Scripts
        // -----------------------------

        private void LoadGames()
        {
            var games = new List<string>();

            if (Directory.Exists("Games"))
                foreach (var dir in Directory.GetDirectories("Games"))
                    games.Add(Path.GetFileName(dir));

            var scriptsRoot = Path.Combine("data", "scripts");
            if (Directory.Exists(scriptsRoot))
                foreach (var dir in Directory.GetDirectories(scriptsRoot))
                    games.Add(Path.GetFileName(dir));

            if (!games.Any())
                games.Add("West Game");

            GameSelect.ItemsSource = games.Distinct().OrderBy(x => x).ToList();
            GameSelect.SelectedItem = GameSelect.Items.Cast<string>().First();
        }

        private void LoadActions(string? game)
        {
            if (!string.IsNullOrWhiteSpace(game))
                StepTemplates.EnsureGameTemplates(game);

            _actionTemplates.Clear();

            foreach (var info in StepTemplates.Library.Values
                         .OrderBy(t => t.Category)
                         .ThenBy(t => t.DisplayName))
            {
                _actionTemplates.Add(new ActionTemplateItem
                {
                    Type = info.Type,
                    DisplayName = info.DisplayName,
                    Category = info.Category,
                    Description = info.Description,
                    Defaults = info.Defaults,
                    ParameterHints = info.ParameterHints
                });
            }

            // Group by Category in UI
            if (_actionTemplates.Count > 0 &&
                CollectionViewSource.GetDefaultView(_actionTemplates) is ListCollectionView view)
            {
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ActionTemplateItem.Category)));
            }
        }

        private void LoadScripts(string game)
        {
            LoadActions(game);

            ScriptSelect.Items.Clear();
            _scriptPaths.Clear();

            var dir = ScriptIO.GetGameDirectory(game);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
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
            foreach (var v in model.Variables)
                _variables.Add(new ScriptVariable
                {
                    Key = v.Key,
                    Default = v.Default,
                    Options = v.Options,
                    Prompt = v.Prompt,
                    Required = v.Required
                });

            RebuildVariables();
            StepSequence.Items.Refresh();
            RefreshInspector();
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

        // -----------------------------
        // Toolbar Buttons
        // -----------------------------

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var game = GameSelect.SelectedItem as string ?? "West Game";
            _currentPath = null;
            ResetEditor(new ScriptModel { Game = game, Name = "New Script" });
            ScriptSelect.SelectedItem = null;
            UpdateStatus("New script.");
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

            var dlg = new SaveFileDialog
            {
                Filter = "DuckBot Script (*.json)|*.json",
                InitialDirectory = Path.GetFullPath(ScriptIO.GetGameDirectory(game)),
                FileName = $"{_current.Name}.json"
            };

            if (dlg.ShowDialog() == true)
            {
                WriteCurrentScript(game, dlg.FileName);
                _currentPath = dlg.FileName;
                _current.Name = Path.GetFileNameWithoutExtension(dlg.FileName);

                if (!_scriptPaths.ContainsKey(_current.Name))
                    ScriptSelect.Items.Add(_current.Name);

                _scriptPaths[_current.Name] = dlg.FileName;
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

            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            });

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_steps.Count == 0)
            {
                MessageBox.Show("No steps to export.", "DuckBot");
                return;
            }

            var model = BuildModel();
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            });

            var save = new SaveFileDialog
            {
                Filter = "JSON File (*.json)|*.json",
                FileName = $"{model.Name}.json"
            };
            if (save.ShowDialog() == true)
            {
                File.WriteAllText(save.FileName, json);
                UpdateStatus($"Exported script to {save.FileName}");
            }
        }

        private void TestRun_Click(object sender, RoutedEventArgs e)
        {
            var model = BuildModel();
            MessageBox.Show($"Test running '{model.Name}' with {model.Steps.Count} steps.", "DuckBot");
        }

        // -----------------------------
        // Actions Library → Steps
        // -----------------------------

        private void ActionsLibrary_DoubleClick(object? sender, MouseButtonEventArgs e)
        {
            if (ActionsLibrary.SelectedItem is ActionTemplateItem template)
            {
                var step = CreateStep(template);
                _steps.Add(step);
                StepSequence.SelectedItem = step;
                UpdateStatus($"Added step {template.DisplayName}.");
            }
        }

        private static ScriptStep CreateStep(ActionTemplateItem template)
        {
            var step = new ScriptStep { Type = template.Type };
            step.ApplyDefaults(template.Defaults);
            return step;
        }

        // -----------------------------
        // Step Inspector
        // -----------------------------

        private void StepSequence_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _selectedStep = StepSequence.SelectedItem as ScriptStep;
            RefreshInspector();
        }

        private void RefreshInspector()
        {
            InspectorContent.Children.Clear();

            if (_selectedStep == null)
            {
                InspectorContent.Children.Add(new TextBlock
                {
                    Text = "Select a step to edit.",
                    Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush")
                });
                return;
            }

            var templateInfo = StepTemplates.Library.TryGetValue(_selectedStep.Type, out var info) ? info : null;
            var template = templateInfo?.Defaults ?? _selectedStep.Params;
            var hints = templateInfo?.ParameterHints;

            foreach (var kv in template)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };

                var label = new TextBlock { Text = kv.Key, Width = 120, VerticalAlignment = VerticalAlignment.Center };
                if (hints != null && hints.TryGetValue(kv.Key, out var hint))
                    label.ToolTip = hint;

                row.Children.Add(label);

                FrameworkElement editor;

                switch (kv.Value)
                {
                    case bool b:
                        var chk = new CheckBox { IsChecked = _selectedStep.GetValue(kv.Key, b) };
                        chk.Checked += (_, _) => SetStepParam(_selectedStep, kv.Key, true);
                        chk.Unchecked += (_, _) => SetStepParam(_selectedStep, kv.Key, false);
                        editor = chk;
                        break;

                    case int or long or float or double:
                        var initText = _selectedStep[kv.Key]?.ToString() ?? Convert.ToDouble(kv.Value).ToString("0.##");
                        var num = new TextBox { Width = 160, Text = initText };
                        num.LostFocus += (_, _) =>
                        {
                            if (double.TryParse(num.Text, out var val))
                                SetStepParam(_selectedStep, kv.Key, val);
                            else if (!string.IsNullOrWhiteSpace(num.Text))
                                SetStepParam(_selectedStep, kv.Key, num.Text.Trim());
                            else
                                SetStepParam(_selectedStep, kv.Key, null!);
                        };
                        editor = num;
                        break;

                    default:
                        var box = new TextBox
                        {
                            Width = 220,
                            Text = _selectedStep.GetValue<string>(kv.Key, kv.Value?.ToString() ?? string.Empty)
                        };
                        box.LostFocus += (_, _) => SetStepParam(_selectedStep, kv.Key, box.Text);

                        if (IsImageParameter(kv.Key))
                        {
                            var container = new StackPanel { Orientation = Orientation.Horizontal };
                            container.Children.Add(box);

                            var browse = new Button { Content = "Browse", Width = 80, Margin = new Thickness(8, 0, 0, 0) };
                            browse.Click += (_, _) => BrowseForImage(box, kv.Key);

                            var crop = new Button { Content = "Crop", Width = 70, Margin = new Thickness(6, 0, 0, 0) };
                            crop.Click += (_, _) => CropImage(box, kv.Key);

                            container.Children.Add(browse);
                            container.Children.Add(crop);
                            editor = container;
                        }
                        else
                        {
                            editor = box;
                        }
                        break;
                }

                row.Children.Add(editor);
                InspectorContent.Children.Add(row);
            }

            var advanced = new Button { Content = "Advanced...", Width = 120, Margin = new Thickness(0, 12, 0, 0) };
            advanced.Click += (_, _) => EditAdvancedStep(_selectedStep);
            InspectorContent.Children.Add(advanced);
        }

        private void SetStepParam(ScriptStep step, string key, object? value)
        {
            step[key] = value;
            StepSequence.Items.Refresh();
        }

        private void EditAdvancedStep(ScriptStep step)
        {
            var editor = new ScriptStepEditor
            {
                Owner = Window.GetWindow(this)
            };
            editor.LoadStep(step);

            if (editor.ShowDialog() == true)
            {
                StepSequence.Items.Refresh();
                RefreshInspector();
            }
        }

        // -----------------------------
        // Step list ops
        // -----------------------------

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
                int i = _steps.IndexOf(step);
                if (i > 0) _steps.Move(i, i - 1);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (StepSequence.SelectedItem is ScriptStep step)
            {
                int i = _steps.IndexOf(step);
                if (i >= 0 && i < _steps.Count - 1) _steps.Move(i, i + 1);
            }
        }

        private void StepSequence_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _draggedStep = (e.OriginalSource as FrameworkElement)?.DataContext as ScriptStep ?? StepSequence.SelectedItem as ScriptStep;
        }

        private void StepSequence_PreviewMouseMove(object? sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedStep != null)
            {
                var pos = e.GetPosition(null);
                if (Math.Abs(pos.X - _dragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(pos.Y - _dragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(StepSequence, _draggedStep, DragDropEffects.Move);
                }
            }
        }

        private void StepSequence_Drop(object? sender, DragEventArgs e)
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

        // -----------------------------
        // Variables
        // -----------------------------

        private void AddVariable_Click(object sender, RoutedEventArgs e)
        {
            var v = new ScriptVariable { Key = $"var{_variables.Count + 1}", Default = string.Empty };
            _variables.Add(v);
            RebuildVariables();
        }

        private void RebuildVariables()
        {
            VariablesHost.Children.Clear();

            foreach (var v in _variables.ToList())
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };

                var keyBox = new TextBox { Width = 120, Text = v.Key };
                keyBox.LostFocus += (_, _) => v.Key = keyBox.Text;

                var defaultBox = new TextBox { Width = 160, Text = v.Default, Margin = new Thickness(8, 0, 8, 0) };
                defaultBox.LostFocus += (_, _) => v.Default = defaultBox.Text;

                var remove = new Button { Content = "Remove", Width = 80 };
                remove.Click += (_, _) =>
                {
                    _variables.Remove(v);
                    RebuildVariables();
                };

                row.Children.Add(keyBox);
                row.Children.Add(defaultBox);
                row.Children.Add(remove);
                VariablesHost.Children.Add(row);
            }
        }

        // -----------------------------
        // Bottom helpers
        // -----------------------------

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

        // -----------------------------
        // XAML event hooks
        // -----------------------------

        private void GameSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameSelect.SelectedItem is string game)
                LoadScripts(game);
        }

        private void ScriptSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScriptSelect.SelectedItem is string name &&
                _scriptPaths.TryGetValue(name, out var path))
            {
                var model = ScriptIO.Load(path);
                _currentPath = path;
                ResetEditor(model);
                UpdateStatus($"Loaded '{name}'.");
            }
        }

        // -----------------------------
        // Image helpers
        // -----------------------------

        private static bool IsImageParameter(string key) =>
            key.Contains("image", StringComparison.OrdinalIgnoreCase) ||
            key.EndsWith("Path", StringComparison.OrdinalIgnoreCase);

        private void BrowseForImage(TextBox target, string key)
        {
            if (_selectedStep == null) return;
            if (!EnsureGameSelected(out var game)) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
                InitialDirectory = Path.GetFullPath(ImageManager.GetImageDir(game))
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imported = ImportImage(dialog.FileName, game);
                    target.Text = imported;
                    SetStepParam(_selectedStep, key, imported);
                    UpdateStatus($"Linked image '{imported}'.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to import image: {ex.Message}", "DuckBot",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CropImage(TextBox target, string key)
        {
            if (_selectedStep == null) return;
            if (!EnsureGameSelected(out var game)) return;

            var chooser = new OpenFileDialog
            {
                Title = "Select image to crop",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
                InitialDirectory = Path.GetFullPath(ImageManager.GetImageDir(game))
            };

            if (chooser.ShowDialog() != true) return;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(chooser.FileName, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                var cropper = new CropperWindow { Owner = Window.GetWindow(this) };
                cropper.LoadImage(bmp, game, Path.GetFileNameWithoutExtension(chooser.FileName));

                if (cropper.ShowDialog() == true && !string.IsNullOrEmpty(cropper.SavedFileName))
                {
                    target.Text = cropper.SavedFileName;
                    SetStepParam(_selectedStep, key, cropper.SavedFileName);
                    UpdateStatus($"Saved crop {cropper.SavedFileName}.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to crop image: {ex.Message}", "DuckBot",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string ImportImage(string path, string game)
        {
            var dir = ImageManager.GetImageDir(game);
            Directory.CreateDirectory(dir);

            var fileName = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"image_{DateTime.Now:yyyyMMddHHmmss}.png";

            // If already under the game image directory, store just the short name.
            var sourceDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
            if (string.Equals(sourceDir, Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
                return fileName;

            var target = Path.Combine(dir, fileName);
            var baseName = Path.GetFileNameWithoutExtension(target);
            var ext = Path.GetExtension(target);
            int i = 1;
            while (File.Exists(target))
            {
                target = Path.Combine(dir, $"{baseName}_{i}{ext}");
                i++;
            }

            File.Copy(path, target, overwrite: false);
            return Path.GetFileName(target);
        }

        private void PickCoords_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement coordinate picking logic here
            MessageBox.Show("Pick Coordinates clicked.");
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement screenshot capture logic here.
            MessageBox.Show("Capture Screenshot clicked.");
        }
    }
}
