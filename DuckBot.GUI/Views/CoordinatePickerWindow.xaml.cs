using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DuckBot.GUI.Views
{
    public partial class CoordinatePickerWindow : Window
    {
        public Point? SelectedPoint { get; private set; }
        private BitmapSource? _bitmap;

        public CoordinatePickerWindow()
        {
            InitializeComponent();
        }

        public void LoadImage(BitmapSource bitmap)
        {
            _bitmap = bitmap;
            SourceImage.Source = bitmap;
            CoordLabel.Text = "Click to select coordinates.";
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (_bitmap == null) return;
            var imagePos = GetImagePosition(e.GetPosition(SourceImage));
            if (imagePos == null) return;
            UpdateCrosshair(imagePos.Value.Display);
            CoordLabel.Text = $"Cursor: {imagePos.Value.Pixel.X:0}, {imagePos.Value.Pixel.Y:0}";
        }

        private void Image_Click(object sender, MouseButtonEventArgs e)
        {
            if (_bitmap == null) return;
            var imagePos = GetImagePosition(e.GetPosition(SourceImage));
            if (imagePos == null) return;
            SelectedPoint = new Point(imagePos.Value.Pixel.X, imagePos.Value.Pixel.Y);
            CoordLabel.Text = $"Selected: {SelectedPoint.Value.X:0}, {SelectedPoint.Value.Y:0}";
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoint == null)
            {
                MessageBox.Show("Select a coordinate first.", "DuckBot");
                return;
            }
            Clipboard.SetText($"{SelectedPoint.Value.X:0},{SelectedPoint.Value.Y:0}");
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private (Point Display, Point Pixel)? GetImagePosition(Point mousePos)
        {
            if (_bitmap == null) return null;
            var img = SourceImage;
            if (img.ActualWidth < double.Epsilon || img.ActualHeight < double.Epsilon) return null;

            double scaleX = _bitmap.PixelWidth / img.ActualWidth;
            double scaleY = _bitmap.PixelHeight / img.ActualHeight;
            if (scaleX <= 0 || scaleY <= 0) return null;

            double localX = Math.Clamp(mousePos.X, 0, img.ActualWidth);
            double localY = Math.Clamp(mousePos.Y, 0, img.ActualHeight);
            var offset = img.TranslatePoint(new Point(0, 0), Overlay);
            double displayX = offset.X + localX;
            double displayY = offset.Y + localY;
            double pixelX = Math.Clamp(localX * scaleX, 0, _bitmap.PixelWidth - 1);
            double pixelY = Math.Clamp(localY * scaleY, 0, _bitmap.PixelHeight - 1);
            return (new Point(displayX, displayY), new Point(pixelX, pixelY));
        }

        private void UpdateCrosshair(Point display)
        {
            CrosshairHorizontal.X1 = 0;
            CrosshairHorizontal.X2 = Overlay.ActualWidth;
            CrosshairHorizontal.Y1 = CrosshairHorizontal.Y2 = display.Y;

            CrosshairVertical.Y1 = 0;
            CrosshairVertical.Y2 = Overlay.ActualHeight;
            CrosshairVertical.X1 = CrosshairVertical.X2 = display.X;

            CrosshairHorizontal.Visibility = Visibility.Visible;
            CrosshairVertical.Visibility = Visibility.Visible;
        }
    }
}
