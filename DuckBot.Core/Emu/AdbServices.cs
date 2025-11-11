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
    }
}