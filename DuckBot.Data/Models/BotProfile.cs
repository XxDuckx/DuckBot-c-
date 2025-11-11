using System.Collections.Generic;

namespace DuckBot.Data.Models
{
    public class BotProfile
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "New Bot";
        public string Game { get; set; } = "West Game";
        public string Instance { get; set; } = ""; // reserved LD instance name
        public List<ScriptSetting> Scripts { get; set; } = new();
        public List<AccountProfile> Accounts { get; set; } = new();
        public BotSettings Settings { get; set; } = new();
    }

    public class ScriptSetting
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    public class AccountProfile
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Pin { get; set; } = "";
        public bool Active { get; set; } = true;
    }

    public class BotSettings
    {
        public bool IgnoreCooldowns { get; set; } = false;
        public bool StopInstanceAfterLoop { get; set; } = false;
    }
}
