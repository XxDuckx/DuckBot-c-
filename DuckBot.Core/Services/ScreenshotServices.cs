using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DuckBot.Core.Emu;

namespace DuckBot.Core.Services
{
    public static class ScreenshotService
    {
        // Placeholder bitmap fallback when no ADB screenshot is available
        public static BitmapSource GeneratePlaceholder(string caption)
        {
            int w = 480, h = 270, dpi = 96;
            var wb = new WriteableBitmap(w, h, dpi, dpi, PixelFormats.Bgra32, null);
            wb.Lock();
            unsafe
            {
                var p = (int*)wb.BackBuffer.ToPointer();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int idx = y * wb.BackBufferStride / 4 + x;
                        byte shade = (byte)(x * 255 / w);
                        p[idx] = unchecked((int)0xFF000000 | (shade << 16) | (shade << 8) | 0x20);
                    }
                }
            }
            wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, w, h));
            wb.Unlock();
            return wb;
        }

        public static BitmapSource CaptureOrPlaceholder(string instance, string caption)
        {
            var shot = Capture(instance);
            return shot ?? GeneratePlaceholder(caption);
        }

        public static BitmapSource? Capture(string instance)
        {
            if (!AdbService.CaptureRawScreenshot(instance, out var data) || data.Length == 0)
                return null;

            try
            {
                using var ms = new MemoryStream(data);
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch
            {
                return null;
            }
        }
    }
}