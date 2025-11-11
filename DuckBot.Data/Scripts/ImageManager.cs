using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuckBot.Core.Scripts
{
    public static class ImageManager
    {
        public static string GetImageDir(string game)
        {
            string dir = Path.Combine("Games", game.Replace(" ", string.Empty), "images");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string SaveCrop(BitmapSource bmp, string game, string baseName)
        {
            string dir = GetImageDir(game);
            string file = Path.Combine(dir, $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using var fs = new FileStream(file, FileMode.Create);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(fs);

            return Path.GetFileName(file);
        }

        public static IEnumerable<string> EnumerateImages(string game)
        {
            string dir = GetImageDir(game);
            return Directory.Exists(dir)
                ? Directory.EnumerateFiles(dir, "*.png").Select(Path.GetFileName)!
                : Enumerable.Empty<string>();
        }

        public static BitmapSource? LoadImage(string game, string fileName)
        {
            string path = Path.Combine(GetImageDir(game), fileName);
            if (!File.Exists(path)) return null;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource frame = decoder.Frames[0];
            frame.Freeze();
            return frame;
        }

        public static BitmapSource? LoadThumbnail(string game, string fileName, int maxWidth = 220)
        {
            var source = LoadImage(game, fileName);
            if (source == null) return null;
            double scale = source.PixelWidth > maxWidth ? maxWidth / (double)source.PixelWidth : 1.0;
            if (scale < 1.0)
            {
                var transform = new ScaleTransform(scale, scale);
                var thumb = new TransformedBitmap(source, transform);
                thumb.Freeze();
                return thumb;
            }
            return source;
        }
    }
}