using DuckBot.Core.Scripts;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuckBot.GUI.Views
{
    public partial class CropperWindow : Window
    {
        private Point? _dragStart;
        private Rect _selection;
        private string _game = "Default";
        private string _baseName = "crop";

        public string? SavedFileName { get; private set; }

        public CropperWindow()
        {
            InitializeComponent();
        }

        public void LoadImage(BitmapSource bitmap, string game, string baseName)
        {
            SourceImage.Source = bitmap;
            _game = game;
            _baseName = baseName;
            SelectionRect.Visibility = Visibility.Collapsed;
            SelectionInfo.Text = "Drag to select a region.";
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SourceImage.Source == null) return;
            _dragStart = e.GetPosition(OverlayCanvas);
            Canvas.SetLeft(SelectionRect, _dragStart.Value.X);
            Canvas.SetTop(SelectionRect, _dragStart.Value.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;
            OverlayCanvas.CaptureMouse();
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStart == null) return;
            var position = e.GetPosition(OverlayCanvas);
            UpdateSelection(position);
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragStart == null) return;
            OverlayCanvas.ReleaseMouseCapture();
            var position = e.GetPosition(OverlayCanvas);
            UpdateSelection(position);
            _dragStart = null;
        }

        private void UpdateSelection(Point current)
        {
            if (_dragStart == null) return;
            double x = Math.Min(current.X, _dragStart.Value.X);
            double y = Math.Min(current.Y, _dragStart.Value.Y);
            double width = Math.Abs(current.X - _dragStart.Value.X);
            double height = Math.Abs(current.Y - _dragStart.Value.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = width;
            SelectionRect.Height = height;
            _selection = new Rect(x, y, width, height);
            SelectionInfo.Text = width > 0 && height > 0
                ? $"Selection: {width:0} x {height:0}"
                : "Drag to select a region.";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SourceImage.Source is not BitmapSource bmp || _selection.Width < 1 || _selection.Height < 1)
            {
                MessageBox.Show("Create a selection before saving.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var imageRectTopLeft = SourceImage.TranslatePoint(new Point(0, 0), OverlayCanvas);
            var imageRect = new Rect(imageRectTopLeft, new Size(SourceImage.ActualWidth, SourceImage.ActualHeight));
            Rect intersection = Rect.Intersect(_selection, imageRect);
            if (intersection.IsEmpty)
            {
                MessageBox.Show("Selection is outside of the image bounds.", "DuckBot");
                return;
            }

            double scaleX = bmp.PixelWidth / imageRect.Width;
            double scaleY = bmp.PixelHeight / imageRect.Height;
            int x = (int)Math.Clamp((intersection.X - imageRect.X) * scaleX, 0, bmp.PixelWidth - 1);
            int y = (int)Math.Clamp((intersection.Y - imageRect.Y) * scaleY, 0, bmp.PixelHeight - 1);
            int w = (int)Math.Clamp(intersection.Width * scaleX, 1, bmp.PixelWidth - x);
            int h = (int)Math.Clamp(intersection.Height * scaleY, 1, bmp.PixelHeight - y);

            var cropped = new CroppedBitmap(bmp, new Int32Rect(x, y, w, h));
            cropped.Freeze();
            SavedFileName = ImageManager.SaveCrop(cropped, _game, _baseName);
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