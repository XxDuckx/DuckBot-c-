using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuckBot.Core.Services
{
    public static class ScreenshotService
    {
        // Placeholder bitmap (replace with real ADB screencap later)
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
    }
}
