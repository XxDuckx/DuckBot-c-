using DuckBot.Core.Emu;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;
using DuckBot.Scripting;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using Tesseract;
using CvRect = OpenCvSharp.Rect;

namespace DuckBot.Core.Scripting
{
    public sealed class OcrBridge : IScriptBridge, IDisposable
    {
        private readonly string _instance;
        private readonly IAdbService _adbService;
        private readonly IAppLogger _logger;
        private readonly Lazy<TesseractEngine?> _engine;

        public string Name => "ocr";

        public OcrBridge(string instance, IAdbService adbService, IAppLogger logger)
        {
            _instance = instance;
            _adbService = adbService;
            _logger = logger;
            _engine = new Lazy<TesseractEngine?>(CreateEngine, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public string readText(int x, int y, int width, int height)
        {
            using var frame = CaptureFrame();
            if (frame is null) return string.Empty;

            var rect = new CvRect(x, y, width, height);
            rect = rect.Intersect(new CvRect(0, 0, frame.Width, frame.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return string.Empty;

            using var roi = new Mat(frame, rect);
            using var gray = roi.CvtColor(ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(gray, gray, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);

            var engine = _engine.Value;
            if (engine == null) return string.Empty;

            try
            {
                var buffer = gray.ImEncode(".png");
                using var pix = Pix.LoadFromMemory(buffer);
                using var page = engine.Process(pix);
                return page.GetText().Trim();
            }
            catch (Exception ex)
            {
                _logger.Error($"[{_instance}] OCR failed: {ex.Message}");
                return string.Empty;
            }
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

        private TesseractEngine? CreateEngine()
        {
            try
            {
                string root = Path.Combine(AppContext.BaseDirectory, "data", "ocr");
                string tessData = Path.Combine(root, "tessdata");
                Directory.CreateDirectory(tessData);
                if (Directory.GetFiles(tessData, "*.traineddata").Length == 0)
                {
                    _logger.Warn("No tessdata language files found. Place traineddata files in /data/ocr/tessdata for OCR support.");
                }
                return new TesseractEngine(tessData, "eng", EngineMode.Default);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialise OCR engine: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (_engine.IsValueCreated && _engine.Value is { } engine)
            {
                engine.Dispose();
            }
        }
    }
}