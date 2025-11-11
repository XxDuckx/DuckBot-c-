using System.IO;
using System.Windows.Media.Imaging;

namespace DuckBot.Core.Scripts
{
    public static class ImageManager
    {
        public static string GetImageDir(string game)
        {
            string dir = Path.Combine("Games", game, "images");
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

        // TODO: Add load & thumbnail preview helpers
    }
}
