namespace DuckBot.Core.Models
{
    // DTO used by Core for persistence — avoids referencing GUI types from Core.
    public class BotRecord
    {
        public string Name { get; set; } = "New Bot";
        public string Game { get; set; } = "West Game";
        public string? Instance { get; set; } = "None";
        public DuckBot.Data.Models.ScriptModel? Script { get; set; }
    }
}