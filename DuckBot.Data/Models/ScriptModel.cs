namespace DuckBot.Data.Models
{
    public class ScriptModel
    {
        public string Name { get; set; } = "";
        public string Game { get; set; } = "";
        public string Author { get; set; } = "Unknown";
        public List<ScriptStep> Steps { get; set; } = new();
        public List<ScriptVariable> Variables { get; set; } = new();
    }
}
