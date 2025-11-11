using System.Collections.Generic;

namespace DuckBot.Core.Models
{
    public class GeneralSettings
    {
        public string Emulator { get; set; } = "LDPlayer9";
        public bool AskEmulatorOnStart { get; set; } = true;
        public bool RestartOnBoot { get; set; } = true;
        public bool AutoSignInLastAccount { get; set; } = true;
        public bool ClearCache { get; set; } = false;
        public bool CloseEmulatorsOnStop { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public bool AutoSaveConfigs { get; set; } = true;
        public List<string> EmulatorPaths { get; set; } = new();
    }
}