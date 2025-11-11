namespace DuckBot.Data.Models
{
    public class ScriptStep
    {
        public string Type { get; set; } = "";
        public Dictionary<string, object> Params { get; set; } = new();
        public List<ScriptStep>? ActionIfFound { get; set; }
        public List<ScriptStep>? ActionIfNotFound { get; set; }

        // TODO: Extend with helper methods for serialization, 
        // param validation, nested step handling, etc.
    }
}
