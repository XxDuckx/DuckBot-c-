using DuckBot.Core.Emu;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;
using DuckBot.Scripting;

namespace DuckBot.Core.Scripting
{
    public sealed class AdbBridge : IScriptBridge
    {
        private readonly string _instance;
        private readonly string _botName;
        private readonly IAdbService _adbService;
        private readonly IAppLogger _logger;

        public string Name => "adb";

        public AdbBridge(string instance, string botName, IAdbService adbService, IAppLogger logger)
        {
            _instance = instance;
            _botName = botName;
            _adbService = adbService;
            _logger = logger;
        }

        public bool tap(int x, int y)
        {
            var ok = _adbService.TapAsync(_instance, x, y).GetAwaiter().GetResult();
            if (!ok) _logger.Warn($"[{_botName}] tap failed at {x},{y} on {_instance}");
            return ok;
        }

        public bool swipe(int x1, int y1, int x2, int y2, int duration = 300)
        {
            var ok = _adbService.SwipeAsync(_instance, x1, y1, x2, y2, duration).GetAwaiter().GetResult();
            if (!ok) _logger.Warn($"[{_botName}] swipe failed on {_instance}");
            return ok;
        }

        public bool inputText(string text)
        {
            var ok = _adbService.InputTextAsync(_instance, text).GetAwaiter().GetResult();
            if (!ok) _logger.Warn($"[{_botName}] inputText failed on {_instance}");
            return ok;
        }

        public bool keyEvent(int code)
            => _adbService.KeyEventAsync(_instance, code).GetAwaiter().GetResult();

        public bool shell(string command)
            => _adbService.ShellAsync(_instance, command).GetAwaiter().GetResult();
    }
}