namespace DuckBot.Core.Models
{
    public class AdvancedSettings
    {
        public bool RestartReconnectedInstances { get; set; } = false;
        public bool KillBridgeIfOffline { get; set; } = false;
        public bool RestartGameIfHomeScreen { get; set; } = false;
        public bool SkipResolutionCheck { get; set; } = false;
        public bool SkipBotUpdatesDuringRun { get; set; } = false;
        public bool RestartAfterScriptTimeout { get; set; } = false;
        public bool RemoveCooldownScripts { get; set; } = false;
        public bool CloseUnusedEmulators { get; set; } = false;
        public int ConditionCheckDelayMs { get; set; } = 200;
        public string UpdateManifestUrl { get; set; } = "https://updates.duckbot.app/manifest.json";
    }
}