using DuckBot.Core.Emu;
using DuckBot.Data.Scripts;
using DuckBot.Core.Services;
using DuckBot.Data.Scripts;
using DuckBot.Scripting;
using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace DuckBot.Core.Scripting
{
    public sealed class CvBridge : IScriptBridge
    {
        private static readonly ConcurrentDictionary<string, Mat> _templateCache = new();

        private readonly string _instance;
        private readonly string _game;

        public string Name => "cv";

        public CvBridge(string instance, string game)
        {
            _instance = instance;
            _game = game;
        }

        public bool find(string imagePath, double confidence = 0.9)
        {
            using var screenshot = CaptureFrame();
            using var template = LoadTemplate(imagePath);
            if (screenshot is null || template is null)
            {
                LogService.Warn($"[{_instance}] CV find failed. Screenshot? {(screenshot is null ? "no" : "yes")}, template? {(template is null ? "no" : "yes")}.");
                return false;
            }

            using var result = new Mat();
            Cv2.MatchTemplate(screenshot, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal >= confidence;
        }

        private Mat? CaptureFrame()
        {
            if (!AdbService.CaptureRawScreenshot(_instance, out var data) || data.Length == 0)
                return null;

            try
            {
                return Cv2.ImDecode(data, ImreadModes.Color);
            }
            catch (Exception ex)
            {
                LogService.Error($"[{_instance}] Failed to decode screenshot: {ex.Message}");
                return null;
            }
        }

        private Mat? LoadTemplate(string imagePath)
        {
            string path = imagePath;
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(ImageManager.GetImageDir(_game), imagePath);
            }

            if (!File.Exists(path))
            {
                LogService.Warn($"Template image '{path}' not found.");
                return null;
            }

            try
            {
                var cached = _templateCache.GetOrAdd(path, key => Cv2.ImRead(key, ImreadModes.Color));
                return cached?.Clone();
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to load template '{path}': {ex.Message}");
                return null;
            }
        }
    }
}