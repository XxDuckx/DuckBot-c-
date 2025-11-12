using DuckBot.Core.Emu;
using DuckBot.Core.Services;
using DuckBot.Scripting;

namespace DuckBot.Core.Scripting
{
    public sealed class AdbBridge : IScriptBridge
    {
        private readonly string _instance;
        private readonly string _botName;

        public string Name => "adb";

        public AdbBridge(string instance, string botName)
        {
            _instance = instance;
            _botName = botName;
        }

        public bool tap(int x, int y)
        {
            var ok = AdbService.Tap(_instance, x, y);
            if (!ok) LogService.Warn($"[{_botName}] tap failed at {x},{y} on {_instance}");
            return ok;
        }

        public bool swipe(int x1, int y1, int x2, int y2, int duration = 300)
        {
            var ok = AdbService.Swipe(_instance, x1, y1, x2, y2, duration);
            if (!ok) LogService.Warn($"[{_botName}] swipe failed on {_instance}");
            return ok;
        }

        public bool inputText(string text)
        {
            var ok = AdbService.InputText(_instance, text);
            if (!ok) LogService.Warn($"[{_botName}] inputText failed on {_instance}");
            return ok;
        }

        public bool keyEvent(int code) => AdbService.KeyEvent(_instance, code);

        public bool shell(string command) => AdbService.Shell(_instance, command);
    }
}