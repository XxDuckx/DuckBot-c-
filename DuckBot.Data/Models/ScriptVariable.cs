using System.Collections.Generic;

namespace DuckBot.Data.Models
{
    public class ScriptVariable
    {
        public string Key { get; set; } = "";
        public string Default { get; set; } = "";
        public List<string>? Options { get; set; }
        public string? Prompt { get; set; }
        public bool Required { get; set; } = false;
    }
}