using System.Collections.Generic;
using DuckBot.Core.Services;

namespace DuckBot.Core.Emu
{
    public static class AdbService
    {
        public static void Refresh() => EmulatorManager.Refresh();

        public static List<string> ListInstances() => EmulatorManager.GetInstanceNames();

        public static bool LaunchInstance(string instance) => EmulatorManager.TryLaunch(instance);

        public static bool StopInstance(string instance) => EmulatorManager.TryStop(instance);

        public static bool FocusInstance(string instance) => EmulatorManager.TryFocus(instance);

        public static bool Tap(string instance, int x, int y) => EmulatorManager.Tap(instance, x, y);

        public static bool Swipe(string instance, int x1, int y1, int x2, int y2, int durationMs)
            => EmulatorManager.Swipe(instance, x1, y1, x2, y2, durationMs);

        public static bool InputText(string instance, string text) => EmulatorManager.InputText(instance, text);

        public static bool KeyEvent(string instance, int keyCode) => EmulatorManager.KeyEvent(instance, keyCode);

        public static bool Shell(string instance, string command) => EmulatorManager.TryShell(instance, command);

        public static bool CaptureRawScreenshot(string instance, out byte[] data)
            => EmulatorManager.TryExecOut(instance, "exec-out screencap -p", out data);
    }
}