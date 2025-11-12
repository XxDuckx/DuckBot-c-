using DuckBot.Core.Emu;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;
using DuckBot.Data.Scripts;
using DuckBot.Scripting;
using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace DuckBot.Core.Scripting
{
    public sealed class CvBridge : IScriptBridge, IDisposable
    {
        private static readonly ConcurrentDictionary<string, Mat> TemplateCache = new();

        private readonly string _instance;
        private readonly string _game;
        private readonly IAdbService _adbService;
        private readonly IAppLogger _logger;
        private bool _disposed;

        public string Name => "cv";

        public CvBridge(string instance, string game, IAdbService adbService, IAppLogger logger)
        {
            _instance = instance;
            _game = game;
            _adbService = adbService;
            _logger = logger;
        }

        public bool find(string imagePath, double confidence = 0.9)
        {
            using var screenshot = CaptureFrame();
            using var template = LoadTemplate(imagePath);
            if (screenshot is null || template is null)
            {
                _logger.Warn($"[{_instance}] CV find failed. Screenshot? {(screenshot is null ? "no" : "yes")}, template? {(template is null ? "no" : "yes")}.");
                return false;
            }

            using var result = new Mat();
            Cv2.MatchTemplate(screenshot, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal >= confidence;
        }

        private Mat? CaptureFrame()
        {
            var (success, data) = _adbService.CaptureRawScreenshotAsync(_instance).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!success || data.Length == 0)
            {
                return null;
            }

            try
            {
                return Cv2.ImDecode(data, ImreadModes.Color);
            }
            catch (Exception ex)
            {
                _logger.Error($"[{_instance}] Failed to decode screenshot: {ex.Message}");
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
                _logger.Warn($"Template image '{path}' not found.");
                return null;
            }

            try
            {
                var cached = TemplateCache.GetOrAdd(path, key => Cv2.ImRead(key, ImreadModes.Color));
                return cached?.Clone();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load template '{path}': {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}